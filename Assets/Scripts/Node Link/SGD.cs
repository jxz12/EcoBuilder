using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace EcoBuilder.NodeLink
{
    public static class SGD
    {
        ////////////////////////////
        // init layout with SGD

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

        static Func<int, Vector2> GetPos;
        static Action<int, Vector2> SetPos;
        static Dictionary<int, HashSet<int>> undirected;
        public static void InitSGD(Func<int, Vector2> GetPosition, Action<int, Vector2> SetPosition, Dictionary<int, HashSet<int>> graph)
        {
            GetPos = GetPosition;
            SetPos = SetPosition;
            undirected = graph;
        }

        public static void LayoutSGD(bool keepY=false, int t_init=5, int t_max=10, float eps=.1f, int seed=0)
        {
            sgdPos.Clear(); // node positions
            sgdSquished.Clear();
            sgdUnsquished.Clear();

            var rand = new System.Random(seed);
            foreach (int i in undirected.Keys)
            {
                sgdSquished[i] = sgdUnsquished.Count;
                sgdUnsquished.Add(i);
                if (!keepY) {
                    sgdPos.Add(new Vector2(i, (float)rand.NextDouble()));
                } else {
                    // still add a little jitter to prevent NaN
                    sgdPos.Add(new Vector2(i, GetPos(i).y + .1f*(float)rand.NextDouble()));
                }
                // sgdPos.Add(nodes[i].StressPos);
            }
            sgdSources.Clear();
            sgdTargets.Clear();
            foreach (int i in undirected.Keys)
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
                if (keepY) {
                    for (int i=0; i<sgdPos.Count; i++) {
                        // sgdPos[i] = new Vector2(sgdPos[i].x, nodes[sgdUnsquished[i]].StressPos.y);
                        sgdPos[i] = new Vector2(sgdPos[i].x, Mathf.Lerp(sgdPos[i].y, GetPos(i).y, .5f));
                    }
                }
            }

            for (int i=0; i<sgdPos.Count; i++)
            {
                SetPos(i, sgdPos[i]);
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
    }
}
