using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using SparseMatrix;
using System;
using System.Collections;
using System.Collections.Generic;

namespace EcoBuilder.NodeLink
{
    public class NodeLink : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField] Node nodePrefab;
        [SerializeField] Link linkPrefab;
        // [SerializeField] Mesh sphereMesh, cubeMesh;
        // [SerializeField] Mesh sphereOutline, cubeOutline;
        [SerializeField] Transform graphParent, nodesParent, linksParent;

        SparseVector<Node> nodes = new SparseVector<Node>();
        SparseMatrix<Link> links = new SparseMatrix<Link>();
        Dictionary<int, HashSet<int>> adjacency = new Dictionary<int, HashSet<int>>();

        public void AddNode(int idx, GameObject shape)
        {
            if (nodes[idx] != null)
                throw new Exception("already has idx " + idx);

            Node newNode;
            newNode = Instantiate(nodePrefab, nodesParent);

            var startPos = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f));
            newNode.Init(idx, startPos, shape);
            nodes[idx] = newNode;

            adjacency[idx] = new HashSet<int>();
            toBFS.Enqueue(idx);
        }
        // public void ShapeNodeIntoCube(int idx)
        // {
        //     nodes[idx].SetShape(cubeMesh, cubeOutline);
        // }
        // public void ShapeNodeIntoSphere(int idx)
        // {
        //     nodes[idx].SetShape(sphereMesh, sphereOutline);
        // }
        public void RemoveNode(int idx)
        {
            // if (focus != null && focus.Idx == idx)
            //     UnfocusAll();

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
            // toBFS.Enqueue(myNull);

            // prevent memory leak in trophic level data structures
            trophicA.RemoveAt(idx);
            trophicLevels.RemoveAt(idx);
        }

        public void AddLink(int i, int j)
        {
            Link newLink = Instantiate(linkPrefab, linksParent);
            newLink.Init(nodes[i], nodes[j], true);
            links[i, j] = newLink;

            adjacency[i].Add(j);
            adjacency[j].Add(i);

            // if (links[j, i] != null)
            // {
            //     links[j, i].Curve();
            //     links[i, j].Curve();
            // }
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
        }
        public IEnumerable<Tuple<int, int>> GetLinks()
        {
            foreach (var ij in links.IndexPairs)
            {
                yield return ij;
            }
        }
        public void ReshapeNode(int idx, GameObject shape)
        {
            nodes[idx].Reshape(shape);
        }

        public void ResizeNode(int idx, float size)
        {
            if (size < 0)
                throw new Exception("size cannot be negative");

            nodes[idx].Size = .5f + Mathf.Sqrt(size); // to make area increase linearly with 'size'
        }
        // public void ResizeNodeOutline(int idx, float size)
        // {
        //     if (size < 0)
        //         throw new Exception("size cannot be negative");

        //     nodes[idx].OutlineSize = .5f + Mathf.Sqrt(size);
        // }

        // [SerializeField] float maxEdgeWidth;
        // public void ResizeEdge(int i, int j, float width)
        // {
        //     links[i,j].Width = .2f + maxEdgeWidth*width;
        //     // TODO: some pulse here too
        // }

        Node focus=null;
        public void FocusNode(int idx)
        {
            focus = nodes[idx];
        }
        public void Unfocus()
        {
            focus = null;
        }
        public void FlashNode(int idx)
        {
            nodes[idx].Flash();
        }
        public void IdleNode(int idx)
        {
            nodes[idx].Idle();
        }
        // public void HeavyFlashNode(int idx)
        // {
        //     nodes[idx].HeavyFlash();
        // }

        ////////////////////////////////////
        // for user-interaction rotation

        public event Action<int> OnNodeHeld;
        public event Action<int> OnNodeClicked;
        public event Action OnEmptyClicked;

        [SerializeField] float rotationMultiplier=.9f, zoomMultiplier=.005f;
        [SerializeField] float yMinRotation=.4f, yRotationDrag=.1f;
        [SerializeField] float xDefaultRotation=-15, xRotationTween=.2f;

        // TODO: might want to change this into viewport coordinates
        [SerializeField] float clickRadius=15;
        private Node ClosestNodeToPointer(Vector2 pointerPos)
        {
            Node closest = null;
            float closestDist = float.MaxValue;
            float radius = clickRadius * clickRadius;
            foreach (Node node in nodes)
            {
                Vector2 screenPos = Camera.main.WorldToScreenPoint(node.transform.position);

                // if the click is within the clickable radius 
                if ((pointerPos-screenPos).sqrMagnitude < radius)
                {
                    // choose the node closer to the screen
                    float dist = (Camera.main.transform.position - node.transform.position).sqrMagnitude;
                    if (dist < closestDist)
                    {
                        closest = node;
                        closestDist = dist;
                    }
                }
            }
            return closest;
        }

        [SerializeField] float holdThreshold = .5f;
        bool potentialHold = false;
        bool dragging = false;
        public void OnPointerDown(PointerEventData ped)
        {
            potentialHold = true;
            dragging = true;
            StartCoroutine(WaitForHold(holdThreshold, ped));
        }
        // TODO: if nothing is selected, then make even a normal click select
        IEnumerator WaitForHold(float seconds, PointerEventData ped)
        {
            float endTime = Time.time + seconds;
            while (Time.time < endTime)
            {
                if (potentialHold == false)
                    yield break;
                else
                    yield return null;
            }
            potentialHold = false;
            Node held = ClosestNodeToPointer(ped.position);
            if (held != null)
            {
                OnNodeHeld.Invoke(held.Idx);
            }
        }
        public void OnPointerUp(PointerEventData ped)
        {
            if (potentialHold)
            {
                potentialHold = false;
                Node clicked = ClosestNodeToPointer(ped.position);
                if (clicked != null)
                    OnNodeClicked.Invoke(clicked.Idx);
                else
                    OnEmptyClicked.Invoke();
            }
            dragging = false;
        }

        float yRotationMomentum = 0;
        public void OnBeginDrag(PointerEventData ped)
        {
            potentialHold = false;
        }
        public void OnEndDrag(PointerEventData ped)
        {
            yRotationMomentum = -ped.delta.x * rotationMultiplier;
            dragging = false;
        }
        public void OnDrag(PointerEventData ped)
        {
            if (ped.button == PointerEventData.InputButton.Left)
            {
                float ySpin = -ped.delta.x * rotationMultiplier;
                nodesParent.Rotate(Vector3.up, ySpin);
                yRotationMomentum = ySpin;
                yMinRotation = Mathf.Abs(yMinRotation) * Mathf.Sign(yRotationMomentum);

                float xSpin = ped.delta.y * rotationMultiplier;
                graphParent.Rotate(Vector3.right, xSpin);
            }
            // TODO: make this two fingers instead
            else if (ped.button == PointerEventData.InputButton.Right)
            {
                float zoom = ped.delta.y * zoomMultiplier;
                if (zoom > .5f)
                    zoom = .5f;
                if (zoom < -.5f)
                    zoom = -.5f;
                nodesParent.localScale *= 1 + zoom;
            }
        }
        public event Action OnDroppedOn;
        public void OnDrop(PointerEventData ped)
        {
            if (ped.pointerDrag != this.gameObject)
                OnDroppedOn.Invoke();
        }
        private void Rotate()
        {
            yRotationMomentum += (yMinRotation - yRotationMomentum) * yRotationDrag;
            nodesParent.Rotate(Vector3.up, yRotationMomentum);

            var graphParentGoal = Quaternion.Euler(xDefaultRotation, 0, 0);
            var lerped = Quaternion.Lerp(graphParent.transform.localRotation, graphParentGoal, xRotationTween);
            graphParent.transform.localRotation = lerped;
        }


        /////////////////////////////////
        // for stress-based layout

        [SerializeField] float SGDStep=.2f;
        [SerializeField] float separationStep=1;

        private Queue<int> toBFS = new Queue<int>();

        // SGD
        private void LayoutSGD(int i, Dictionary<int, int> d_j)
        {
            foreach (int j in FYShuffle(nodes.Indices))
            {
                if (i != j)
                {
                    Vector3 X_ij = nodes[i].TargetPos - nodes[j].TargetPos;
                    float mag = X_ij.magnitude;

                    if (d_j.ContainsKey(j)) // if there is a path between the two
                    {
                        int d_ij = d_j[j];
                        float mu = Mathf.Min(SGDStep * (1f/(d_ij*d_ij)), 1); // w = 1/d^2

                        Vector3 r = ((mag-d_ij)/2) * (X_ij/mag);
                        r.y = 0; // use to keep y position
                        nodes[i].TargetPos -= mu * r;
                        nodes[j].TargetPos += mu * r;
                    }
                    else // otherwise try to move the vertices at least a distance of 2 away
                    {
                        if (mag < 1) // only push away
                        {
                            float mu = Mathf.Min(separationStep, 1);

                            Vector3 r = ((mag-1)/2) * (X_ij/mag);
                            r.y = 0; // use to keep y position
                            nodes[i].TargetPos -= mu * r;
                            nodes[j].TargetPos += mu * r;
                        }
                    }
                }
                // nodes[i].TargetPos += jitterStep * UnityEngine.Random.insideUnitSphere;
            }
        }

        private Dictionary<int, int> ShortestPathsBFS(int source)
        {
            var visited = new Dictionary<int, int>(); // ugly, but reuse here to ease GC

            visited[source] = 0;
            var q = new Queue<int>();
            q.Enqueue(source);

            while (q.Count > 0)
            {
                int current = q.Dequeue();
                foreach (int next in adjacency[current])
                {
                    if (!visited.ContainsKey(next))
                    {
                        q.Enqueue(next);
                        visited[next] = visited[current] + 1;
                    }
                }
            }
            return visited;
        }

        public static IEnumerable<int> FYShuffle(IEnumerable<int> toShuffle)
        {
            var shuffled = new List<int>();
            foreach (int i in toShuffle)
                shuffled.Add(i);
            
            int n = shuffled.Count;
            for (int i=0; i<n-1; i++)
            {
                int j = UnityEngine.Random.Range(i, n);
                // i = j
                yield return shuffled[j];
                // then j = i
                shuffled[j] = shuffled[i];
            }
            yield return shuffled[n-1];
        }

        [SerializeField] float layoutTween=.05f;//, centeringTween=.1f;
        void TweenNodes()
        {
            Vector3 centroid = Vector3.zero;
            if (focus == null)
            {
                // get average of all positions, and center
                foreach (Node no in nodes)
                {
                    Vector3 pos = no.TargetPos;
                    pos.y = 0;
                    centroid += pos;
                }
                centroid /= nodes.Count;
            }
            else
            {
                // center to focus
                centroid = focus.TargetPos;
                centroid.y = 0;
            }
            foreach (Node no in nodes)
            {
                no.TargetPos -= centroid;// * centeringTween;
                no.transform.localPosition =
                    Vector3.Lerp(no.transform.localPosition, no.TargetPos, layoutTween);
            }
        }

        ////////////////////////////////////
        // for trophic level calculation

        private SparseVector<float> trophicA = new SparseVector<float>(); // we can assume all matrix values are equal, so only need a vector
        private SparseVector<float> trophicLevels = new SparseVector<float>();

        // update the system of linear equations (Laplacian)
        // return set of roots to the tree
        private HashSet<int> BuildTrophicEquations()
        {
            foreach (int i in nodes.Indices)
                trophicA[i] = 0;

            foreach (var ij in links.IndexPairs)
            {
                int res = ij.Item1, con = ij.Item2;
                trophicA[con] += 1f;
            }

            var basal = new HashSet<int>();
            foreach (int i in nodes.Indices)
            {
                if (trophicA[i] != 0)
                    trophicA[i] = -1f / trophicA[i]; // ensures diagonal dominance
                else
                    basal.Add(i);
            }
            return basal;
        }

        // simplified gauss-seidel iteration because of the simplicity of the laplacian
        void TrophicGaussSeidel()
        {
            SparseVector<float> temp = new SparseVector<float>();
            foreach (var ij in links.IndexPairs)
            {
                int res = ij.Item1, con = ij.Item2;
                temp[con] += trophicA[con] * trophicLevels[res];
            }
            foreach (int i in nodes.Indices)
            {
                trophicLevels[i] = (1 - temp[i]);
            }
        }
        public void TrophicGaussSeidel(int iter)
        {
            for (int i=0; i<iter; i++)
            {
                TrophicGaussSeidel();
            }
        }

        public float MaxTrophicHeight()
        {
            float max = 0;
            foreach (float trophicLevel in trophicLevels)
            {
                if (trophicLevel > max)
                    max = trophicLevel;
            }
            return max;
        }

        private Dictionary<int, int> HeightBFS(IEnumerable<int> sources)
        {
            var visited = new Dictionary<int, int>();
            var q = new Queue<int>();
            foreach (int source in sources)
            {
                q.Enqueue(source);
                visited[source] = 0;
            }

            while (q.Count > 0)
            {
                int current = q.Dequeue();
                foreach (int next in links.GetColumnIndicesInRow(current))
                {
                    if (!visited.ContainsKey(next))
                    {
                        q.Enqueue(next);
                        visited[next] = visited[current] + 1;
                    }
                }
            }
            return visited;
        }

        ///////////////////////////
        // for loops

        public bool LoopExists()
        {
            throw new Exception("not implemented");
        }
        public bool LoopExists(int length)
        {
            throw new Exception("not implemented");
        }

        /////////////////////////
        // for trophic coherence

        public float CalculateOmnivory()
        {
            int L = 0;
            float sum_x_sqr = 0;
            foreach (Link li in links)
            {
                L += 1;
                int res = li.Source.Idx, con = li.Target.Idx;
                float x = trophicLevels[res] - trophicLevels[con];
                sum_x_sqr += x * x;
            }
            return Mathf.Sqrt(sum_x_sqr - 1);
        }

        bool laplacianDetNeg = false;
        public event Action LaplacianUnsolvable;
        public event Action LaplacianSolvable;
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
                        LaplacianSolvable();
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
                        LaplacianUnsolvable();
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

    }
}