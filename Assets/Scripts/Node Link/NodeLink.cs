using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using SparseMatrix;
using System;
using System.Collections.Generic;

public class NodeLink : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] Node nodeProducerPrefab, nodeConsumerPrefab;
    [SerializeField] Link linkPrefab;

    [Serializable] class IntEvent : UnityEvent<int> { };
    [SerializeField] IntEvent NodeInspectedEvent;

    [SerializeField] float stepSize=.1f, centeringForce=.01f, trophicForce=.5f;
    [SerializeField] float rotationMultiplier=.9f, yMinRotation=.4f, yRotationDrag=.1f, xRotationForce=15;
    [SerializeField] float zoomMultiplier=.005f;

    SparseVector<Node> nodes = new SparseVector<Node>();
    SparseMatrix<Link> links = new SparseMatrix<Link>();

    private Transform nodesParent, linksParent;
    void Awake()
    {
        nodesParent = new GameObject("Nodes").transform;
        nodesParent.SetParent(transform, false);
        linksParent = new GameObject("Links").transform;
        linksParent.SetParent(transform, false);
    }

    public void AddProducerNode(int idx)
    {
        Node newNode = Instantiate(nodeProducerPrefab, nodesParent);
        newNode.Init(idx);
        newNode.Pos = new Vector3(UnityEngine.Random.Range(-1f, 1f), 1, UnityEngine.Random.Range(-1f, 1f));
        nodes[idx] = newNode;
    }
    public void AddConsumerNode(int idx)
    {
        Node newNode = Instantiate(nodeConsumerPrefab, nodesParent);
        newNode.Init(idx);
        newNode.Pos = new Vector3(UnityEngine.Random.Range(-1f, 1f), 1, UnityEngine.Random.Range(-1f, 1f));
        nodes[idx] = newNode;
    }
    public void RemoveNode(int idx)
    {
        Destroy(nodes[idx].gameObject);
        nodes.RemoveAt(idx);

        var toRemove = new List<Tuple<int,int>>();
        foreach (int other in links.GetColumnIndicesInRow(idx))
            toRemove.Add(Tuple.Create(idx, other));
        foreach (int other in links.GetRowIndicesInColumn(idx))
            toRemove.Add(Tuple.Create(other, idx));

        foreach (var ij in toRemove)
            RemoveLink(ij.Item1, ij.Item2);

        UpdateTrophicEquations();
    }

    public void AddLink(int i, int j)
    {
        Link newLink = Instantiate(linkPrefab, linksParent);
        newLink.Init(nodes[i], nodes[j]);
        links[i, j] = newLink;

        UpdateTrophicEquations();
    }
    public void RemoveLink(int i, int j)
    {
        Destroy(links[i, j].gameObject);
        links.RemoveAt(i, j);

        UpdateTrophicEquations();
    }
    public void ColorNode(int idx, Color c)
    {
        nodes[idx].Col = c;
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
    }

    private void Update()
    {
        TrophicGaussSeidel();
        SetYAxis(i=>trophicLevels[i]-1);
        LayoutSGD(stepSize);
        Rotate();
    }

    void Rotate()
    {
        if (!dragging)
        {
            yRotationMomentum += (yMinRotation - yRotationMomentum) * yRotationDrag;
            nodesParent.Rotate(Vector3.up, yRotationMomentum);

            float xRotation = -transform.rotation.x * xRotationForce;
            transform.Rotate(Vector3.right, xRotation);
        }
    }

    // TODO: add zooming with pinch
    // TODO: check for unsolvable equations
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
            transform.Rotate(Vector3.right, xSpin);
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

    void UpdateTrophicEquations()
    {
        // update the system of linear equations
        foreach (Node no in nodes)
            trophicA[no.Idx] = 0;

        foreach (Link li in links)
            trophicA[li.Target.Idx] += 1f; // add one to the row for every resource it has

        foreach (Node no in nodes)
            if (trophicA[no.Idx] != 0)
                trophicA[no.Idx] = -1f / trophicA[no.Idx]; // invert, ensures diagonal dominance
    }

    void TrophicGaussSeidel()
    {
        SparseVector<float> temp = new SparseVector<float>();
        foreach (Link li in links)
        {
            int resource = li.Source.Idx, consumer = li.Target.Idx;
            temp[consumer] += trophicA[consumer] * trophicLevels[resource];
        }
        foreach (Node no in nodes)
        {
            trophicLevels[no.Idx] = (1 - temp[no.Idx]);
        }
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

                    if (links[i,j] != null || links[j,i] != null) // if connected, do normal SGD
                    {
                        float mu = Mathf.Min(eta, 1); // w = 1/d^2 = 1/1
                        Vector3 r = ((mag-1)/2) * (X_ij/mag);

                        r.y = 0; // keep y position
                        nodes[i].Pos -= mu * r;
                        nodes[j].Pos += mu * r;
                    }
                    else // otherwise try to move the vertices at least a distance of 2 away
                    {
                        if (mag < 2)
                        {
                            float mu = Mathf.Min(.25f*eta, 1); // w = 1/d^2 = 1/4
                            Vector3 r = ((mag-2)/2) * (X_ij/mag);
                            r.y = 0; // keep y position
                            nodes[i].Pos -= mu * r;
                            nodes[j].Pos += mu * r;
                        }
                    }
                }
            }
            nodes[i].Pos -= nodes[i].Pos * centeringForce;
        }
    }

    // local majorization
    private void LayoutMajorization()
    {
        foreach (int i in nodes.Indices)
        {
            float xTop = 0, zTop = 0;
            float wBot = 0;
            Vector3 X_i = nodes[i].transform.localPosition;

            foreach (int j in nodes.Indices)
            {
                if (i != j)
                {
                    Vector3 X_j = nodes[j].transform.localPosition;
                    float d = (X_i - X_j).magnitude;

                    if (links[i,j] != null || links[j,i] != null) // if connected, then do normal stress
                    {
                        xTop += X_j.x + (X_i.x - X_j.x) / d;
                        zTop += X_j.z + (X_i.z - X_j.z) / d;
                        wBot += 1;
                    }
                    else // otherwise try to move the vertices at least a distance of 2 away
                    {
                        if (d < 2)
                        {
                            xTop += .25f * (X_j.x + 2 * (X_i.x - X_j.x) / d);
                            zTop += .25f * (X_j.z + 2 * (X_i.z - X_j.z) / d);
                            wBot += .25f;
                        }
                    }
                }
            }
            if (wBot != 0)
            {
                float y = trophicLevels[i];
                float x = xTop / wBot, z = zTop / wBot;
                nodes[i].transform.localPosition = new Vector3(x, y, z);
            }
        }

        float xAvg = 0, zAvg = 0;
        int n = 0;
        foreach (Node node in nodes)
        {
            xAvg += node.transform.localPosition.x;
            zAvg += node.transform.localPosition.z;
            n += 1;
        }
        if (n > 0)
        {
            xAvg /= n;
            zAvg /= n;
            foreach (Node node in nodes)
                node.transform.localPosition -= new Vector3(xAvg, 0, zAvg);
        }
    }

    //public static string MatStr<T>(T[,] mat)
    //{
    //    var sb = new System.Text.StringBuilder();
    //    int m = mat.GetLength(0), n = mat.GetLength(1);
    //    for (int i=0; i<m; i++)
    //    {
    //        for (int j=0; j<n; j++)
    //        {
    //            sb.Append(mat[i, j].ToString() + " ");
    //        }
    //        sb.Append("\n");
    //    }
    //    sb.Remove(sb.Length - 1, 1);
    //    return sb.ToString();
    //}
    //public static string VecStr<T>(T[] vec)
    //{
    //    var sb = new System.Text.StringBuilder();
    //    int n = vec.Length;
    //    for (int i=0; i<n; i++)
    //    {
    //            sb.Append(vec[i].ToString() + " ");
    //    }
    //    sb.Remove(sb.Length - 1, 1);
    //    return sb.ToString();
    //}
}