﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using MathNet.Numerics.LinearAlgebra;

namespace EcoBuilder.NodeLink
{
    public partial class NodeLink
    { 
        [SerializeField] float focusHeight;
        [SerializeField] float layoutSmoothTime;

        Vector3 nodesVelocity, graphVelocity;
        void TweenNodesToStress()
        {
            if (focusState != FocusState.SuperFocus)
            {
                // get average of all positions, and center
                // Vector2 centroid;
                // centroid = Vector3.zero;
                // foreach (Node no in nodes)
                // {
                //     Vector2 pos = no.StressPos;
                //     centroid += pos;
                // }
                // centroid /= nodes.Count;
                Vector2 minPos = new Vector2(float.MaxValue, float.MaxValue);
                Vector2 maxPos = new Vector2(float.MinValue, float.MinValue);
                foreach (Node no in nodes)
                {
                    minPos.x = Mathf.Min(no.StressPos.x, minPos.x);
                    minPos.y = Mathf.Min(no.StressPos.y, minPos.y);
                    maxPos.x = Mathf.Max(no.StressPos.x, maxPos.x);
                    maxPos.y = Mathf.Max(no.StressPos.y, maxPos.y);
                }
                Vector2 centroid = (minPos + maxPos) / 2;

                nodesParent.localPosition = Vector3.SmoothDamp(nodesParent.localPosition, Vector3.zero, ref nodesVelocity, layoutSmoothTime);
                graphParent.localPosition = Vector3.SmoothDamp(graphParent.localPosition, graphParentUnfocused, ref graphVelocity, layoutSmoothTime);

                if (ConstrainTrophic)
                {
                    centroid.y = 0;
                    foreach (Node no in nodes) {
                        no.StressPos -= centroid;
                    }
                }
                else
                {
                    float minY = float.MaxValue;
                    foreach (Node no in nodes) {
                        minY = Mathf.Min(no.StressPos.y, minY);
                    }
                    centroid.y = minY;
                    foreach (Node no in nodes) {
                        no.StressPos -= centroid;
                    }
                }
            }
            else // superfocused
            {
                nodesParent.localPosition =
                    Vector3.SmoothDamp(nodesParent.localPosition, -focusedNode.FocusPos,
                                       ref nodesVelocity, layoutSmoothTime);
                graphParent.localPosition =
                    Vector3.SmoothDamp(graphParent.localPosition, Vector3.up*focusHeight,
                                    ref graphVelocity, layoutSmoothTime);
            }
            
            foreach (Node no in nodes)
            {
                no.TweenPos(layoutSmoothTime);
            }
        }

        [SerializeField] float zoomSmoothTime;
        float graphScale=1, graphScaleTarget=1, graphScaleVelocity=0;
        Vector3 panVelocity;
        void TweenZoomToFit()
        {
            if (focusState != FocusState.SuperFocus)
            {
                // adjust zoom target and pan when unfocused
                float maxError = float.MinValue;
                foreach (Node no in nodes)
                {
                    // make sure that all nodes fit on screen
                    var viewportPos = mainCam.WorldToViewportPoint(no.transform.localPosition * graphScaleTarget) - new Vector3(.5f,.5f);
                    maxError = Mathf.Max(maxError, Mathf.Abs(viewportPos.x) - .375f);
                    maxError = Mathf.Max(maxError, Mathf.Abs(viewportPos.y) - .3f);
                }
                graphScaleTarget -= maxError*.1f;
                graphScaleTarget = Mathf.Min(graphScaleTarget, 1); // don't scale too much
                graphScale = Mathf.SmoothDamp(graphScale, graphScaleTarget, ref graphScaleVelocity, zoomSmoothTime);

                // reset pan
                transform.localPosition = Vector3.SmoothDamp(transform.localPosition, Vector3.zero, ref panVelocity, layoutSmoothTime);
            }
            else // superfocus
            {
                graphScale = Mathf.SmoothDamp(graphScale, 1, ref graphScaleVelocity, zoomSmoothTime);
            }
            graphParent.localScale = graphScale * Vector3.one;
        }

        float xDefaultRotation;
        float yRotation=0, yTargetRotation=0, yRotationVelocity=0;
        float xRotation=0, xTargetRotation=0, xRotationVelocity=0;
        private void MomentumRotate()
        {
            if (dragging)
            {
                yRotation = Mathf.SmoothDamp(yRotation, yTargetRotation, ref yRotationVelocity, .05f);
                xRotation = Mathf.SmoothDamp(xRotation, xTargetRotation, ref xRotationVelocity, .05f);
            }
            else
            {
                yTargetRotation = 0;
                yRotation = Mathf.SmoothDamp(yRotation, yTargetRotation, ref yRotationVelocity, .5f);
                xTargetRotation = xDefaultRotation;
                xRotation = Mathf.SmoothDamp(xRotation, xTargetRotation, ref xRotationVelocity, .5f);
            }
            nodesParent.transform.localRotation = Quaternion.Euler(0,yRotation,0);
            graphParent.transform.localRotation = Quaternion.Euler(xRotation,0,0);
        }

        ////////////////////////////
        // init layout with SGD
        // also calculate number of components

        // for SGD
        struct StressTerm {
            public int i, j;
            public float d, w;
        }
        static List<StressTerm> sgdTerms = new List<StressTerm>();
        static List<Vector2> sgdPos = new List<Vector2>();
        static Dictionary<int, int> sgdSquished = new Dictionary<int, int>();
        static List<int> sgdUnsquished = new List<int>();

        // for BFS
        static List<int> sgdSources = new List<int>();
        static List<int> sgdTargets = new List<int>();

        [SerializeField] int t_init, t_max;
        [SerializeField] float eps;

        private void LayoutSGD(int seed=0)
        {
            sgdPos.Clear(); // node positions
            sgdSquished.Clear();
            sgdUnsquished.Clear();

            var rand = new System.Random(seed);
            foreach (int i in nodes.Indices)
            {
                sgdSquished[i] = sgdUnsquished.Count;
                sgdUnsquished.Add(i);
                if (!ConstrainTrophic) {
                    sgdPos.Add(new Vector2(i, (float)rand.NextDouble()));
                } else {
                    // still add a little jitter to prevent NaN
                    sgdPos.Add(new Vector2(i, nodes[i].StressPos.y + .1f*(float)rand.NextDouble()));
                }
                // sgdPos.Add(nodes[i].StressPos);
            }
            sgdSources.Clear();
            sgdTargets.Clear();
            foreach (int i in nodes.Indices)
            {
                sgdSources.Add(sgdTargets.Count);
                foreach (int j in undirected[i])
                {
                    sgdTargets.Add(sgdSquished[j]);
                }
            }
            sgdSources.Add(sgdTargets.Count); // for iteration to next


            // calculate terms with BFS
            sgdTerms.Clear();
            int d_max = 0;
            var q = new Queue<int>();
            for (int source=0; source<sgdSources.Count-1; source++)
            {
                // BFS for each node
                q.Enqueue(source);
                var d = new Dictionary<int, int>();
                d[source] = 0;
                while (q.Count > 0)
                {
                    int prev = q.Dequeue();
                    for (int i=sgdSources[prev]; i<sgdSources[prev+1]; i++)
                    {
                        int next = sgdTargets[i];
                        if (!d.ContainsKey(next))
                        {
                            d[next] = d[prev] + 1;
                            q.Enqueue(next);

                            if (source < next) // only add every other term
                            {
                                sgdTerms.Add(new StressTerm () {
                                    i=source,
                                    j=next,
                                    d=d[next],
                                    w=1f/(d[next]*d[next])
                                });
                                d_max = Math.Max(d[next], d_max);
                            }
                        }
                    }
                }
            }

            // keep memory tidy
            sgdTerms.TrimExcess();
            sgdPos.TrimExcess();
            sgdUnsquished.TrimExcess();
            sgdSources.TrimExcess();
            sgdTargets.TrimExcess();

            // perform optimisation
            foreach (float eta in SGDSchedule(d_max, t_init, t_max, eps))
            {
                FYShuffle(sgdTerms, rand);
                foreach (var term in sgdTerms)
                {
                    Vector2 X_ij = sgdPos[term.i] - sgdPos[term.j];

                    float mag = X_ij.magnitude;
                    if (mag == 0) // prevent divide by zero
                    {
                        sgdPos[term.i] += new Vector2((float)rand.NextDouble(), (float)rand.NextDouble());
                        continue;
                    }
                    float mu = Math.Min(term.w * eta, 1);
                    Vector2 r = ((mag-term.d)/2f) * (X_ij/mag);

                    Assert.IsFalse(float.IsNaN(r.x) || float.IsNaN(r.y), "r=NaN for SGD");
                   
                    sgdPos[term.i] -= mu * r;
                    sgdPos[term.j] += mu * r;
                }
                // clamp back to y-axis position if constrained
                if (ConstrainTrophic) {
                    for (int i=0; i<sgdPos.Count; i++) {
                        // sgdPos[i] = new Vector2(sgdPos[i].x, nodes[sgdUnsquished[i]].StressPos.y);
                        sgdPos[i] = new Vector2(sgdPos[i].x, Mathf.Lerp(sgdPos[i].y, nodes[sgdUnsquished[i]].StressPos.y, .5f));
                    }
                }
            }

            for (int i=0; i<sgdPos.Count; i++)
            {
                nodes[sgdUnsquished[i]].StressPos = sgdPos[i];
            }
        }
        // // reassigns positions, but flips components if it will preserve distances better
        // static List<float> oldCentroids = new List<float>();
        // static List<float> newCentroids = new List<float>();
        // static List<int> counts = new List<int>();
        // private void SGDProcrustesFlipComponents()
        // {
        //     oldCentroids.Clear();
        //     newCentroids.Clear();
        //     counts.Clear();
        //     for (int i=0; i<NumComponents; i++)
        //     {
        //         oldCentroids.Add(0);
        //         newCentroids.Add(0);
        //         counts.Add(0);
        //     }
        //     oldCentroids.TrimExcess();
        //     newCentroids.TrimExcess();
        //     counts.TrimExcess();

        //     foreach (int idx in componentMap.Keys)
        //     {
        //         int cc = componentMap[idx];
        //         oldCentroids[cc] += nodes[idx].StressPos.x;
        //         newCentroids[cc] += sgdPos[sgdSquished[idx]].x;
        //         counts[cc] += 1;
        //     }
        //     for (int i=0; i<NumComponents; i++)
        //     {
        //         oldCentroids[i] /= counts[i];
        //         newCentroids[i] /= counts[i];
        //     }

        //     // second dimension holds sum of errors for flipped, not flipped
        //     var errors = new float[NumComponents, 2];
        //     foreach (int idx in componentMap.Keys)
        //     {
        //         int cc = componentMap[idx];
        //         float error = (nodes[idx].StressPos.x - oldCentroids[cc])  - (sgdPos[sgdSquished[idx]].x - newCentroids[cc]);
        //         errors[cc,0] += error * error;

        //         float flippedError = (nodes[idx].StressPos.x - oldCentroids[cc])  + (sgdPos[sgdSquished[idx]].x - newCentroids[cc]);
        //         errors[cc,1] += flippedError * flippedError;
        //     }
        //     foreach (int idx in componentMap.Keys)
        //     {
        //         int cc = componentMap[idx];
        //         var pos = sgdPos[sgdSquished[idx]];
        //         if (errors[cc,0] > errors[cc,1]) {
        //             pos.x = -pos.x; // reflect
        //         }
        //         nodes[idx].StressPos = pos;
        //     }
        // }
        private static IEnumerable<float> SGDSchedule(int d_max, int t_init, int t_max, float eps)
        {
            float eta_max = d_max*d_max;
            for (int t=0; t<t_init; t++)
            {
                yield return eta_max; // extra iterations to escape local minima
            }
            float lambda = Mathf.Log(eta_max/eps) / (t_max-1);
            for (int t=0; t<t_max; t++)
            {
                yield return eta_max * Mathf.Exp(-lambda * t);
            }
        }
        public static void FYShuffle<T>(List<T> deck, System.Random rand)
        {
            int n = deck.Count;
            for (int i=0; i<n-1; i++)
            {
                int j = rand.Next(i, n);
                T temp = deck[j];
                deck[j] = deck[i];
                deck[i] = temp;
            }
        }

        /////////////////////////////////////////////
        // fine tune with majorization every frame

        private Queue<int> todoBFS = new Queue<int>();
        private void FineTuneLayout()
        {
            if (nodes.Count==0 || focusState==FocusState.SuperFocus) {
                return;
            }
            int i = todoBFS.Dequeue(); // only do one vertex at a time
            if (ConstrainTrophic)
            {
                if (!LaplacianDetZero)
                {
                    TrophicGaussSeidel();
                    MoveNodesToTrophicLevel(.1f);
                }
                LayoutMajorizationHorizontal(i);
            }
            else
            {
                LayoutMajorization(i);
            }

            todoBFS.Enqueue(i);
        }
        private void MoveNodesToTrophicLevel(float lerp=1)
        {
            Assert.IsTrue(lerp>=0 && lerp<=1, $"lerp {lerp} is out of bounds");

            float trophicScaling = 1;
            if (MaxTrophicLevel-1 > MaxChain)
            {
                trophicScaling = MaxChain / (MaxTrophicLevel-1);
            }
            foreach (Node no in nodes)
            {
                float y = Mathf.Lerp(no.StressPos.y, trophicScaling * (trophicLevels[no.Idx]-1), lerp);
                no.StressPos = new Vector2(no.StressPos.x, y);
            }
        }
        private void LayoutMajorization(int i)
        {
            Vector2 X_i = nodes[i].StressPos;
            float topSumX = 0, topSumY = 0, botSum=0;
            foreach (var d_j in ShortestPathsBFS(i))
            {
                int j = d_j.Key;
                float d_ij = d_j.Value;
                Vector2 X_j = nodes[j].StressPos;
                float mag = (X_i - X_j).magnitude;
                float w_ij = 1f / (d_ij * d_ij);

                topSumX += w_ij * (X_j.x + (d_ij*(X_i.x - X_j.x))/mag);
                topSumY += w_ij * (X_j.y + (d_ij*(X_i.y - X_j.y))/mag);
                botSum += w_ij;
            }
            if (botSum > 0)
            {
                nodes[i].StressPos = new Vector2(topSumX/botSum, topSumY/botSum);
            }
        }
        private void LayoutMajorizationHorizontal(int i)
        {
            float topSumX = 0, /*topSumY = 0,*/ botSum=0;
            Vector2 X_i = nodes[i].StressPos;
            foreach (var d_j in ShortestPathsBFS(i))
            {
                int j = d_j.Key;
                float d_ij = d_j.Value;

                Vector2 X_j = nodes[j].StressPos;
                float mag = (X_i - X_j).magnitude;
                float w_ij = 1f / (d_ij * d_ij);

                topSumX += w_ij * (X_j.x + (d_ij*(X_i.x - X_j.x))/mag);
                // topSumY += w_ij * (X_j.y + (d_ij*(X_i.y - X_j.y))/mag);
                botSum += w_ij;
            }
            if (botSum > 0)
            {
                // nodes[i].StressPos = new Vector2(topSumX/botSum, topSumY/botSum);
                nodes[i].StressPos = new Vector2(topSumX/botSum, nodes[i].StressPos.y);
            }
        }
        private static Dictionary<int, int> visitedBFS = new Dictionary<int, int>();
        private IEnumerable<KeyValuePair<int, int>> ShortestPathsBFS(int source)
        {
            visitedBFS.Clear();

            visitedBFS[source] = 0;
            var q = new Queue<int>();
            q.Enqueue(source);

            while (q.Count > 0)
            {
                int current = q.Dequeue();
                foreach (int next in undirected[current])
                {
                    if (!visitedBFS.ContainsKey(next))
                    {
                        q.Enqueue(next);
                        visitedBFS[next] = visitedBFS[current] + 1;
                    }
                }
            }
            visitedBFS.Remove(source);
            return visitedBFS;
        }

        /////////////////////////////////
        // cholesky factorization
        /*
        private void InitCholesky(int seed)
        {
            sgdPos.Clear();
            sgdSquished.Clear();
            sgdUnsquished.Clear();

            var rand = new System.Random(seed);
            foreach (int i in nodes.Indices)
            {
                sgdSquished[i] = sgdUnsquished.Count;
                sgdUnsquished.Add(i);
                // if (!ConstrainTrophic) {
                    sgdPos.Add(new Vector2(sgdSquished[i], (float)rand.NextDouble()));
                // } else {
                //     sgdPos.Add(new Vector2(sgdSquished[i], nodes[i].StressPos.y + .1f*(float)rand.NextDouble()));
                //     // still add a little jitter to prevent NaN
                // }
                // sgdPos.Add(nodes[i].StressPos);
            }
            sgdSources.Clear();
            sgdTargets.Clear();
            foreach (int i in nodes.Indices)
            {
                sgdSources.Add(sgdTargets.Count);
                foreach (int j in undirected[i])
                {
                    sgdTargets.Add(sgdSquished[j]);
                }
            }
            sgdSources.Add(sgdTargets.Count); // for iteration to next


            int n = sgdPos.Count;
            var Lw = Matrix<float>.Build.Dense(n-1, n-1);
            var Lz = Matrix<float>

            // calculate terms with BFS
            sgdTerms.Clear();
            int d_max = 0;
            var q = new Queue<int>();
            for (int source=0; source<sgdSources.Count-1; source++)
            {
                // BFS for each node
                q.Enqueue(source);
                var d = new Dictionary<int, int>();
                d[source] = 0;
                while (q.Count > 0)
                {
                    int prev = q.Dequeue();
                    for (int i=sgdSources[prev]; i<sgdSources[prev+1]; i++)
                    {
                        int next = sgdTargets[i];
                        if (!d.ContainsKey(next))
                        {
                            d[next] = d[prev] + 1;
                            q.Enqueue(next);

                            // if (source < next) // only add every other term
                            // {
                            //     sgdTerms.Add(new StressTerm () {
                            //         i=source,
                            //         j=next,
                            //         d=d[next],
                            //         w=1f/(d[next]*d[next])
                            //     });
                            //     d_max = Math.Max(d[next], d_max);
                            // }
                        }
                    }
                }
            }

            // keep memory tidy
            sgdTerms.TrimExcess();
            sgdPos.TrimExcess();
            sgdUnsquished.TrimExcess();
            sgdSources.TrimExcess();
            sgdTargets.TrimExcess();
        }
        */

    }
}