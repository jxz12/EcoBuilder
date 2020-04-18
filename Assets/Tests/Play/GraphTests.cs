
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Diagnostics;
using EcoBuilder.NodeLink;

namespace EcoBuilder.Tests
{
    public class GraphTests // for things in nodelink
    {
        Camera mainCam;
        Graph graph;
        [SetUp]
        public void SetUp()
        {
            mainCam = new GameObject().AddComponent<Camera>(); // smelly singleton required for tooltip and zooming
            mainCam.tag = "MainCamera";
            Graph prefab = (Graph)AssetDatabase.LoadAssetAtPath("Assets/Prefabs/NodeLink/Graph.prefab", typeof(Graph));
            graph = GameObject.Instantiate(prefab);
        }
        [TearDown]
        public void TearDown()
        {
            GameObject.Destroy(graph.gameObject);
            GameObject.Destroy(mainCam.gameObject);
        }

        readonly int n = 100;
        readonly int m = 10000;
        [Test]
        public void RandomGraphAdjacency()
        {
            var nodes = new bool[n];
            for (int i=0; i<n; i++)
            {
                graph.AddNode(i);
                nodes[i] = true;
            }

            var edges = new bool[n,n];
            for (int i=0; i<m; i++)
            {
                int src = UnityEngine.Random.Range(0,n);
                int tgt = UnityEngine.Random.Range(0,n);
                                                              // no self or bidirectional links
                if (nodes[src] && nodes[tgt] && src != tgt && !edges[tgt,src])
                {
                    if (!edges[src,tgt]) {
                        graph.AddLink(src, tgt);
                    } else {
                        graph.RemoveLink(src, tgt);
                    }
                    edges[src,tgt] = !edges[src,tgt];
                }

                // 1% of the time remove a node
                if (UnityEngine.Random.Range(0, 100) == 0)
                {
                    int idx = UnityEngine.Random.Range(0,n);
                    if (nodes[idx]) {
                        graph.ArchiveNode(idx);
                    } else {
                        graph.AddNode(idx);
                    }
                    nodes[idx] = !nodes[idx];
                }
            }

            var finalEdges = new bool[n,n];
            for (int i=0; i<n; i++)
            {
                foreach (int j in graph.GetActiveTargets(i))
                {
                    finalEdges[i,j] = true;
                }
            }
            for (int i=0; i<n; i++)
            {
                for (int j=0; j<n; j++)
                {
                    if (nodes[i] && nodes[j] && edges[i,j]) {
                        Assert.IsTrue(finalEdges[i,j]);
                    } else {
                        Assert.IsFalse(finalEdges[i,j]);
                    }
                }
            }
        }
        [UnityTest]
        public IEnumerator Chain()
        {
            for (int i=0; i<4; i++)
            {
                graph.AddNode(i);
            }
            for (int i=1; i<4; i++)
            {
                graph.AddLink(i-1, i);
            }
            while (!graph.GraphLayedOut) {
                yield return null;
            }
            Assert.IsTrue(graph.MaxChain == 3);

            graph.AddLink(0,3);
            while (!graph.GraphLayedOut) {
                yield return null;
            }
            Assert.IsTrue(graph.MaxChain == 2);
        }
        int i=0;
        [UnityTest]
        public IEnumerator LoopTest()
        {
            for (int i=0; i<4; i++)
            {
                graph.AddNode(i);
            }
            for (int i=1; i<4; i++)
            {
                graph.AddLink(i-1, i);
            }
            while (!graph.GraphLayedOut) {
                yield return null;
            }
            Assert.IsTrue(graph.MaxLoop == 0);

            graph.AddLink(3,1);
            while (!graph.GraphLayedOut) {
                yield return null;
            }
            Assert.IsTrue(graph.MaxLoop == 3);

            graph.AddLink(3,0);
            while (!graph.GraphLayedOut) {
                yield return null;
            }
            Assert.IsTrue(graph.MaxLoop == 4);

            // TODO: try multiple max loops
        }
        [UnityTest]
        public IEnumerator StressExamples()
        {
            yield return null;
        }

    }
}
