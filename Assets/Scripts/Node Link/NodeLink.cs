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
        public event Action<int> OnNodeRemoved;
        public event Action<int, int> OnLinkAdded;
        public event Action<int, int> OnLinkRemoved;

        [SerializeField] Node nodePrefab;
        [SerializeField] Link linkPrefab;
        [SerializeField] Transform graphParent, nodesParent, linksParent;

        [SerializeField] float etaMax, etaDecay;
        // [SerializeField] float eta;
        float etaIteration = 0;
        private void FixedUpdate()
        {
            //////////////////////
            // do stress SGD
            if (nodes.Count > 0)
            {
                if (focus == null)
                {
                    int dq = toBFS.Dequeue(); // only do one vertex at a time
                    var d_j = ShortestPathsBFS(dq);

                    float eta = etaMax / (1f + etaDecay*(float)etaIteration++);
                    LayoutSGD(dq, d_j, eta);
                    toBFS.Enqueue(dq);
                }
            }
            ///////////////////////////////
            // calculate trophic levels
            if (!LaplacianDetZero)
            {
                TrophicGaussSeidel();
            }

            if (doLayout)
                TweenNodes();
            if (!Input.anyKey && Input.touchCount==0)
                RotateWithMomentum();
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
                etaIteration = 0; // reset SGD

                Disjoint = CheckDisjoint();
                NumEdges = links.Count();

                HashSet<int> basal = BuildTrophicEquations();
                var heights = HeightBFS(basal);
                LaplacianDetZero = (heights.Count != nodes.Count);
                // MaxTrophic done in Update()

                if (focus != null)
                {
                    SuperFocus();
                }

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
            newLink.Init(nodes[i], nodes[j]);
            links[i,j] = newLink;
            if (links[j,i] != null)
            {
                links[i,j].Curved = links[j,i].Curved = true;
            }

            adjacency[i].Add(j);
            adjacency[j].Add(i);

            constraintsSolved = false;
            OnLinkAdded.Invoke(i, j);
        }
        public void RemoveLink(int i, int j)
        {
            Destroy(links[i,j].gameObject);
            links.RemoveAt(i,j);
            if (links[j,i] == null)
            {
                adjacency[i].Remove(j);
                adjacency[j].Remove(i);
            }
            else
            {
                links[j,i].Curved = false;
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
            // if (removable)
            //     nodes[idx].GetComponent<MeshRenderer>().material = nodeRemovable;
            // else
            //     nodes[idx].GetComponent<MeshRenderer>().material = nodeFixed;
        }
        public void SetIfLinkRemovable(int i, int j, bool removable)
        {
            links[i,j].Removable = removable;
            // if (removable)
            //     links[i,j].GetComponent<LineRenderer>().material = linkRemovable;
            // else
            //     links[i,j].GetComponent<LineRenderer>().material = linkFixed;
        }


        [SerializeField] float minNodeSize, maxNodeSize;
        public void ResizeNodes(Func<int, float> sizes)
        {
            float sizeRange = maxNodeSize - minNodeSize;
            foreach (Node no in nodes)
            {
                float size = sizes(no.Idx);
                if (size > 0)
                {
                    no.Size = minNodeSize + sizeRange*size;
                }
                else
                {
                    no.Size = minNodeSize;
                }
            }
        }
        [SerializeField] float minLinkFlow, maxLinkFlow;
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
                    li.TileSpeed = minLinkFlow;
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