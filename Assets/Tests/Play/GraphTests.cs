
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
        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.

        Camera mainCam;
        Graph graph;
        [SetUp]
        public void SetUp()
        {
            mainCam = new GameObject().AddComponent<Camera>(); // smelly singleton
            mainCam.tag = "MainCamera";
            Graph prefab = (Graph)AssetDatabase.LoadAssetAtPath("Assets/Prefabs/NodeLink/Graph.prefab", typeof(Graph));
            graph = GameObject.Instantiate(prefab);
        }
        /*
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var loader = SceneManager.LoadSceneAsync("Assets/Tests/Graph.unity");
            while (!loader.isDone) {
                yield return null;
            }
            graph = MonoBehaviour.FindObjectOfType<Graph>();
        }
        */
        [TearDown]
        public void TearDown()
        {
            GameObject.Destroy(graph.gameObject);
            GameObject.Destroy(mainCam.gameObject);
        }

        readonly int n = 10;
        readonly int m = 100;
        [UnityTest]
        public IEnumerator RandomGraphAdjacency()
        {
            yield return null;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int i=0; i<n; i++)
            {
                var shape = new GameObject();
                shape.AddComponent<MeshRenderer>(); // dummy renderer
                graph.AddNode(i, shape);
            }

            var adj = new bool[n,n];
            for (int i=0; i<m; i++)
            {
                int src = UnityEngine.Random.Range(0,n);
                int tgt = UnityEngine.Random.Range(0,n);

                if (src != tgt && !adj[tgt,src]) // no self or bidirectional links
                {
                    if (!adj[src,tgt]) {
                        graph.AddLink(src, tgt);
                    } else {
                        graph.RemoveLink(src, tgt);
                    }
                    adj[src,tgt] = !adj[src,tgt];
                }
                // TODO: randomly remove/add nodes as well as un/redoing
            }
            for (int i=0; i<n; i++)
            {
                foreach (int j in graph.GetActiveTargets(i))
                {
                    Assert.IsTrue(adj[i,j]);
                    adj[i,j] = false;
                }
            }
            for (int i=0; i<n; i++)
            {
                for (int j=0; j<n; j++)
                {
                    Assert.IsFalse(adj[i,j]);
                }
            }
            sw.Stop();
            UnityEngine.Debug.Log($"elapsed={sw.Elapsed}");
        }
        public IEnumerator ChainTest()
        {
            yield return null;
        }
        public IEnumerator LoopTest()
        {
            yield return null;
        }
        [UnityTest]
        public IEnumerator StressTest()
        {
            // TODO: test trophiclevels constraints with bad local minima
            yield return null;
        }
    }
}
