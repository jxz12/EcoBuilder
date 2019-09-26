using UnityEngine;
using SparseMatrix;
using System;
using System.Linq;
using System.Collections.Generic;

namespace EcoBuilder.NodeLink
{
    public partial class NodeLink : MonoBehaviour
    {
        public event Action<int> OnNodeFocused;
        public event Action OnUnfocused;
        public event Action OnEmptyPressed;
        public event Action OnConstraints;
        public event Action OnLinked;

        // called when user does something
        public event Action<int, int> OnUserLinked;
        public event Action<int, int> OnUserUnlinked;

        [SerializeField] Node nodePrefab;
        [SerializeField] Link linkPrefab;
        [SerializeField] Transform graphParent, nodesParent, linksParent;

        [SerializeField] float etaMax, etaDecay;
        int etaIteration = 0;
        private void FixedUpdate()
        {
            //////////////////////
            // do stress SGD
            if (nodes.Count > 0)
            {
                if (focusState != FocusState.SuperFocus && focusState != FocusState.SuperUnfocus)
                {
                    int dq = toBFS.Dequeue(); // only do one vertex at a time
                    var d_j = ShortestPathsBFS(dq);

                    float eta = etaMax / (1f + etaDecay*(etaIteration++/nodes.Count));
                    if (constrainTrophic)
                        LayoutSGDHorizontal(dq, d_j, eta);
                    else
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

        SparseVector<Node> nodeGrave = new SparseVector<Node>();
        SparseMatrix<Link> linkGrave = new SparseMatrix<Link>();

        public void AddNode(int idx)
        {
            // TODO: mystery bug?!
            if (nodes[idx] != null)
                throw new Exception("index " + idx + " already added");

            if (nodes[idx] == null && nodeGrave[idx] == null) // entirely new
            {
                Node newNode = Instantiate(nodePrefab, nodesParent);

                newNode.Init(idx, (minNodeSize+maxNodeSize)/2);
                nodes[idx] = newNode;
                adjacency[idx] = new HashSet<int>();
            }
            else if (nodeGrave[idx] != null) // bring back old
            {
                nodes[idx] = nodeGrave[idx];
                nodeGrave.RemoveAt(idx);
                nodes[idx].gameObject.SetActive(true);

                adjacency[idx] = new HashSet<int>();
                foreach (int col in linkGrave.GetColumnIndicesInRow(idx))
                {
                    links[idx, col] = linkGrave[idx, col];
                    links[idx, col].gameObject.SetActive(true);
                    adjacency[idx].Add(col);
                    adjacency[col].Add(idx);
                }
                foreach (int row in linkGrave.GetRowIndicesInColumn(idx))
                {
                    links[row, idx] = linkGrave[row, idx];
                    links[row, idx].gameObject.SetActive(true);
                    adjacency[idx].Add(row);
                    adjacency[row].Add(idx);
                }
                // clear linkGrave
                foreach (int col in links.GetColumnIndicesInRow(idx))
                    linkGrave.RemoveAt(idx, col);
                foreach (int row in links.GetRowIndicesInColumn(idx))
                    linkGrave.RemoveAt(row, idx);
            }
            nodes[idx].StressPos += UnityEngine.Random.insideUnitSphere * .2f; // prevent divide by zero
            toBFS.Enqueue(idx);
        }

        public void RemoveNode(int idx)
        {
            if (nodes[idx] == null)
                throw new Exception("no index " + idx);
            if (focusedNode != null && focusedNode.Idx == idx)
                FullUnfocus();

            // move to graveyard to save for later
            nodes[idx].gameObject.SetActive(false);
            nodeGrave[idx] = nodes[idx];
            nodes.RemoveAt(idx);

            foreach (int col in links.GetColumnIndicesInRow(idx))
            {
                links[idx, col].gameObject.SetActive(false);
                linkGrave[idx, col] = links[idx, col];
            }
            foreach (int row in links.GetRowIndicesInColumn(idx))
            {
                links[row, idx].gameObject.SetActive(false);
                linkGrave[row, idx] = links[row, idx];
            }
            // clear links
            foreach (int col in linkGrave.GetColumnIndicesInRow(idx))
                links.RemoveAt(idx, col);
            foreach (int row in linkGrave.GetRowIndicesInColumn(idx))
                links.RemoveAt(row, idx);


            // prevent memory leak in SGD data structures
            adjacency.Remove(idx);
            toBFS.Clear();
            foreach (int i in adjacency.Keys)
            {
                adjacency[i].Remove(idx);
                toBFS.Enqueue(i);
            }
        }        
        public void RemoveNodeCompletely(int idx)
        {
            if (nodeGrave[idx] == null)
                throw new Exception("node not in graveyard");
            
            Destroy(nodeGrave[idx].gameObject);
            nodeGrave.RemoveAt(idx);

            foreach (int col in linkGrave.GetColumnIndicesInRow(idx).ToArray())
                linkGrave.RemoveAt(idx, col);
            foreach (int row in linkGrave.GetRowIndicesInColumn(idx).ToArray())
                linkGrave.RemoveAt(row, idx);
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

            OnLinked.Invoke();
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

            OnLinked.Invoke();
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
            return links.GetColumnIndicesInRow(source);
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
    }
}