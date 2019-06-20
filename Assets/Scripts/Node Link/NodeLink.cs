using UnityEngine;
using SparseMatrix;
using System;
using System.Collections.Generic;

namespace EcoBuilder.NodeLink
{
    public partial class NodeLink : MonoBehaviour
    {
        public event Action OnDroppedOn;
        public event Action OnUnfocused;
        public event Action<int> OnNodeFocused;
        public event Action<int> OnNodeRemoved; // TODO: should not be here
        public event Action<int, int> OnLinkAdded;
        public event Action<int, int> OnLinkRemoved;

        [SerializeField] Node nodePrefab;
        [SerializeField] Link linkPrefab;
        [SerializeField] Transform graphParent, nodesParent, linksParent;

        private void FixedUpdate()
        {
            if (nodes.Count > 0)
            {
                //////////////////////
                // do stress SGD

                int dq = toBFS.Dequeue(); // only do one vertex at a time
                var d_j = ShortestPathsBFS(dq);

                LayoutSGD(dq, d_j);
                toBFS.Enqueue(dq);
            }

            if (doLayout)
            {
                TweenNodes();
                RotateMomentum();
            }
        }
        private void Update()
        {
            if (nodes.Count > 0 && !LaplacianDetZero)
            {
                ////////////////////////////////////
                // iterate trophic level equations and set y-axis

                TrophicGaussSeidel();
                MaxTrophic = 1;
                foreach (float trophic in trophicLevels)
                    MaxTrophic = Math.Max(trophic, MaxTrophic);

                float height = Mathf.Min(MaxChain, 2.5f);
                float trophicScaling = MaxTrophic>1? height / (MaxTrophic-1) : 1;
                foreach (Node no in nodes)
                {
                    float targetY = trophicScaling * (trophicLevels[no.Idx]-1);
                    // targetY = Mathf.Sqrt(targetY);
                    no.GoalPos -= new Vector3(0, no.GoalPos.y-targetY, 0);
                }
            }
        }
        // [SerializeField] GameObject busyIcon;
        bool constraintsSolved = true, calculating = false;
        private void LateUpdate()
        {
            if (!constraintsSolved && !calculating && nodes.Count>0)
            {
                constraintsSolved = true;
                ConstraintsAsync();
            }
            // busyIcon.SetActive(calculating);
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

            var startPos = new Vector3(UnityEngine.Random.Range(.5f, .9f), 0, -.2f);
            // var startPos = nodesParent.InverseTransformPoint(shape.transform.position);
            newNode.Init(idx, startPos, shape);
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
            newLink.Init(nodes[i], nodes[j], true);
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
        }
        public void SetIfLinkRemovable(int i, int j, bool removable)
        {
            links[i,j].Removable = removable;
        }


        [SerializeField] float minNodeSize=.5f, maxNodeSize=1.5f, minLinkFlow=.005f, maxLinkFlow=.05f;
        [SerializeField] float logRangeMultiplier=1.5f;
        public void ResizeNodes(Func<int, float> sizes)
        {
            float max=0, min=float.MaxValue;
            foreach (Node no in nodes)
            {
                float size = sizes(no.Idx);
                if (size > 0)
                {
                    max = Mathf.Max(max, size);
                    min = Mathf.Min(min, size);
                }
            }

            float logMin = Mathf.Log10(min / 2f); // avoid min==max
            float logMax = Mathf.Log10(max * 2f); // put in middle
            float sizeRange = maxNodeSize - minNodeSize;
            foreach (Node no in nodes)
            {
                float size = sizes(no.Idx);
                if (size > 0)
                {
                    float logSize = Mathf.Log10(size);
                    no.GoalSize = minNodeSize + sizeRange*((logSize-logMin) / (logMax-logMin));
                }
                else
                {
                    no.GoalSize = minNodeSize;
                }
            }
        }
        public void ReflowLinks(Func<int, int, float> flows)
        {
            float max=0, min=float.MaxValue;
            foreach (Link li in links)
            {
                int res=li.Source.Idx, con=li.Target.Idx;
                float flow = flows(res, con);
                if (flow > 0)
                {
                    max = Mathf.Max(max, flow);
                    min = Mathf.Min(min, flow);
                }
            }

            float logMin = Mathf.Log10(min / 2f); // avoid min==max
            float logMax = Mathf.Log10(max * 2f); // put in middle
            float flowRange = maxLinkFlow - minLinkFlow;
            foreach (Link li in links)
            {
                int res=li.Source.Idx, con=li.Target.Idx;
                float flow = flows(res, con);

                if (flow > 0)
                {
                    float logSpeed = Mathf.Log10(flow);
                    li.TileSpeed = minLinkFlow + flowRange*((logSpeed-logMin) / (logMax-logMin));
                }
                else
                {
                    li.TileSpeed = 0;
                }
            }
        }
        // public IEnumerable<Tuple<int, int>> GetLinks()
        // {
        //     foreach (var ij in links.IndexPairs)
        //     {
        //         yield return ij;
        //     }
        // }
    }
}