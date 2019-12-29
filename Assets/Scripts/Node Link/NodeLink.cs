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
        [SerializeField] Transform graphParent, nodesParent, linksParent, unfocusParent;
        [SerializeField] Effect heartPrefab, skullPrefab, confettiPrefab;

        void Start()
        {
            xRotation = xDefaultRotation = graphParent.localRotation.eulerAngles.x;
            defaultNodelinkPos = transform.localPosition;
            graphParent.localScale = Vector3.zero;
            StartCoroutine(TweenZoom(Vector3.one, 2));
        }
        void Update()
        {
            ////////////////////////////////////
            // do stress and trophic levels
            if (nodes.Count > 0)
            {
                if (focusState != FocusState.SuperFocus)
                {
                    int i = todoBFS.Dequeue(); // only do one vertex at a time
                    var d_j = ShortestPathsBFS(i);

                    if (ConstrainTrophic && !LaplacianDetZero)
                    {
                        TrophicGaussSeidel();
                        LayoutMajorizationHorizontal(i, d_j);
                    }
                    else
                    {
                        LayoutMajorization(i, d_j);
                    }
                    todoBFS.Enqueue(i);
                }
            }
            ///////////////////////////////////
            // smoothly interpolate positions
            TweenNodes();
            MomentumRotate();
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
            if (nodes[idx] != null)
                throw new Exception("index " + idx + " already added");

            if (nodes[idx] == null && nodeGrave[idx] == null) // entirely new
            {
                Node newNode = Instantiate(nodePrefab, nodesParent);
                newNode.Init(idx);
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
                    if (nodes[col] != null)
                    {
                        links[idx, col] = linkGrave[idx, col];
                        links[idx, col].gameObject.SetActive(true);
                        adjacency[idx].Add(col);
                        adjacency[col].Add(idx);
                    }
                }
                foreach (int row in linkGrave.GetRowIndicesInColumn(idx))
                {
                    if (nodes[row] != null)
                    {
                        links[row, idx] = linkGrave[row, idx];
                        links[row, idx].gameObject.SetActive(true);
                        adjacency[idx].Add(row);
                        adjacency[row].Add(idx);
                    }
                }
                // clear linkGrave
                foreach (int col in links.GetColumnIndicesInRow(idx))
                    linkGrave.RemoveAt(idx, col);
                foreach (int row in links.GetRowIndicesInColumn(idx))
                    linkGrave.RemoveAt(row, idx);
            }
            // nodes[idx].StressPos = Vector3.back + .5f*UnityEngine.Random.insideUnitSphere;
            nodes[idx].StressPos = UnityEngine.Random.insideUnitCircle;
            todoBFS.Enqueue(idx);
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
            todoBFS.Clear();
            foreach (int i in adjacency.Keys)
            {
                adjacency[i].Remove(idx);
                todoBFS.Enqueue(i);
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
                // links[i,j].Curved = links[j,i].Curved = true;
                throw new Exception("no bidirectional links allowed");
            }

            adjacency[i].Add(j);
            adjacency[j].Add(i);

            nodes[i].Disconnected = false;
            nodes[j].Disconnected = false;

            OnLinked?.Invoke();
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
                throw new Exception("no bidirectional links allowed");
            }

            if (adjacency[i].Count == 0)
                nodes[i].Disconnected = true;
            if (adjacency[j].Count == 0)
                nodes[j].Disconnected = true;

            OnLinked?.Invoke();
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
        public void SetIfNodeRemovable(int idx, bool removable)
        {
            nodes[idx].Removable = removable;
        }
        public void SetIfLinkRemovable(int i, int j, bool removable)
        {
            links[i,j].Removable = removable;
        }

        // to give other classes access to the adjacency
        public IEnumerable<int> GetTargets(int source)
        {
            return links.GetColumnIndicesInRow(source);
        }


        ////////////
        // effects

        public void RehealthBars(Func<int, float> healths)
        {
            foreach (Node no in nodes)
            {
                float health = healths(no.Idx);
                health = Mathf.Max(Mathf.Min(health, 1), 0);
                no.SetHealth(health);
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

        public void FlashNode(int idx)
        {
            nodes[idx].Flash(true);
        }
        public void UnflashNode(int idx)
        {
            nodes[idx].Flash(false);
        }
        public void SkullEffectNode(int idx)
        {
            Instantiate(skullPrefab, nodes[idx].transform);
        }
        public void HeartEffectNode(int idx)
        {
            Instantiate(heartPrefab, nodes[idx].transform);
        }
        public void BounceNode(int idx)
        {
            nodes[idx].Bounce();
        }
        public void LieDownNode(int idx)
        {
            nodes[idx].LieDown();
        }

        // adds a gameobject like the jump shadow in mario bros.
        public void AddDropShadow(GameObject shadowPrefab)
        {
            if (shadowPrefab == null)
                return;

            var shadow = Instantiate(shadowPrefab);
            shadow.transform.SetParent(nodesParent, false);
            shadow.transform.localPosition = Vector3.zero;
            shadow.transform.localRotation = Quaternion.AngleAxis(-45, Vector3.up);
        }

        public void OutlineNode(int idx, cakeslice.Outline.Colour colour)
        {
            nodes[idx].PushOutline(colour);
        }
        public void UnoutlineNode(int idx)
        {
            nodes[idx].PopOutline();
        }
        public void OutlineLink(int src, int trg, cakeslice.Outline.Colour colour)
        {
            links[src,trg].PushOutline(colour);
        }
        public void OutlineChain(bool highlighted, cakeslice.Outline.Colour colour)
        {
            if (MaxChain == 0)
                return;
            if (highlighted)
            {

                nodes[TallestNodes[0]].PushOutline(colour);
                for (int i=1; i<TallestNodes.Count; i++)
                {
                    links[TallestNodes[i-1], TallestNodes[i]].PushOutline(colour);
                    nodes[TallestNodes[i]].PushOutline(colour);
                }
            }
            else
            {
                nodes[TallestNodes[0]].PopOutline();
                for (int i=1; i<TallestNodes.Count; i++)
                {
                    links[TallestNodes[i-1], TallestNodes[i]].PopOutline();
                    nodes[TallestNodes[i]].PopOutline();
                }
            }
        }
        // public void OutlineLoop(bool highlighted)
        // {
        //     if (highlighted)
        //     {
        //         foreach (int idx in LongestLoop)
        //             nodes[idx].Outline(cakeslice.Outline.Colour.Red);
        //     }
        //     else
        //     {
        //         foreach (int idx in LongestLoop)
        //             nodes[idx].Unoutline();
        //     }
        // }

        // TODO: this is a bit messy and doesn't work if graph is spinning
        public void TooltipNode(int idx, string msg)
        {
            tooltip.transform.position = Camera.main.WorldToScreenPoint(nodes[idx].transform.position);
            tooltip.ShowText(msg);
            tooltip.Enable();
            tweenNodes = dragging = false;
        }
        public void UntooltipNode(int idx)
        {
            tooltip.Disable();
            tweenNodes = dragging = true;
        }
    }
}