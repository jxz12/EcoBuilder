
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

        [UnitySetUp]
        public IEnumerator Setup()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            SceneManager.LoadScene("Play", LoadSceneMode.Additive);
            yield return null;
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // GameObject.Destroy(nodelink.gameObject);
            yield return null;
        }

        readonly int n = 10;
        readonly int m = 100;
        [UnityTest]
        public IEnumerator RandomGraphAdjacency()
        {
            yield return null;
            Stopwatch sw = new Stopwatch();
            var graphs = MonoBehaviour.FindObjectsOfType<Graph>();
            Assert.IsFalse(graphs.Length != 1);
            var graph = graphs[0];
            sw.Start();
            for (int i=0; i<n; i++)
            {
                var shape = new GameObject();
                shape.AddComponent<MeshRenderer>();
                graph.AddNode(i, shape);
                yield return null;
            }
            yield return null;

            // bool[,] graph = new bool[n,n];
            // for (int i=0; i<m; i++)
            // {
            //     int src = UnityEngine.Random.Range(0,n);
            //     int tgt = UnityEngine.Random.Range(0,n);

            //     if (src != tgt && !graph[tgt,src]) // no self or bidirectional links
            //     {
            //         if (graph[src,tgt]) {
            //             nodelink.RemoveLink(src, tgt);
            //         } else {
            //             nodelink.AddLink(src, tgt);
            //         }
            //         graph[src,tgt] = !graph[src,tgt];
            //     }
            // }
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
