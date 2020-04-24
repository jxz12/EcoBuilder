using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Collections;
using System.Collections.Generic;

#if !UNITY_WEBGL
using System.Threading.Tasks;
#endif

namespace EcoBuilder.NodeLink
{
    public partial class Graph : MonoBehaviour
    {
        // called regardless of user
        public event Action<int> OnFocused;
        public event Action<int> OnUnfocused;
        public event Action OnLayedOut;
        // public event Action OnLinked;

        // called only when user does something
        public event Action<int, int> OnUserLinked;
        public event Action<int, int> OnUserUnlinked;
        public event Action OnEmptyTapped;

        [SerializeField] Node nodePrefab;
        [SerializeField] Link linkPrefab;
        [SerializeField] Transform unfocusParent;

        Transform xAxle, yAxle;
        void Awake()
        {
            xAxle = new GameObject("X Axle").transform;
            xAxle.SetParent(transform);
            yAxle = new GameObject("Y Axle").transform;
            yAxle.SetParent(xAxle);
        }
        Camera mainCam;
        void Start()
        {
            mainCam = Camera.main;
            Assert.IsNotNull(mainCam);
            xRotation = xDefaultRotation = xAxle.localRotation.eulerAngles.x;
        }
        void Update()
        {
            if (layoutTriggered && !isCalculatingAsync)
            {
                layoutTriggered = false;
                Layout();
            }

            FineTuneLayout();
            if (tweenNodes)
            {
                TweenNodesToStress();
                TweenZoomToFit();
                MomentumRotate();
            }
        }

        private bool layoutTriggered = false;
        private bool isCalculatingAsync = false;
        public bool GraphLayedOut { get { return !layoutTriggered && !isCalculatingAsync; } }
        public bool ConstrainTrophic { private get; set; } = false;

        [SerializeField] int t_max=30;
        [SerializeField] float epsSGD=.01f, epsGS=.1f;

// because webgl does not support threads
#if !UNITY_WEBGL
        async void Layout()
#else
        void Layout()
#endif
        {
            isCalculatingAsync = true;

            NumEdges = links.Count();

            // these are not async to ensure synchronized adjacency
            Johnson.Init(nodes.Indices, links.GetColumnIndicesInRow);
            Trophic.Init(nodes.Indices, links.GetColumnIndicesInRow);
            SGD.Init(undirected);

#if !UNITY_WEBGL
            await Task.Run(()=> Johnson.SolveLoop());
            await Task.Run(()=> Trophic.SolveTrophic(epsGS));
            if (ConstrainTrophic) {
                await Task.Run(()=> SGD.SolveStress(t_max, epsSGD, Trophic.GetTrophicLevel));
            } else {
                await Task.Run(()=> SGD.SolveStress(t_max, epsSGD));
            }
            SGD.RewriteSGD((i,v)=>{ if (nodes[i]!=null) nodes[i].StressPos=v; }); // 'if' used in case node is deleted
#else
            Johnson.SolveLoop();
            Trophic.SolveTrophic(epsGS);
            if (ConstrainTrophic) {
                SGD.SolveStress(t_max, epsSGD, Trophic.GetTrophicLevel);
            } else {
                SGD.SolveStress(t_max, epsSGD);
            }
            SGD.RewriteSGD((i,v)=>nodes[i].StressPos=v);
#endif

            isCalculatingAsync = false;
            OnLayedOut?.Invoke();
            print(SGD.CalculateStress());
        }

        ///////////////////////////////////
        // structure changing functions

        // show versions for random access operations
        SparseVector<Node> nodes = new SparseVector<Node>();
        SparseMatrix<Link> links = new SparseMatrix<Link>();

        SparseVector<Node> nodeArchive = new SparseVector<Node>();
        SparseMatrix<Link> linkArchive = new SparseMatrix<Link>();

        Dictionary<int, HashSet<int>> undirected = new Dictionary<int, HashSet<int>>();

        public void AddNode(int idx)
        {
            Assert.IsNull(nodes[idx], $"node {idx} already added");

            if (nodeArchive[idx] == null) // entirely new
            {
                Node newNode = Instantiate(nodePrefab, yAxle);
                newNode.Init(idx);
                nodes[idx] = newNode;
                undirected[idx] = new HashSet<int>();
            }
            else // bring back previously removed
            {
                nodes[idx] = nodeArchive[idx];
                nodeArchive.RemoveAt(idx);

                nodes[idx].HideShape(false);
                nodes[idx].StressPos = UnityEngine.Random.insideUnitCircle; // to prevent divide-by-zero in majorization

                undirected[idx] = new HashSet<int>();
                foreach (int col in linkArchive.GetColumnIndicesInRow(idx))
                {
                    if (nodes[col] != null)
                    {
                        links[idx, col] = linkArchive[idx, col];
                        links[idx, col].gameObject.SetActive(true);
                        undirected[idx].Add(col);
                        undirected[col].Add(idx);
                    }
                }
                foreach (int row in linkArchive.GetRowIndicesInColumn(idx))
                {
                    if (nodes[row] != null)
                    {
                        links[row, idx] = linkArchive[row, idx];
                        links[row, idx].gameObject.SetActive(true);
                        undirected[idx].Add(row);
                        undirected[row].Add(idx);
                    }
                }
                // clear linkArchive
                foreach (int col in links.GetColumnIndicesInRow(idx)) {
                    linkArchive.RemoveAt(idx, col);
                }
                foreach (int row in links.GetRowIndicesInColumn(idx)) {
                    linkArchive.RemoveAt(row, idx);
                }
            }
            todoBFS.Enqueue(idx);
            layoutTriggered = true;
        }
        public void ShapeNode(int idx, GameObject shape)
        {
            nodes[idx].SetShape(shape);
        }
        // this function is needed for undo and redo functionality
        public void ArchiveNode(int idx)
        {
            Assert.IsNotNull(nodes[idx], $"node {idx} not added yet");
            if (focusedNode != null && focusedNode.Idx == idx) {
                ForceUnfocus();
            }

            // move to graveyard to save for later
            nodes[idx].HideShape(true);
            nodeArchive[idx] = nodes[idx];
            nodes.RemoveAt(idx);

            foreach (int col in links.GetColumnIndicesInRow(idx))
            {
                links[idx, col].gameObject.SetActive(false);
                linkArchive[idx, col] = links[idx, col];
            }
            foreach (int row in links.GetRowIndicesInColumn(idx))
            {
                links[row, idx].gameObject.SetActive(false);
                linkArchive[row, idx] = links[row, idx];
            }
            // clear links
            foreach (int col in linkArchive.GetColumnIndicesInRow(idx)) {
                links.RemoveAt(idx, col);
            }
            foreach (int row in linkArchive.GetRowIndicesInColumn(idx)) {
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
        public void RemoveNode(int idx)
        {
            Assert.IsNotNull(nodeArchive[idx], $"node {idx} must be archived first");

            Destroy(nodeArchive[idx].gameObject);
            nodeArchive.RemoveAt(idx);
            linkArchive.RemoveIndex(idx);
        }


        public void AddLink(int i, int j)
        {
            Assert.IsFalse(i==j, $"self links {i}:{j} not supported");
            Assert.IsNull(links[i,j], $"link {i}:{j} already added");
            Assert.IsNull(links[j,i], "bidirectional links not allowed");
            Assert.IsNotNull(nodes[i], $"node {i} not added");
            Assert.IsNotNull(nodes[j], $"node {j} not added");

            Link newLink = Instantiate(linkPrefab, transform);
            newLink.Init(nodes[i], nodes[j]);
            links[i,j] = newLink;

            undirected[i].Add(j);
            undirected[j].Add(i);

            layoutTriggered = true;
        }
        public void RemoveLink(int i, int j)
        {
            Assert.IsNotNull(links[i,j], $"link {i}:{j} not added");

            Destroy(links[i,j].gameObject);
            links.RemoveAt(i,j);

            undirected[i].Remove(j);
            undirected[j].Remove(i);

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
            return Trophic.GetChainLength(idx);
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

        [SerializeField] float minLinkFlow=.1f, maxLinkFlow=2;
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

        // chain and loop
        public int NumEdges { get; private set; } = 0;
        public int NumComponents { get { return SGD.NumComponents; } }
        public int MaxChain { get { return Trophic.MaxChain; } }
        public int NumMaxChain { get { return Trophic.NumMaxChain; } }
        public int MaxLoop { get { return Johnson.MaxLoop; } }
        public int NumMaxLoop { get { return Johnson.NumMaxLoop; } }

        private List<Action> toUnoutline = new List<Action>();
        public void OutlineChain(cakeslice.Outline.Colour colour)
        {
            Assert.IsTrue(toUnoutline.Count == 0, "previous outline not undone");
            StartCoroutine(WaitThenOutlineChain(colour));
        }
        public void OutlineLoop(cakeslice.Outline.Colour colour)
        {
            Assert.IsTrue(toUnoutline.Count == 0, "previous outline not undone");
            StartCoroutine(WaitThenOutlineLoop(colour));
        }
        private IEnumerator WaitThenOutlineChain(cakeslice.Outline.Colour colour)
        {
            if (nodes.Count == 0) {
                yield break;
            }
            // if calculating then we could potentially try to outline inactive or destroyed nodes
            while (isCalculatingAsync) {
                yield return null;
            }
            foreach (int idx in Trophic.MaxChainIndices)
            {
                nodes[idx].PushOutline(colour);
                toUnoutline.Add(()=> nodes[idx].PopOutline());
            }
        }
        private IEnumerator WaitThenOutlineLoop(cakeslice.Outline.Colour colour)
        {
            if (Johnson.MaxLoop == 0) {
                yield break;
            }
            // if calculating then we could potentially try to outline inactive or destroyed nodes
            while (isCalculatingAsync) {
                yield return null;
            }
            for (int i=0; i<Johnson.MaxLoop; i++)
            {
                int src = Johnson.MaxLoopIndices[i];
                int tgt = Johnson.MaxLoopIndices[(i+1) % Johnson.MaxLoop];
                var loopNode = nodes[src];
                var loopLink = links[src,tgt];

                loopNode.PushOutline(colour);
                loopLink.PushOutline(colour);
                toUnoutline.Add(()=> loopNode.PopOutline());
                toUnoutline.Add(()=> loopLink.PopOutline());
            }
        }
        public void UnoutlineChainOrLoop()
        {
            foreach (Action Undo in toUnoutline) {
                Undo?.Invoke();
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
                shadowParent.SetParent(yAxle);
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
        }
        public void UntooltipNode(int idx)
        {
            tooltip.Disable();
            tweenNodes = dragging = true;
        }
        public void Finish()
        {
            ForceUnfocus();

            GetComponent<Collider>().enabled = false;
            foreach (var node in nodes)
            {
                node.GetComponent<Collider>().enabled = false;
            }
        }
    }
}