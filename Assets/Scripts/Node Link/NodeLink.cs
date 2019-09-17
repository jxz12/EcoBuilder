using UnityEngine;
using SparseMatrix;
using System;
using System.Collections.Generic;

namespace EcoBuilder.NodeLink
{
    public partial class NodeLink : MonoBehaviour
    {
        public event Action<int> OnNodeFocused;
        public event Action OnUnfocused;
        public event Action OnEmptyPressed;
        public event Action OnConstraints;

        // called when user does something
        public event Action<int, int> OnUserLinked;
        public event Action<int, int> OnUserUnlinked;


        [SerializeField] Node nodePrefab;
        [SerializeField] Link linkPrefab;
        [SerializeField] Transform graphParent, nodesParent, linksParent;

        [SerializeField] float etaMax, etaDecay;
        float etaIteration = 0;
        private void FixedUpdate()
        {
            //////////////////////
            // do stress SGD
            if (nodes.Count > 0)
            {
                if (!superfocused)
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


        ///////////////////////////////////
        // structure changing functions

        SparseVector<Node> nodes = new SparseVector<Node>();
        SparseMatrix<Link> links = new SparseMatrix<Link>();
        Dictionary<int, HashSet<int>> adjacency = new Dictionary<int, HashSet<int>>();

        public void AddNode(int idx)
        {
            adjacency[idx] = new HashSet<int>();

            if (nodes[idx] == null)
            {
                Node newNode = Instantiate(nodePrefab, nodesParent);

                var startPos = new Vector3(UnityEngine.Random.Range(.5f, 1f), 0, -.2f);
                // var startPos = nodesParent.InverseTransformPoint(shape.transform.position);

                newNode.Init(idx, startPos, (minNodeSize+maxNodeSize)/2);
                nodes[idx] = newNode;
            }
            else // readdition
            {
                if (nodes[idx].isActiveAndEnabled)
                    throw new Exception("node already active at idx " + idx);

                nodes[idx].gameObject.SetActive(true);
                foreach (int col in links.GetColumnIndicesInRow(idx))
                {
                    links[idx,col].gameObject.SetActive(true);
                    adjacency[idx].Add(col);
                    adjacency[col].Add(idx);
                    // OnLinked.Invoke(row, idx);
                }
                foreach (int row in links.GetRowIndicesInColumn(idx))
                {
                    links[row,idx].gameObject.SetActive(true);
                    adjacency[idx].Add(row);
                    adjacency[row].Add(idx);
                    // OnLinked.Invoke(row, idx);
                }
            }
            toBFS.Enqueue(idx);
            ConstraintsSolved = false;
        }

        public void RemoveNode(int idx)
        {
            if (nodes[idx] == null)
                throw new Exception("no index " + idx);
            if (focusedNode != null && focusedNode.Idx == idx)
                Unfocus();

            // disable instead of completely remove
            nodes[idx].gameObject.SetActive(false);
            foreach (Link li in links.GetColumnData(idx))
            {
                li.gameObject.SetActive(false);
                // OnUnlinked.Invoke(li.Source.Idx, li.Target.Idx);
            }
            foreach (Link li in links.GetRowData(idx))
            {
                li.gameObject.SetActive(false);
                // OnUnlinked.Invoke(li.Source.Idx, li.Target.Idx);
            }

            // prevent memory leak in SGD data structures
            adjacency.Remove(idx);
            toBFS.Clear();
            foreach (int i in adjacency.Keys)
            {
                adjacency[i].Remove(idx);
                toBFS.Enqueue(i);
            }
            ConstraintsSolved = false;
        }

        // public void AddNode(int idx)
        // {
        //     if (nodes[idx] != null)
        //         throw new Exception("already has idx " + idx);

        //     Node newNode = Instantiate(nodePrefab, nodesParent);

        //     var startPos = new Vector3(UnityEngine.Random.Range(.5f, 1f), 0, -.2f);
        //     // var startPos = nodesParent.InverseTransformPoint(shape.transform.position);

        //     newNode.Init(idx, startPos, (minNodeSize+maxNodeSize)/2);
        //     nodes[idx] = newNode;

        //     adjacency[idx] = new HashSet<int>();
        //     toBFS.Enqueue(idx);

        //     ConstraintsSolved = false;
        // }
        // public void RemoveNode(int idx)
        // {
        //     if (nodes[idx] == null)
        //         throw new Exception("no index " + idx);
        //     if (focusedNode != null && focusedNode.Idx == idx)
        //         Unfocus();

        //     Destroy(nodes[idx].gameObject);
        //     nodes.RemoveAt(idx);

        //     // prevent memory leak in sparse matrices
        //     var toRemove = new List<Tuple<int,int>>();
        //     foreach (int other in links.GetColumnIndicesInRow(idx))
        //         toRemove.Add(Tuple.Create(idx, other));
        //     foreach (int other in links.GetRowIndicesInColumn(idx))
        //         toRemove.Add(Tuple.Create(other, idx));

        //     foreach (var ij in toRemove)
        //         RemoveLink(ij.Item1, ij.Item2);

        //     // prevent memory leak in SGD data structures
        //     adjacency.Remove(idx);
        //     toBFS.Clear();
        //     foreach (int i in adjacency.Keys)
        //     {
        //         adjacency[i].Remove(idx);
        //         toBFS.Enqueue(i);
        //     }

        //     // prevent memory leak in trophic level data structures
        //     trophicA.RemoveAt(idx);
        //     trophicLevels.RemoveAt(idx);

        //     ConstraintsSolved = false;
        // }

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

            ConstraintsSolved = false;
            // OnLinked.Invoke(i, j);
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

            ConstraintsSolved = false;
            // OnUnlinked.Invoke(i, j);
        }

        public void ShapeNode(int idx, GameObject shape)
        {
            if (nodes[idx] == null)
                throw new Exception("no index " + idx);

            nodes[idx].Shape(shape);
        }
        public void SetIfNodeCanBeSource(int idx, bool canBeSource) // basal
        {
            nodes[idx].CanBeSource = canBeSource;
        }
        public void SetIfNodeCanBeTarget(int idx, bool canBeTarget) // apex predator
        {
            nodes[idx].CanBeTarget = canBeTarget;
        }
        // public void SetIfNodeRemovable(int idx, bool removable)
        // {
        //     // nodes[idx].Removable = removable;
        //     // if (removable)
        //     //     nodes[idx].GetComponent<MeshRenderer>().material = nodeRemovable;
        //     // else
        //     //     nodes[idx].GetComponent<MeshRenderer>().material = nodeFixed;
        // }
        public void SetIfLinkRemovable(int i, int j, bool removable)
        {
            links[i,j].Removable = removable;
            // if (removable)
            //     links[i,j].GetComponent<LineRenderer>().material = linkRemovable;
            // else
            //     links[i,j].GetComponent<LineRenderer>().material = linkFixed;
        }

        public IEnumerable<int> GetTargets(int source)
        {
            foreach (int target in links.GetColumnIndicesInRow(source))
            {
                // FIXME: ugly
                if (adjacency.ContainsKey(target))
                {
                    yield return target;
                }
            }
        }

        [SerializeField] float minNodeSize, maxNodeSize;
        public void ResizeNodes(Func<int, float> sizes)
        {
            float sizeRange = maxNodeSize - minNodeSize;
            foreach (Node no in nodes)
            {
                if (!no.isActiveAndEnabled)
                    continue;

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
                if (!li.isActiveAndEnabled)
                    continue;

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
    }
}