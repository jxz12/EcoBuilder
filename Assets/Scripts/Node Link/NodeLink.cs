using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using SparseMatrix;
using System;
using System.Collections;
using System.Collections.Generic;

namespace EcoBuilder.NodeLink
{
    public class NodeLink : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] Node nodePrefab;
        [SerializeField] Link linkPrefab;
        // [SerializeField] Mesh sphereMesh, sphereOutlineMesh, cubeMesh, cubeOutlineMesh;
        [SerializeField] Transform graphParent, nodesParent, linksParent;

        SparseVector<Node> nodes = new SparseVector<Node>();
        SparseMatrix<Link> links = new SparseMatrix<Link>();

        public void AddNode(int idx)
        {
            if (nodes[idx] != null)
                throw new Exception("already has idx " + idx);

            Node newNode;
            newNode = Instantiate(nodePrefab, nodesParent);

            var startPos = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f));
            newNode.Init(idx, startPos);
            nodes[idx] = newNode;

            adjacency[idx] = new HashSet<int>();
            toBFS.Enqueue(idx);
        }
        // public void ShapeNodeIntoCube(int idx)
        // {
        //     nodes[idx].SetShape(cubeMesh, cubeOutlineMesh);
        // }
        // public void ShapeNodeIntoSphere(int idx)
        // {
        //     nodes[idx].SetShape(sphereMesh, sphereOutlineMesh);
        // }
        public void RemoveNode(int idx)
        {
            if (focus != null && focus.Idx == idx)
                UnfocusAll();

            Destroy(nodes[idx].gameObject);
            nodes.RemoveAt(idx);

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

        public void AddLink(int i, int j)
        {
            Link newLink = Instantiate(linkPrefab, linksParent);
            newLink.Init(nodes[i], nodes[j]);
            links[i, j] = newLink;
            adjacency[i].Add(j);
            adjacency[j].Add(i);

            SGDStep = adjacency.Count * adjacency.Count; // adapt step size to new distance matrix
        }
        public void RemoveLink(int i, int j)
        {
            Destroy(links[i, j].gameObject);
            links.RemoveAt(i, j);
            adjacency[i].Remove(j);
            adjacency[j].Remove(i);

            SGDStep = adjacency.Count * adjacency.Count; // max shortest path squared so that mu=1
        }
        public void ColorNode(int idx, Color c)
        {
            nodes[idx].Col = c;
        }

        // public void ResizeNode(int idx, float size)
        // {
        //     if (size < 0)
        //         throw new Exception("size cannot be negative");

        //     nodes[idx].Size = .5f + Mathf.Sqrt(size); // to make area increase linearly with 'size'
        //     // TODO: do some pulse animation here!
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
        public void UnfocusAll()
        {
            focus = null;
        }
        public void FlashNode(int idx)
        {
            nodes[idx].Flash();
        }
        public void HeavyFlashNode(int idx)
        {
            nodes[idx].HeavyFlash();
        }
        public void UnflashNode(int idx)
        {
            nodes[idx].Idle();
        }

        bool laplacianDetNeg = false;
        public event Action LaplacianUnsolvable;
        public event Action LaplacianSolvable;
        private void FixedUpdate()
        {
            if (nodes.Count > 0)
            {
                bool solvable = UpdateTrophicEquations();
                if (solvable)
                {
                    if (laplacianDetNeg == true)
                    {
                        laplacianDetNeg = false;
                        LaplacianSolvable();
                    }
                    TrophicGaussSeidel();
                    TweenYAxis(v=>trophicLevels[v.Idx]-1, trophicStep);
                }
                else
                {
                    if (laplacianDetNeg == false)
                    {
                        laplacianDetNeg = true;
                        LaplacianUnsolvable();
                    }
                }

                int dq = toBFS.Dequeue(); // only do one vertex per frame
                if (dq == myNull)
                {
                    SGDStep = Mathf.Max(SGDStep * SGDMultiplier, minSGDStep); // decay step size
                    dq = toBFS.Dequeue();
                    toBFS.Enqueue(myNull);
                }

                var d_j = ShortestPathsBFS(dq);
                if (focus != null)
                {
                    if (focus.Idx == dq)
                    {
                        LayoutSGD(dq, d_j, focusStep);
                        CenteringSGD(dq, focusCenteringStep);
                    }
                    else
                    {
                        LayoutSGD(dq, d_j, SGDStep);
                    }
                }
                else
                {
                    LayoutSGD(dq, d_j, SGDStep);
                    CenteringSGD(dq, centeringStep);
                }
                toBFS.Enqueue(dq);

                Rotate();
            }
            // baseDisk.localScale = Vector3.Lerp(baseDisk.localScale, 2*Vector3.one, .1f);
        }

        ////////////////////////////////////
        // for user-interaction rotation

        [SerializeField] float rotationMultiplier=.9f, zoomMultiplier=.005f;
        [SerializeField] float yMinRotation=.4f, yRotationDrag=.1f;
        [SerializeField] float xDefaultRotation=-15, xRotationTween=.2f;

        // TODO: might want to change this into viewport coordinates
        [SerializeField] float clickRadius=100;

        public event Action<int> OnFocus;
        public event Action OnUnfocus;
        public void OnPointerClick(PointerEventData ped)
        {
            if (!ped.dragging)
            {
                Node closest = null;
                float closestDist = float.MaxValue;
                foreach (Node node in nodes)
                {
                    Vector2 screenPos = Camera.main.WorldToScreenPoint(node.transform.position);
                    // if the click is within the clickable radius 
                    if ((ped.position-screenPos).sqrMagnitude < clickRadius)
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
                if (closest != null)
                {
                    if (focus != closest)
                    {
                        FocusNode(closest.Idx);
                        OnFocus.Invoke(closest.Idx);
                    }
                }
                else
                {
                    UnfocusAll();
                    OnUnfocus.Invoke();
                }
            }
        }
        float yRotationMomentum = 0;
        bool dragging = false;
        public void OnBeginDrag(PointerEventData ped)
        {
            dragging = true;
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
        void Rotate()
        {
            if (!dragging)
            {
                yRotationMomentum += (yMinRotation - yRotationMomentum) * yRotationDrag;
                nodesParent.Rotate(Vector3.up, yRotationMomentum);

                var graphParentGoal = Quaternion.Euler(xDefaultRotation, 0, 0);
                var lerped = Quaternion.Lerp(graphParent.transform.localRotation, graphParentGoal, xRotationTween);
                graphParent.transform.localRotation = lerped;
            }
        }


        /////////////////////////////////
        // for stress-based layout

        [SerializeField] float SGDStep=.1f, SGDMultiplier=.5f, minSGDStep=.1f;
        [SerializeField] float separationStep=1, focusStep=4;
        [SerializeField] float centeringStep=.05f, focusCenteringStep=1;

        private Dictionary<int, HashSet<int>> adjacency = new Dictionary<int, HashSet<int>>();
        private Queue<int> toBFS = new Queue<int>(new int[]{ myNull });
        private static readonly int myNull = int.MinValue;

        private Dictionary<int, int> visited; // ugly, but reuse here to ease GC
        private Dictionary<int, int> ShortestPathsBFS(int source)
        {
            if (visited == null)
                visited = new Dictionary<int, int>();
            else
                visited.Clear();

            visited[source] = 0;
            var q = new Queue<int>();
            q.Enqueue(source);
            q.Enqueue(myNull); // use this as null value
            int depth = 1;

            while (q.Count > 1) // will always have one myNull somewhere
            {
                int current = q.Dequeue();
                if (current == myNull) // we have reached the end of this depth level
                {
                    q.Enqueue(myNull);
                    depth += 1;
                }
                else
                {
                    foreach (int next in adjacency[current])
                    {
                        if (!visited.ContainsKey(next))
                        {
                            q.Enqueue(next);
                            visited[next] = depth;
                        }
                    }
                }
            }
            return visited;
        }

        // SGD
        private void LayoutSGD(int i, Dictionary<int, int> d_j, float eta)
        {
            foreach (int j in nodes.Indices) // no shuffle, TODO: add later
            {
                if (i != j)
                {
                    Vector3 X_ij = nodes[i].TargetPos - nodes[j].TargetPos;
                    float mag = X_ij.magnitude;

                    if (d_j.ContainsKey(j)) // if there is a path between the two
                    {
                        int d_ij = d_j[j];
                        float mu = Mathf.Min(eta * (1f/(d_ij*d_ij)), 1); // w = 1/d^2

                        Vector3 r = ((mag-d_ij)/2) * (X_ij/mag);
                        // r.y = 0; // use to keep y position
                        nodes[i].TargetPos -= mu * r;
                        nodes[j].TargetPos += mu * r;
                    }
                    else // otherwise try to move the vertices at least a distance of 2 away
                    {
                        if (mag < 2) // only push away
                        {
                            // float mu = Mathf.Min(eta * .25f, 1); // w = 1/d^2 = 1/2^2 = .25
                            float mu = Mathf.Min(separationStep*.25f, 1);

                            Vector3 r = ((mag-2)/2) * (X_ij/mag);
                            // r.y = 0; // use to keep y position
                            nodes[i].TargetPos -= mu * r;
                            nodes[j].TargetPos += mu * r;
                        }
                    }
                }
            }
        }
        private void CenteringSGD(int i, float eta)
        {
            var fromCenter = new Vector3(nodes[i].TargetPos.x, 0, nodes[i].TargetPos.z);
            nodes[i].TargetPos -= eta * fromCenter;
        }

        ////////////////////////////////////
        // for trophic level calculation

        [SerializeField] float trophicStep=.05f;

        private SparseVector<float> trophicA = new SparseVector<float>(); // we can assume all matrix values are equal, so only need a vector
        private SparseVector<float> trophicLevels = new SparseVector<float>();

        // update the system of linear equations (Laplacian), return whether solvable
        private bool UpdateTrophicEquations()
        {
            foreach (Node no in nodes)
                trophicA[no.Idx] = 0;

            foreach (Link li in links)
            {
                int res = li.Source.Idx, con = li.Target.Idx;
                // if (links[li.Target.Idx, li.Source.Idx] == null) // prevent intraguild predation (TODO: MAY NOT BE NECESSARY)
                    trophicA[con] += 1f;
            }

            var basal = new HashSet<int>();
            foreach (Node no in nodes)
            {
                if (trophicA[no.Idx] != 0)
                    trophicA[no.Idx] = -1f / trophicA[no.Idx]; // ensures diagonal dominance
                else
                    basal.Add(no.Idx);
            }

            // returns whether determinant of the Laplacian is 0
            int cc = ConnectedComponentBFS(basal);
            if (cc != nodes.Count)
                return false;
            else
                return true;
        }

        // simplified gauss-seidel iteration because of the simplicity of the laplacian
        void TrophicGaussSeidel()
        {
            SparseVector<float> temp = new SparseVector<float>();
            foreach (Link li in links)
            {
                int res = li.Source.Idx, con = li.Target.Idx;
                // if (links[con, res] == null) // prevent intraguild predation
                    temp[con] += trophicA[con] * trophicLevels[res];
            }
            foreach (Node no in nodes)
            {
                trophicLevels[no.Idx] = (1 - temp[no.Idx]);
            }
        }

        void TweenYAxis(Func<Node, float> YAxisPos, float force)
        {
            foreach (Node no in nodes)
            {
                float targetY = YAxisPos(no);
                if (targetY == 0)
                {
                    no.TargetPos -= new Vector3(0, no.TargetPos.y, 0);
                }
                else
                {
                    float toAdd = (targetY - no.TargetPos.y) * force;
                    no.TargetPos += new Vector3(0, toAdd, 0);
                }
            }
        }

        // [SerializeField] float diskTween=.2f;
        // [SerializeField] Transform focusDisk, baseDisk;

        // void TweenDisk(Node target)
        // {
        //     if (target == null)
        //     {
        //         focusDisk.localPosition = Vector3.Lerp(focusDisk.localPosition, Vector3.zero, diskTween);
        //         focusDisk.localScale = Vector3.Lerp(focusDisk.localScale, 2*Vector3.one, diskTween);
        //     }
        //     else
        //     {
        //         float dist = focus.TargetPos.y - focusDisk.localPosition.y;
        //         // focusDisk.localPosition = Vector3.Lerp(focusDisk.localPosition, focus.transform.localPosition, focusDiskStep);
        //         focusDisk.localPosition = Vector3.Lerp(focusDisk.localPosition, focus.TargetPos, diskTween);
        //         focusDisk.localScale = Vector3.Lerp(focusDisk.localScale, Vector3.one, diskTween);
        //     }
        // }

        // private Stack<GameObject> disks = new Stack<GameObject>();
        // void SetCorrectYDisks(float maxY)
        // {
        //     // assume one disk always present
        //     int numDisks = disks.Count;
        //     if (maxY >= numDisks+1) // add disk
        //     {
        //         var newDisk = Instantiate(diskPrefab, nodesParent, false);
        //         newDisk.transform.localPosition = new Vector3(0,numDisks+1,0);
        //         disks.Push(newDisk);
        //     }
        //     else if (maxY < numDisks)
        //     {
        //         var oldDisk = disks.Pop();
        //         Destroy(oldDisk);
        //     }
        // }

        private int ConnectedComponentBFS(IEnumerable<int> sources)
        {
            var visited = new HashSet<int>();
            var q = new Queue<int>();
            foreach (int source in sources)
            {
                q.Enqueue(source);
                visited.Add(source);
            }
            while (q.Count != 0)
            {
                int current = q.Dequeue();
                foreach (int next in links.GetColumnIndicesInRow(current))
                {
                    if (!visited.Contains(next))
                    {
                        q.Enqueue(next);
                        visited.Add(next);
                    }
                }
            }
            return visited.Count;
        }
    }
}