using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using SparseMatrix;
using System;
using System.Linq;
using System.Collections.Generic;

namespace EcoBuilder.NodeLink
{
    public class NodeLink : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Serializable] class IntEvent : UnityEvent<int> { };
        [SerializeField] IntEvent NodeInspectedEvent;
        [SerializeField] Node nodePrefab;
        [SerializeField] GameObject diskPrefab;
        [SerializeField] Mesh sphereMesh, sphereOutlineMesh, cubeMesh, cubeOutlineMesh;
        [SerializeField] Link linkPrefab;

        [SerializeField] float stepSize=.2f, centeringForce=.01f, trophicForce=.5f;
        [SerializeField] float rotationMultiplier=.9f, yMinRotation=.4f, yRotationDrag=.1f, xRotationForce=15;
        [SerializeField] float zoomMultiplier=.005f;

        SparseVector<Node> nodes = new SparseVector<Node>();
        SparseMatrix<Link> links = new SparseMatrix<Link>();

        [SerializeField] Transform graphParent, nodesParent, linksParent, disksParent;

        public void AddNode(int idx)
        {
            Node newNode;
            newNode = Instantiate(nodePrefab, nodesParent);

            newNode.Init(idx);
            newNode.Pos = new Vector3(UnityEngine.Random.Range(-1f, 1f), 1, UnityEngine.Random.Range(-1f, 1f));
            nodes[idx] = newNode;
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
            if (inspected != null && inspected.Idx == idx)
                Uninspect();

            Destroy(nodes[idx].gameObject);
            nodes.RemoveAt(idx);

            var toRemove = new List<Tuple<int,int>>();
            foreach (int other in links.GetColumnIndicesInRow(idx))
                toRemove.Add(Tuple.Create(idx, other));
            foreach (int other in links.GetRowIndicesInColumn(idx))
                toRemove.Add(Tuple.Create(other, idx));

            foreach (var ij in toRemove)
                RemoveLink(ij.Item1, ij.Item2);
        }

        public void AddLink(int i, int j)
        {
            Link newLink = Instantiate(linkPrefab, linksParent);
            newLink.Init(nodes[i], nodes[j]);
            links[i, j] = newLink;
        }
        public void RemoveLink(int i, int j)
        {
            Destroy(links[i, j].gameObject);
            links.RemoveAt(i, j);
        }
        public void ColorNode(int idx, Color c)
        {
            nodes[idx].Col = c;
        }
        public void ResizeNode(int idx, float size)
        {
            if (size < 0 || size > 1)
                throw new Exception("not normalized");

            nodes[idx].Size = .5f + size; // do some tweening here!
        }
        public void ResizeEdge(int i, int j, float size)
        {
            links[i,j].Size = size;
        }

        Node inspected=null;
        public void InspectNode(int idx)
        {
            if (inspected != null && inspected != nodes[idx])
                inspected.Uninspect();

            nodes[idx].Inspect();
            inspected = nodes[idx];
        }
        public void Uninspect()
        {
            if (inspected != null)
                inspected.Uninspect();
            
            inspected = null;
        }
        public void FlashNode(int idx)
        {
            print("Warning: " + idx);
        }
        public void HeavyflashNode(int idx)
        {
            print("AHHHHHH: " + idx);
        }
        public void UnflashNode(int idx)
        {
            print("Phew: " + idx);
        }

        private void Update()
        {
            if (nodes.Count < 1)
                return;

            // checks whether there is a directed path to every species from basal
            HashSet<int> basal = UpdateTrophicEquations();
            int componentSize = ConnectedComponentBFS(basal);
            if (componentSize==nodes.Count)
            {
                float maxTrophicLevel = TrophicGaussSeidel();
                SetYAxis(i=>trophicLevels[i]-1);             
                SetCorrectTrophicDisks(maxTrophicLevel);
            }
            else
            {
                print("asdasda"); // TODO: change this to a warning or something
                return;
            }

            Rotate();
            LayoutSGD(stepSize);
        }

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

        // TODO: add zooming with pinch
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


        private SparseVector<float> trophicA = new SparseVector<float>(); // we can assume all matrix values are equal, so only need a vector
        private SparseVector<float> trophicLevels = new SparseVector<float>();

        // returns a set of basal species
        HashSet<int> UpdateTrophicEquations()
        {
            // update the system of linear equations
            foreach (Node no in nodes)
                trophicA[no.Idx] = 0;

            foreach (Link li in links)
                trophicA[li.Target.Idx] += 1f; // add one to the consumer's row for every resource it has

            var basal = new HashSet<int>();
            foreach (Node no in nodes)
            {
                if (trophicA[no.Idx] != 0)
                    trophicA[no.Idx] = -1f / trophicA[no.Idx]; // invert, ensures diagonal dominance
                else
                    basal.Add(no.Idx);
            }
            
            return basal;
        }

        float TrophicGaussSeidel()
        {
            SparseVector<float> temp = new SparseVector<float>();
            foreach (Link li in links)
            {
                int resource = li.Source.Idx, consumer = li.Target.Idx;
                temp[consumer] += trophicA[consumer] * trophicLevels[resource];
            }
            float maxTrophicLevel = 0;
            foreach (Node no in nodes)
            {
                trophicLevels[no.Idx] = (1 - temp[no.Idx]);
                maxTrophicLevel = Math.Max(maxTrophicLevel, trophicLevels[no.Idx]);
            }
            return maxTrophicLevel;
        }

        void SetYAxis(Func<int, float> YAxisPos)
        {
            foreach (Node no in nodes)
            {
                float targetY = YAxisPos(no.Idx);
                float toAdd = (targetY - no.Pos.y) * trophicForce;
                
                no.Pos += new Vector3(0, toAdd, 0);
            }
        }
        Stack<GameObject> disks = new Stack<GameObject>();
        void SetCorrectTrophicDisks(float maxTrophicLevel)
        {
            // always assume one disk always present

            int numDisks = disks.Count;
            if (maxTrophicLevel >= numDisks+2) // add disk
            {
                var newDisk = Instantiate(diskPrefab, disksParent, false);
                newDisk.transform.localPosition = new Vector3(0,numDisks+1,0);
                disks.Push(newDisk);
            }
            else if (maxTrophicLevel < numDisks+1)
            {
                var oldDisk = disks.Pop();
                Destroy(oldDisk);
            }
        }


        // SGD
        private void LayoutSGD(float eta)
        {
            if (eta < 0)
                return;

            // no shuffle, could add later
            foreach (int i in nodes.Indices)
            {
                Vector3 X_i = nodes[i].Pos;
                foreach (int j in nodes.Indices)
                {
                    if (i < j)
                    {
                        Vector3 X_j = nodes[j].Pos;
                        Vector3 X_ij = X_i - X_j;
                        float mag = X_ij.magnitude;
                        // float d_ij = 1 + (nodes[i].Size-1) + (nodes[j].Size-1);

                        if (links[i,j] != null || links[j,i] != null) // if connected, do normal SGD
                        {
                            float mu = Mathf.Min(eta, 1); // w = 1/d^2 = 1/1
                            // Vector3 r = ((mag-d_ij)/2) * (X_ij/mag);
                            Vector3 r = ((mag-1)/2) * (X_ij/mag);

                            r.y = 0; // keep y position
                            // if (inspected != null && (inspected.Idx==i || inspected.Idx==j))
                            // {
                            //     // focus
                            //     nodes[i].Pos -= r;
                            //     nodes[j].Pos += r;
                            // } 
                            // else
                            // {
                                nodes[i].Pos -= mu * r;
                                nodes[j].Pos += mu * r;
                            // }
                        }
                        else // otherwise try to move the vertices at least a distance of 2 away
                        {
                            if (mag < 2)
                            {
                                float mu = Mathf.Min(.25f*eta, 1); // w = 1/d^2 = 1/4
                                // Vector3 r = ((mag-(d_ij+1))/2) * (X_ij/mag);
                                Vector3 r = ((mag-2)/2) * (X_ij/mag);
                                r.y = 0; // keep y position
                                nodes[i].Pos -= mu * r;
                                nodes[j].Pos += mu * r;
                            }
                        }
                    }
                }
                var centering = new Vector3(-centeringForce*nodes[i].Pos.x, 0, -centeringForce*nodes[i].Pos.z);  
                nodes[i].Pos += centering;
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