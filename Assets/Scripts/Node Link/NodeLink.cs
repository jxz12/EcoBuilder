using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using SparseMatrix;
using System;
// using System.Linq;
using System.Collections.Generic;

namespace EcoBuilder.NodeLink
{
    public class NodeLink : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Serializable] class IntEvent : UnityEvent<int> { };

        [SerializeField] UnityEvent OnUnfocus;
        [SerializeField] IntEvent OnFocus;

        [SerializeField] Node nodePrefab;
        [SerializeField] GameObject diskPrefab;
        [SerializeField] Mesh sphereMesh, sphereOutlineMesh, cubeMesh, cubeOutlineMesh;
        [SerializeField] Link linkPrefab;

        SparseVector<Node> nodes = new SparseVector<Node>();
        SparseMatrix<Link> links = new SparseMatrix<Link>();

        [SerializeField] Transform graphParent, nodesParent, linksParent;

        public void AddNode(int idx)
        {
            if (nodes[idx] != null)
                throw new Exception("already has idx " + idx);

            Node newNode;
            newNode = Instantiate(nodePrefab, nodesParent);

            newNode.Init(idx);
            // newNode.Pos = new Vector3(UnityEngine.Random.Range(-1f, 1f), 1, UnityEngine.Random.Range(-1f, 1f));
            newNode.Pos = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1, 1f), UnityEngine.Random.Range(-1f, 1f));
            nodes[idx] = newNode;

            adjacency[idx] = new HashSet<int>();
            toBFS.Enqueue(idx);
        }
        public void ShapeNodeIntoCube(int idx)
        {
            nodes[idx].SetShape(cubeMesh, cubeOutlineMesh);
        }
        public void ShapeNodeIntoSphere(int idx)
        {
            nodes[idx].SetShape(sphereMesh, sphereOutlineMesh);
        }
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
        }
        public void RemoveLink(int i, int j)
        {
            Destroy(links[i, j].gameObject);
            links.RemoveAt(i, j);
            adjacency[i].Remove(j);
            adjacency[j].Remove(i);
        }
        public void ColorNode(int idx, Color c)
        {
            nodes[idx].Col = c;
        }
        public void ResizeNode(int idx, float size)
        {
            // if (size < 0 || size > 1)
            //     throw new Exception("not normalized");

            nodes[idx].Size = size; // do some tweening here!
        }
        public void ResizeEdge(int i, int j, float size)
        {
            links[i,j].Size = size;
        }

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
        public void UnflashNode(int idx)
        {
            nodes[idx].Idle();
        }

        private void Update()
        {
            if (nodes.Count < 1)
                return;

            int dq = toBFS.Dequeue(); // only do one vertex per frame
            var d_j = ShortestPathsBFS(dq);
            if (focus != null && focus.Idx == dq)
            {
                LayoutSGD(dq, d_j, focusStep);
                CenteringSGD(dq, focusCenteringStep);
            }
            else
            {
                LayoutSGD(dq, d_j, SGDStep);
                CenteringSGD(dq, centeringStep);
            }

            toBFS.Enqueue(dq);

            bool solvable = UpdateTrophicEquations();
            if (solvable)
                TrophicGaussSeidel();
            else
                print("LAPLACIAN DET=0"); //TODO: replace this with a warning message

            SetYAxis(i=>trophicLevels[i]-1, trophicStep);

            Rotate();
        }

        ////////////////////////////////////
        // for user-interaction rotation

        [SerializeField] float rotationMultiplier=.9f, yMinRotation=.4f, yRotationDrag=.1f, xRotationForce=15;
        [SerializeField] float zoomMultiplier=.005f;
        [SerializeField] float clickRadius=100; // TODO: might want to change this into viewport coordinates
        void Rotate()
        {
            if (!dragging)
            {
                yRotationMomentum += (yMinRotation - yRotationMomentum) * yRotationDrag;
                nodesParent.Rotate(Vector3.up, yRotationMomentum);

                float xRotation = -graphParent.localRotation.x * xRotationForce;
                graphParent.Rotate(Vector3.right, xRotation);
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
                // TODO: make SGD step go up when shaken
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

        /////////////////////////////////
        // for stress-based layout

        [SerializeField] float SGDStep=.2f;
        [SerializeField] float separationStep, focusStep, centeringStep, focusCenteringStep;

        private Dictionary<int, HashSet<int>> adjacency = new Dictionary<int, HashSet<int>>();
        private Queue<int> toBFS = new Queue<int>();

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
            int myNull = int.MinValue;
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
                    Vector3 X_ij = nodes[i].Pos - nodes[j].Pos;
                    float mag = X_ij.magnitude;

                    if (d_j.ContainsKey(j)) // if there is a path between the two
                    {
                        int d_ij = d_j[j];
                        float mu = Mathf.Min(eta * (1f/(d_ij*d_ij)), 1); // w = 1/d^2

                        Vector3 r = ((mag-d_ij)/2) * (X_ij/mag);
                        // r.y = 0; // use to keep y position
                        nodes[i].Pos -= mu * r;
                        nodes[j].Pos += mu * r;
                    }
                    else // otherwise try to move the vertices at least a distance of 2 away
                    {
                        if (mag < 2) // only push away
                        {
                            // float mu = Mathf.Min(eta * .25f, 1); // w = 1/d^2 = 1/2^2 = .25
                            float mu = Mathf.Min(separationStep*.25f, 1);

                            Vector3 r = ((mag-2)/2) * (X_ij/mag);
                            // r.y = 0; // use to keep y position
                            nodes[i].Pos -= mu * r;
                            nodes[j].Pos += mu * r;
                        }
                    }
                }
            }
        }
        private void CenteringSGD(int i, float eta)
        {
            var fromCenter = new Vector3(nodes[i].Pos.x, 0, nodes[i].Pos.z);
            nodes[i].Pos -= eta * fromCenter;
        }

        ////////////////////////////////////
        // for trophic level calculation

        [SerializeField] float trophicStep;

        private SparseVector<float> trophicA = new SparseVector<float>(); // we can assume all matrix values are equal, so only need a vector
        private SparseVector<float> trophicLevels = new SparseVector<float>();

        // update the system of linear equations (Laplacian)
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

        void SetYAxis(Func<int, float> YAxisPos, float force)
        {
            foreach (Node no in nodes)
            {
                float targetY = YAxisPos(no.Idx);
                if (targetY == 0)
                {
                    no.Pos -= new Vector3(0, no.Pos.y, 0);
                }
                else
                {
                    float toAdd = (targetY - no.Pos.y) * force;
                    no.Pos += new Vector3(0, toAdd, 0);
                }
            }
        }

        private Stack<GameObject> disks = new Stack<GameObject>();
        void SetCorrectYDisks(float maxY)
        {
            // assume one disk always present
            int numDisks = disks.Count;
            if (maxY >= numDisks+1) // add disk
            {
                var newDisk = Instantiate(diskPrefab, nodesParent, false);
                newDisk.transform.localPosition = new Vector3(0,numDisks+1,0);
                disks.Push(newDisk);
            }
            else if (maxY < numDisks)
            {
                var oldDisk = disks.Pop();
                Destroy(oldDisk);
            }
        }

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