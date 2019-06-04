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
        public event Action<int, int> OnLinkAdded;
        public event Action<int, int> OnLinkRemoved;

        [SerializeField] Node nodePrefab;
        [SerializeField] Link linkPrefab;
        [SerializeField] Transform graphParent, nodesParent, linksParent;

        public event Action OnLaplacianUnsolvable;
        public event Action OnLaplacianSolvable;

        bool laplacianDetNeg = false;
        private void FixedUpdate()
        {
            if (potentialHold)
                return;

            if (nodes.Count > 0)
            {
                ///////////////////////////////
                // First do height calculations
                HashSet<int> basal = BuildTrophicEquations();
                var heights = HeightBFS(basal);

                if (heights.Count == nodes.Count) // if every node can be reached from basal
                {
                    if (laplacianDetNeg == true)
                    {
                        laplacianDetNeg = false;
                        OnLaplacianSolvable.Invoke();
                    }
                    TrophicGaussSeidel();
                    foreach (Node no in nodes)
                    {
                        float targetY = .4f*heights[no.Idx]+.3f*(trophicLevels[no.Idx]-1);
                        no.TargetPos -= new Vector3(0, no.TargetPos.y-targetY, 0);
                    }
                }
                else
                {
                    if (laplacianDetNeg == false)
                    {
                        laplacianDetNeg = true;
                        OnLaplacianUnsolvable.Invoke();
                    }
                }

                //////////////////////
                // then do stress SGD
                int dq = toBFS.Dequeue(); // only do one vertex per frame
                var d_j = ShortestPathsBFS(dq);

                LayoutSGD(dq, d_j);
                toBFS.Enqueue(dq);

                // center
                TweenNodes();
            }

            if (!dragging)
                Rotate();
        }

        SparseVector<Node> nodes = new SparseVector<Node>();
        SparseMatrix<Link> links = new SparseMatrix<Link>();
        Dictionary<int, HashSet<int>> adjacency = new Dictionary<int, HashSet<int>>();

        public void AddNode(int idx, GameObject shape)
        {
            if (nodes[idx] != null)
                throw new Exception("already has idx " + idx);

            Node newNode = Instantiate(nodePrefab, nodesParent);

            var startPos = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f));
            newNode.Init(idx, startPos, shape);
            nodes[idx] = newNode;

            focus = newNode;

            adjacency[idx] = new HashSet<int>();
            toBFS.Enqueue(idx);
        }
        public void ResizeNodes(Func<int, float> sizes)
        {
            // float max=0, min=float.MaxValue;
            foreach (Node no in nodes)
            {
                int idx = no.Idx;
                float size = sizes(idx);
                if (size >= 0)
                {
                    no.Size = .5f + 10*Mathf.Sqrt(size);
                }
                else
                {
                    no.Size = .4f;
                }
            }

        }

        // public void ResizeNodeOutline(int idx, float size)
        // {
        //     if (size < 0)
        //         throw new Exception("size cannot be negative");

        //     nodes[idx].OutlineSize = .5f + Mathf.Sqrt(size);
        // }

        public void RemoveNode(int idx)
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
        }

        private void AddLink(int i, int j)
        {
            Link newLink = Instantiate(linkPrefab, linksParent);
            newLink.Init(nodes[i], nodes[j], true);
            links[i, j] = newLink;

            adjacency[i].Add(j);
            adjacency[j].Add(i);
        }
        private void RemoveLink(int i, int j)
        {
            Destroy(links[i, j].gameObject);
            links.RemoveAt(i, j);
            if (links[j, i] == null)
            {
                adjacency[i].Remove(j);
                adjacency[j].Remove(i);
            }
        }
        public void SetNodeAsSourceOnly(int idx, bool isSource) // basal
        {
            nodes[idx].IsSourceOnly = isSource;
        }
        public void SetNodeAsSinkOnly(int idx, bool isSink) // apex predator
        {
            nodes[idx].IsSinkOnly = isSink;
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