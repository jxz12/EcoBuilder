using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

// for heavy calculations
using System.Threading.Tasks;

namespace EcoBuilder.NodeLink
{
    public partial class NodeLink : MonoBehaviour
    {
        // called regardless of user
        public event Action<int> OnFocused;
        public event Action<int> OnUnfocused;
        public event Action OnLayedOut;
        public event Action OnLinked;

        // called only when user does something
        public event Action<int, int> OnUserLinked;
        public event Action<int, int> OnUserUnlinked;
        public event Action OnEmptyTapped;

        [SerializeField] Node nodePrefab;
        [SerializeField] Link linkPrefab;
        [SerializeField] Transform graphParent, nodesParent, linksParent, unfocusParent;

        Camera mainCam;
        void Start()
        {
            mainCam = Camera.main;
            xRotation = xDefaultRotation = graphParent.localRotation.eulerAngles.x;
        }
        void Update()
        {
            if (layoutTriggered && !isCalculatingAsync)
            {
                layoutTriggered = false;
                Layout();
            }

            FineTuneLayout();
            if (GraphLayedOut) {
                SeparateConnectedComponents();
            }
            if (tweenNodes) {
                TweenNodesToStress();
                TweenZoomToFit();
                MomentumRotate();
            }

        }

        private bool layoutTriggered = false;
        private bool isCalculatingAsync = false;
        public bool GraphLayedOut { get { return !layoutTriggered && !isCalculatingAsync; } }

// because webgl does not support threads
#if !UNITY_WEBGL
        public async void Layout() {
#else
        public void Layout() {
#endif
            isCalculatingAsync = true;

            CountConnectedComponents();
            RefreshTrophicAndFindChain(1);
            NumEdges = links.Count();

            SGD.InitSGD((i)=>nodes[i].StressPos, undirected); // not async to ensure synchronize state
#if !UNITY_WEBGL
            await Task.Run(()=> SGD.LayoutSGD(ConstrainTrophic, t_init, t_max, eps));
            SGD.RewriteSGD((i,v)=>{ if (nodes[i]!=null) nodes[i].StressPos=v; }); // in case node is deleted
#else
            SGD.LayoutSGD(ConstrainTrophic, t_init, t_max, eps);
            SGD.RewriteSGD((i,v)=>nodes[i].StressPos=v);
#endif

            Johnson.JohnsonInit(nodes.Indices, links.IndexPairs); // not async to ensure synchronize state
#if !UNITY_WEBGL
            var loop = await Task.Run(()=> Johnson.JohnsonsAlgorithm());
#else
            var loop = Johnson.JohnsonsAlgorithm();
#endif
            NumMaxLoop = loop.Item1;
            LongestLoop = loop.Item2;

            isCalculatingAsync = false;
            OnLayedOut.Invoke();
        }

        ///////////////////////////////////
        // structure changing functions

        // show versions for random access operations
        SparseVector<Node> nodes = new SparseVector<Node>();
        SparseMatrix<Link> links = new SparseMatrix<Link>();

        SparseVector<Node> nodeGrave = new SparseVector<Node>();
        SparseMatrix<Link> linkGrave = new SparseMatrix<Link>();

        Dictionary<int, HashSet<int>> undirected = new Dictionary<int, HashSet<int>>();

        public void AddNode(int idx, GameObject shape)
        {
            Assert.IsNull(nodes[idx], $"node {idx} already added");

            if (nodeGrave[idx] == null) // entirely new
            {
                Node newNode = Instantiate(nodePrefab, nodesParent);
                newNode.Init(idx);
                nodes[idx] = newNode;
                undirected[idx] = new HashSet<int>();

                nodes[idx].SetShape(shape);
            }
            else // bring back previously removed
            {
                nodes[idx] = nodeGrave[idx];
                nodeGrave.RemoveAt(idx);

                nodes[idx].Hide(false);
                nodes[idx].SetShape(shape); // technically not necessary
                nodes[idx].StressPos = UnityEngine.Random.insideUnitCircle; // to prevent possible infinities

                undirected[idx] = new HashSet<int>();
                foreach (int col in linkGrave.GetColumnIndicesInRow(idx))
                {
                    if (nodes[col] != null)
                    {
                        links[idx, col] = linkGrave[idx, col];
                        links[idx, col].gameObject.SetActive(true);
                        undirected[idx].Add(col);
                        undirected[col].Add(idx);
                    }
                }
                foreach (int row in linkGrave.GetRowIndicesInColumn(idx))
                {
                    if (nodes[row] != null)
                    {
                        links[row, idx] = linkGrave[row, idx];
                        links[row, idx].gameObject.SetActive(true);
                        undirected[idx].Add(row);
                        undirected[row].Add(idx);
                    }
                }
                // clear linkGrave
                foreach (int col in links.GetColumnIndicesInRow(idx)) {
                    linkGrave.RemoveAt(idx, col);
                }
                foreach (int row in links.GetRowIndicesInColumn(idx)) {
                    linkGrave.RemoveAt(row, idx);
                }
            }
            todoBFS.Enqueue(idx);
            layoutTriggered = true;
        }

        public void RemoveNode(int idx)
        {
            Assert.IsNotNull(nodes[idx], $"node {idx} not added yet");
            if (focusedNode != null && focusedNode.Idx == idx) {
                ForceUnfocus();
            }

            // move to graveyard to save for later
            nodes[idx].Hide(true);
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
            foreach (int col in linkGrave.GetColumnIndicesInRow(idx)) {
                links.RemoveAt(idx, col);
            }
            foreach (int row in linkGrave.GetRowIndicesInColumn(idx)) {
                links.RemoveAt(row, idx);
            }


            // prevent memory leak in SGD data structures
            undirected.Remove(idx);
            todoBFS.Clear();
            foreach (int i in undirected.Keys)
            {
                undirected[i].Remove(idx);
                todoBFS.Enqueue(i);
            }
            layoutTriggered = true;
        }        
        public void RemoveNodeCompletely(int idx)
        {
            Assert.IsNotNull(nodeGrave[idx], $"node {idx} not in graveyard");

            Destroy(nodeGrave[idx].gameObject);
            nodeGrave.RemoveAt(idx);

            foreach (int col in linkGrave.GetColumnIndicesInRow(idx).ToArray()) {
                linkGrave.RemoveAt(idx, col);
            }
            foreach (int row in linkGrave.GetRowIndicesInColumn(idx).ToArray()) {
                linkGrave.RemoveAt(row, idx);
            }
        }

        public void AddLink(int i, int j)
        {
            Assert.IsNull(links[i,j], $"link {i}:{j} already added");
            Assert.IsNull(links[j,i], "bidirectional links not allowed");

            Link newLink = Instantiate(linkPrefab, linksParent);
            newLink.Init(nodes[i], nodes[j]);
            links[i,j] = newLink;

            undirected[i].Add(j);
            undirected[j].Add(i);

            OnLinked?.Invoke();
            layoutTriggered = true;
        }
        public void RemoveLink(int i, int j)
        {
            Assert.IsNotNull(links[i,j], $"link {i}:{j} not added");

            Destroy(links[i,j].gameObject);
            links.RemoveAt(i,j);

            undirected[i].Remove(j);
            undirected[j].Remove(i);

            OnLinked?.Invoke();
            layoutTriggered = true;
        }

        public void SetIfNodeCanBeSource(int idx, bool canBeSource) // basal
        {
            nodes[idx].CanBeSource = canBeSource;
        }
        public void SetIfNodeCanBeTarget(int idx, bool canBeTarget) // apex predator
        {
            nodes[idx].CanBeTarget = canBeTarget;
        }
        public void SetIfNodeCanBeFocused(int idx, bool canBeFocused)
        {
            nodes[idx].CanBeFocused = canBeFocused;
        }
        public void SetIfLinkRemovable(int i, int j, bool removable)
        {
            links[i,j].Removable = removable;
        }

        // to give other classes access to the adjacency
        public IEnumerable<int> GetActiveTargets(int source)
        {
            return links.GetColumnIndicesInRow(source);
        }
        // for tutorial
        public int GetNodeChainLength(int idx)
        {
            Assert.IsTrue(chainLengths.ContainsKey(idx));
            return chainLengths[idx];
        }


        ////////////
        // effects

        public void SpawnEffectOnNode(int idx, GameObject effect)
        {
            Instantiate(effect, nodes[idx].transform);
        }
        public void SpawnEffectOnLink(int src, int tgt, GameObject effect)
        {
            var instantiated = Instantiate(effect, transform);
            instantiated.transform.position = (nodes[src].transform.position + nodes[tgt].transform.position) / 2;
            // TODO: better this
        }

        [SerializeField] float minLinkFlow, maxLinkFlow;
        public void ReflowLinks(Func<int, int, float> flows)
        {
            float flowRange = maxLinkFlow - minLinkFlow;
            foreach (Link li in links)
            {
                int res=li.Source.Idx, con=li.Target.Idx;
                float flow = flows(res, con);
                if (flow > 0) {
                    li.TileSpeed = minLinkFlow + flowRange*flow;
                } else {
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




        ///////////////
        // outlining
        public void OutlineNode(int idx, cakeslice.Outline.Colour colour)
        {
            nodes[idx].PushOutline(colour);
        }
        public void UnoutlineNode(int idx)
        {
            nodes[idx].PopOutline();
        }
        public void OutlineLink(int src, int tgt, cakeslice.Outline.Colour colour)
        {
            links[src, tgt].PushOutline(colour);
        }

        private List<Action> toUnoutline = new List<Action>();
        public void OutlineChain(cakeslice.Outline.Colour colour)
        {
            Assert.IsTrue(toUnoutline.Count == 0, "previous outline not undone");
            if (MaxChain == 0) {
                return;
            }
            StartCoroutine(WaitThenOutlineChain(colour));
        }
        public void OutlineLoop(cakeslice.Outline.Colour colour)
        {
            Assert.IsTrue(toUnoutline.Count == 0, "previous outline not undone");
            if (MaxLoop == 0) {
                return;
            }
            StartCoroutine(WaitThenOutlineLoop(colour));
        }
        private IEnumerator WaitThenOutlineChain(cakeslice.Outline.Colour colour)
        {
            // if calculating then we could potentially try to outline inactive or destroyed nodes
            while (isCalculatingAsync) {
                yield return null;
            }
            foreach (int idx in TallestNodes)
            {
                nodes[idx].PushOutline(colour);
                toUnoutline.Add(()=> nodes[idx].PopOutline());
            }
        }
        private IEnumerator WaitThenOutlineLoop(cakeslice.Outline.Colour colour)
        {
            // if calculating then we could potentially try to outline inactive or destroyed nodes
            while (isCalculatingAsync) {
                yield return null;
            }
            for (int i=0; i<LongestLoop.Count; i++)
            {
                var loopLink = links[LongestLoop[i], LongestLoop[(i+1)%LongestLoop.Count]];
                var loopNode = nodes[LongestLoop[i]];

                loopNode.PushOutline(colour);
                loopLink.PushOutline(colour);

                toUnoutline.Add(()=> loopNode.PopOutline());
                toUnoutline.Add(()=> loopLink.PopOutline());
            }
        }
        public void UnoutlineChainOrLoop()
        {
            foreach (Action Undo in toUnoutline) {
                Undo.Invoke();
            }
            toUnoutline.Clear();
        }


        // adds a gameobject like the jump shadow in mario bros.
        Transform shadowParent;
        public void AddDropShadow(GameObject shadow, float yPos)
        {
            Assert.IsNotNull(shadow, "shadow gameobject is null");
            if (shadowParent == null)
            {
                shadowParent = new GameObject().transform;
                shadowParent.name = "Shadow";
                shadowParent.SetParent(nodesParent);
            }
            shadowParent.localPosition = new Vector3(0,yPos,0);

            shadow.transform.SetParent(shadowParent, true);
        }

        public void TooltipNode(int idx, string msg)
        {
            tooltip.transform.position = mainCam.WorldToScreenPoint(nodes[idx].transform.position);
            tooltip.ShowText(msg);
            tooltip.Enable();
            tweenNodes = dragging = false;

            print("TODO: doesn't work if graph is spinning");
        }
        public void UntooltipNode(int idx)
        {
            tooltip.Disable();
            tweenNodes = dragging = true;
        }
        public void Finish()
        {
            ForceUnfocus();
            // StartCoroutine(TweenZoom(Vector3.one*1.2f, 2));
            // StartCoroutine(TweenPan(defaultNodelinkPos, 2));

            GetComponent<Collider>().enabled = false;
            foreach (var node in nodes)
            {
                node.GetComponent<Collider>().enabled = false;
            }
            print("TODO: more fun explody animation");
        }
    }
}