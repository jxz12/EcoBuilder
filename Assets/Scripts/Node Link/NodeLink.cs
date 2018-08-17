using UnityEngine;
using UnityEngine.Events;
using SparseMatrix;
using System;
using System.Collections.Generic;

public class NodeLink : MonoBehaviour
{
    [SerializeField] Node nodePrefab;
    [SerializeField] Link linkPrefab;

    [Serializable] class IntEvent : UnityEvent<int> { };
    [SerializeField] IntEvent NodeInspectedEvent;

    SparseVector<Node> nodes = new SparseVector<Node>();
    SparseMatrix<Link> links = new SparseMatrix<Link>();

    // TODO: include colour and a centering force
    public void AddNode(int idx)
    {
        Node newNode = Instantiate(nodePrefab, transform);
        newNode.Init(idx);
        newNode.transform.localPosition = new Vector3(UnityEngine.Random.Range(-1f, 1f), 1, UnityEngine.Random.Range(-1f, 1f));
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
    }

    public void AddLink(int i, int j)
    {
        Link newLink = Instantiate(linkPrefab, transform);
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

    // local majorization
    private void Layout()
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
                //float y = (float)(SpeciesManager.Instance.GetTrophicLevel(i)); // TODO: make trophic levels an event and store y
                float y = 1;
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

    private void Update()
    {
        Layout();
        transform.Rotate(Vector3.up, .5f);
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