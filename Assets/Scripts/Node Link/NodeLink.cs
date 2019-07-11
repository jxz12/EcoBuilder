using UnityEngine;
using SparseMatrix;
using System;
using System.Collections.Generic;

namespace EcoBuilder.NodeLink
{
    public partial class NodeLink : MonoBehaviour
    {
        public event Action OnEmptyPressed;
        public event Action<int> OnNodeFocused;
        public event Action<int> OnNodeRemoved; // TODO: should not be here
        public event Action<int, int> OnLinkAdded;
        public event Action<int, int> OnLinkRemoved;

        [SerializeField] Node nodePrefab;
        [SerializeField] Link linkPrefab;
        [SerializeField] Transform graphParent, nodesParent, linksParent;

        [SerializeField] float etaMax=1f, etaDecay = .99f;
        [SerializeField] float eta;
        private void FixedUpdate()
        {
            if (nodes.Count > 0 && eta > 1e-3)
            {
                //////////////////////
                // do stress SGD

                int dq = toBFS.Dequeue(); // only do one vertex at a time
                var d_j = ShortestPathsBFS(dq);

                eta *= etaDecay;
                LayoutSGD(dq, d_j, eta);
                nodes[dq].GoalPos = new Vector3(nodes[dq].GoalPos.x, nodes[dq].GoalPos.y, nodes[dq].GoalPos.z * .99f);
                toBFS.Enqueue(dq);
            }

            if (doLayout)
                TweenNodes();
            if (!Input.anyKey && Input.touchCount==0)
                RotateMomentum();
        }
        private void Update()
        {
            if (nodes.Count > 0 && !LaplacianDetZero)
            {
                ////////////////////////////////////
                // iterate trophic level equations and set y-axis

                TrophicGaussSeidel();
                float maxTrophic = 1;
                foreach (float trophic in trophicLevels)
                    maxTrophic = Mathf.Max(trophic, maxTrophic);

                float height = Mathf.Min(MaxChain, 3f);
                float trophicScaling = maxTrophic>1? height / (maxTrophic-1) : 1;
                foreach (Node no in nodes)
                {
                    float targetY = trophicScaling * (trophicLevels[no.Idx]-1);
                    // targetY = Mathf.Sqrt(targetY);
                    // targetY *= .99f; 
                    // if (targetY == 0)
                    // {
                    //     no.GoalPos -= new Vector3(0, no.GoalPos.y-targetY, 0);
                    // }
                    // else
                    // {
                        no.GoalPos -= new Vector3(0, .1f*(no.GoalPos.y-targetY), 0);
                    // }
                }
            }
        }
        bool constraintsSolved = true, calculating = false;
        public bool Ready { get { return constraintsSolved && !calculating; } }
        private void LateUpdate()
        {
            if (!constraintsSolved && !calculating && nodes.Count>0)
            {
                ///////////////////////////////
                // do constraint calculations

                constraintsSolved = true;
                eta = etaMax;

                Disjoint = CheckDisjoint();
                NumEdges = links.Count();

                HashSet<int> basal = BuildTrophicEquations();
                var heights = HeightBFS(basal);
                LaplacianDetZero = (heights.Count != nodes.Count);
                // MaxTrophic done in Update()

                MaxChain = 0;
                foreach (int height in heights.Values)
                    MaxChain = Math.Max(height, MaxChain);

                #if UNITY_WEBGL
                    ConstraintsSync();
                #else
                    ConstraintsAsync();
                #endif
            }
        }



        ///////////////////////////////////
        // structure changing functions

        SparseVector<Node> nodes = new SparseVector<Node>();
        SparseMatrix<Link> links = new SparseMatrix<Link>();
        Dictionary<int, HashSet<int>> adjacency = new Dictionary<int, HashSet<int>>();

        public void AddNode(int idx, GameObject shape)
        {
            if (nodes[idx] != null)
                throw new Exception("already has idx " + idx);

            Node newNode = Instantiate(nodePrefab, nodesParent);

            var startPos = new Vector3(UnityEngine.Random.Range(.5f, 1f), 0, -.2f);
            // var startPos = nodesParent.InverseTransformPoint(shape.transform.position);

            newNode.Init(idx, startPos, (minNodeSize+maxNodeSize)/2, shape);
            nodes[idx] = newNode;

            FocusNode(idx);

            adjacency[idx] = new HashSet<int>();
            toBFS.Enqueue(idx);

            constraintsSolved = false;
        }


        void RemoveNode(int idx)
        {
            if (focus != null && focus.Idx == idx)
                Unfocus();

            Destroy(nodes[idx].gameObject);
            nodes.RemoveAt(idx);

            // prevent memory leak in sparse matrices
            var toRemove = new List<Tuple<int,int>>();
            foreach (int other in links.GetColumnIndicesInRow(idx))
                toRemove.Add(Tuple.Create(idx, other));
            foreach (int other in links.GetRowIndicesInColumn(idx))
                toRemove.Add(Tuple.Create(other, idx));

            foreach (var ij in toRemove)
                RemoveLink(ij.Item1, ij.Item2);

            // prevent memory leak in SGD data structures
            adjacency.Remove(idx);
            toBFS.Clear();
            foreach (int i in adjacency.Keys)
            {
                adjacency[i].Remove(idx);
                toBFS.Enqueue(i);
            }

            // prevent memory leak in trophic level data structures
            trophicA.RemoveAt(idx);
            trophicLevels.RemoveAt(idx);

            constraintsSolved = false;
            OnNodeRemoved.Invoke(idx);
        }

        public void AddLink(int i, int j)
        {
            Link newLink = Instantiate(linkPrefab, linksParent);
            newLink.Init(nodes[i], nodes[j], false);
            links[i, j] = newLink;

            adjacency[i].Add(j);
            adjacency[j].Add(i);

            constraintsSolved = false;
            OnLinkAdded.Invoke(i, j);
        }
        public void RemoveLink(int i, int j)
        {
            Destroy(links[i, j].gameObject);
            links.RemoveAt(i, j);
            if (links[j, i] == null)
            {
                adjacency[i].Remove(j);
                adjacency[j].Remove(i);
            }

            constraintsSolved = false;
            OnLinkRemoved.Invoke(i,j);
        }

        public void SetIfNodeCanBeSource(int idx, bool canBeSource) // basal
        {
            nodes[idx].CanBeSource = canBeSource;
        }
        public void SetIfNodeCanBeTarget(int idx, bool canBeTarget) // apex predator
        {
            nodes[idx].CanBeTarget = canBeTarget;
        }
        public void SetIfNodeRemovable(int idx, bool removable)
        {
            nodes[idx].Removable = removable;
            // if (!removable)
            //     nodes[idx].Outline();
            // else
            //     nodes[idx].Unoutline();
        }
        public void SetIfLinkRemovable(int i, int j, bool removable)
        {
            links[i,j].Removable = removable;
            // if (!removable)
            //     links[i,j].Outline();
            // else
            //     links[i,j].Unoutline();
        }


        [SerializeField] float minNodeSize=.5f, maxNodeSize=1.5f;
        public void ResizeNodes(Func<int, float> sizes)
        {
            float sizeRange = maxNodeSize - minNodeSize;
            foreach (Node no in nodes)
            {
                float size = sizes(no.Idx);
                if (size > 0)
                {
                    no.GoalSize = minNodeSize + sizeRange*size;
                }
                else
                {
                    no.GoalSize = minNodeSize;
                }
            }
        }
        [SerializeField] float minLinkFlow=.005f, maxLinkFlow=.1f;
        public void ReflowLinks(Func<int, int, float> flows)
        {
            float flowRange = maxLinkFlow - minLinkFlow;
            foreach (Link li in links)
            {
                int res=li.Source.Idx, con=li.Target.Idx;
                float flow = flows(res, con);
                if (flow > 0)
                {
                    li.TileSpeed = minLinkFlow + flowRange*flow;
                }
                else
                {
                    li.TileSpeed = 0;
                }
            }
        }
        public Tuple<List<int>, List<int>> GetLinkIndices()
        {
            var sources = new List<int>();
            var targets = new List<int>();
            foreach (var ij in links.IndexPairs)
            {
                sources.Add(ij.Item1);
                targets.Add(ij.Item2);
            }
            return Tuple.Create(sources, targets);
        }
    }
}