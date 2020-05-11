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
        const string graphPath = "Assets/Prefabs/NodeLink/Graph.prefab";
        Camera mainCam;
        Graph graph;
        [SetUp]
        public void SetUp()
        {
            mainCam = new GameObject("Camera").AddComponent<Camera>(); // smelly singleton required for tooltip and zooming
            mainCam.tag = "MainCamera";
            Graph prefab = (Graph)AssetDatabase.LoadAssetAtPath(graphPath, typeof(Graph));
            graph = GameObject.Instantiate(prefab);
        }
        [TearDown]
        public void TearDown()
        {
            GameObject.Destroy(graph.gameObject);
            GameObject.Destroy(mainCam.gameObject);
        }

        [Test]
        public void RandomGraphAdjacency()
        {
            const int n = 100;
            const int m = 10000;
            var nodes = new bool[n];
            var archive = new bool[n];
            for (int i=0; i<n; i++)
            {
                graph.AddNode(i);
                nodes[i] = true;
                archive[i] = false;
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

                // 1% of the time remove/add a node
                if (i%(m/100) == 0)
                {
                    int idx = UnityEngine.Random.Range(0,n);
                    if (nodes[idx] && !archive[idx])
                    {
                        graph.ArchiveNode(idx);
                        archive[idx] = true;
                        nodes[idx] = false;
                    }
                    else if (!nodes[idx] && archive[idx])
                    {
                        graph.RemoveNode(idx);
                        archive[idx] = false;
                        nodes[idx] = false;
                        for (int arc=0; arc<n; arc++)
                        {
                            edges[idx,arc] = false;
                            edges[arc,idx] = false;
                        }
                    }
                    else
                    {
                        graph.AddNode(idx);
                        archive[idx] = false;
                        nodes[idx] = true;
                    }
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
            while (!graph.GraphLayedOut) { yield return null; }
            Assert.IsTrue(graph.MaxChain == 3);

            graph.AddLink(0,3);
            while (!graph.GraphLayedOut) { yield return null; }
            Assert.IsTrue(graph.MaxChain == 2);
        }
        int i=0;
        [UnityTest]
        public IEnumerator LoopTest()
        {
            graph.FindLoops = true;
            for (int i=0; i<4; i++)
            {
                graph.AddNode(i);
            }
            for (int i=1; i<4; i++)
            {
                graph.AddLink(i-1, i);
            }
            while (!graph.GraphLayedOut) { yield return null; }
            Assert.IsTrue(graph.MaxLoop == 0);

            graph.AddLink(3,1);
            while (!graph.GraphLayedOut) { yield return null; }
            Assert.IsTrue(graph.MaxLoop == 3);

            graph.AddLink(3,0);
            while (!graph.GraphLayedOut) { yield return null; }
            Assert.IsTrue(graph.MaxLoop == 4);

            // TODO: try multiple max loops
        }

        [UnityTest]
        public IEnumerator StressXShape()
        {
            int n=5;
            // test simple 1 level ecosystems
            for (int i=0; i<n; i++) {
                graph.AddNode(i);
            }
            graph.AddLink(0,2);
            graph.AddLink(1,2);
            graph.AddLink(2,3);
            graph.AddLink(2,4);
            yield return TryCrossX();

            IEnumerator TryCrossX()
            {
                graph.AddLink(0,3);
                while (!graph.GraphLayedOut) { yield return null; }
                Assert.IsTrue(SGD.CalculateStress() < 1);

                graph.RemoveLink(0,3);
                graph.AddLink(1,3);
                while (!graph.GraphLayedOut) { yield return null; }
                Assert.IsTrue(SGD.CalculateStress() < 1);

                graph.RemoveLink(1,3);
                graph.AddLink(0,4);
                while (!graph.GraphLayedOut) { yield return null; }
                Assert.IsTrue(SGD.CalculateStress() < 1);

                graph.RemoveLink(0,4);
                graph.AddLink(1,4);
                while (!graph.GraphLayedOut) { yield return null; }
                Assert.IsTrue(SGD.CalculateStress() < 1);
            }
        }
        [UnityTest]
        public IEnumerator StressBipartite()
        {
            int n=5;
            // test simple 1 level ecosystems
            for (int i=0; i<n; i++) {
                graph.AddNode(i);
            }
            var edges = new bool[5,5];
            int nEdges = 0;
            var possibleEdges = new List<Tuple<int,int>>();
            for (int i=0; i<2; i++) {
                for (int j=2; j<5; j++) {
                    possibleEdges.Add(Tuple.Create(i,j));
                }
            }
            int nTests = 0, testsPassed = 0;

            graph.ConstrainTrophic = false;
            Func<bool> ThresholdPassed = ()=> SGD.CalculateStress() < (nEdges==6? .8f:.5f);
            yield return TryEdges(0);
            string debug1 = $"{testsPassed}/{nTests} passed";
            Assert.IsTrue(testsPassed==nTests, debug1);

            graph.ConstrainTrophic = true;
            ThresholdPassed = ()=> SGD.CalculateStress() < (nEdges==6? 2f:1.3f);
            yield return TryEdges(0);
            string debug2 = $"{testsPassed}/{nTests} passed";
            Assert.IsTrue(testsPassed==nTests, debug2);

            MonoBehaviour.print($"{debug1}\n{debug2}");

            // recursively try all possible edges
            IEnumerator TryEdges(int edgeIdx)
            {
                if (edgeIdx == 0) {
                    nTests = 0;
                    testsPassed = 0;
                }
                if (edgeIdx == possibleEdges.Count)
                {
                    while (!graph.GraphLayedOut) { yield return null; }
                    nTests += 1;
                    if (ThresholdPassed()) {
                        testsPassed += 1;
                    } else {
                        MonoBehaviour.print(DebugEdges());
                    }
                    yield break;
                }
                // try without adding edge first
                yield return TryEdges(edgeIdx+1);

                // then try adding the edge
                int src = possibleEdges[edgeIdx].Item1;
                int tgt = possibleEdges[edgeIdx].Item2;
                graph.AddLink(src, tgt);
                edges[src,tgt] = true;
                nEdges += 1;
                yield return TryEdges(edgeIdx+1);
                graph.RemoveLink(src,tgt);
                edges[src,tgt] = false;
                nEdges -= 1;
            }
            string DebugEdges()
            {
                var sb = new StringBuilder();
                for (int s=0; s<5; s++)
                {
                    for (int t=0; t<5; t++)
                    {
                        sb.Append(edges[s,t]? "1":"0");
                    }
                    sb.Append("\n");
                }
                return sb.ToString();
            }
        }
        [UnityTest]
        public IEnumerator LoopsAreSlowButJustHowSlow()
        {
            const int n = 100;
            const int m = 1000;
            graph.FindLoops = true;
            var edges = new bool[n,n];
            graph.AddNode(0);
            var sw = new Stopwatch();
            for (int i=1; i<n; i++)
            {
                sw.Restart();
                graph.AddNode(i);
                for (int j=0; j<m; j++)
                {
                    int src = UnityEngine.Random.Range(0,i);
                    int tgt = UnityEngine.Random.Range(0,i);

                    if (src != tgt && !edges[tgt,src])
                    {
                        if (!edges[src,tgt]) {
                            graph.AddLink(src, tgt);
                        } else {
                            graph.RemoveLink(src, tgt);
                        }
                        edges[src,tgt] = !edges[src,tgt];
                    }
                }
                while (!graph.GraphLayedOut) { yield return null; }
                MonoBehaviour.print($"{i} {sw.Elapsed}");
            }
        }
        [UnityTest]
        public IEnumerator StressSpeedAndRobustTest()
        {
            // test for NaN as well as speed
            const int n = 100;
            const int m = 10000;
            for (int i=0; i<n; i++)
            {
                graph.AddNode(i);
            }

            graph.ConstrainTrophic = true;
            var sw = new Stopwatch();
            sw.Start();
            var edges = new bool[n,n];
            for (int i=0; i<m; i++)
            {
                int src = UnityEngine.Random.Range(0,n);
                int tgt = UnityEngine.Random.Range(0,n);
                if (src != tgt && !edges[tgt,src]) // no self or bidirectional links
                {
                    if (!edges[src,tgt]) {
                        graph.AddLink(src, tgt);
                    } else {
                        graph.RemoveLink(src, tgt);
                    }
                    edges[src,tgt] = !edges[src,tgt];
                }
                if (i%100 == 0)
                {
                    while (!graph.GraphLayedOut) { yield return null; }
                    MonoBehaviour.print($"{i} time: {sw.Elapsed}");
                }
            }
            sw.Stop();
            while (!graph.GraphLayedOut) { yield return null; }
            MonoBehaviour.print($"time: {sw.Elapsed}");
        }
    }
}
