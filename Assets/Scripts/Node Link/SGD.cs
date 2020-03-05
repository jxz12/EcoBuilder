using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace EcoBuilder.NodeLink
{
    public static class SGD
    {
        ////////////////////////////
        // layout with SGD

        // indices
        static Dictionary<int, int> sgdSquished = new Dictionary<int, int>();
        static List<int> sgdUnsquished = new List<int>();

        // for BFS
        static List<int> sgdSources = new List<int>();
        static List<int> sgdTargets = new List<int>();

        static List<Vector3> sgdPos = new List<Vector3>();
        static List<float> sgdKeptY = new List<float>();
        static System.Random sgdRand;
        static bool sgdKeepY = false;
        public static void InitSGD(Dictionary<int, HashSet<int>> undirected, Func<int, float> YConstraint=null, int seed=0)
        {
            sgdSquished.Clear();
            sgdUnsquished.Clear();
            sgdPos.Clear(); // node positions

            sgdRand = new System.Random(seed);
            sgdKeepY = YConstraint!=null;
            sgdKeptY.Clear();
            foreach (int i in undirected.Keys)
            {
                sgdSquished[i] = sgdUnsquished.Count;
                sgdUnsquished.Add(i);

                /*if (!keepY) {
                    sgdPos.Add(new Vector2(i, (float)sgdRand.NextDouble()));
                } else {
                    sgdPos.Add(new Vector2(i, GetPos(i).y + .1f*(float)sgdRand.NextDouble()));
                    // still add a little jitter to prevent NaN
                }*/

                // sgdPos.Add(GetPos(i));
                // sgdPos[sgdPos.Count-1] += .1f * new Vector2((float)sgdRand.NextDouble(), (float)sgdRand.NextDouble());

                sgdPos.Add(new Vector3((float)sgdRand.NextDouble(), (float)sgdRand.NextDouble(), (float)sgdRand.NextDouble()));
                if (sgdKeepY) {
                    sgdKeptY.Add(YConstraint(i));
                }
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
        }

        struct StressTerm {
            public int i, j;
            public float d, w;
        }
        static List<StressTerm> sgdTerms = new List<StressTerm>();
        public static void LayoutSGD(int t_max=10, float eps=.1f)
        {
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
            sgdKeptY.TrimExcess();

            // init y-position if constrained
            if (sgdKeepY) {
                for (int i=0; i<sgdPos.Count; i++) {
                    sgdPos[i] = new Vector3(sgdPos[i].x, sgdKeptY[i], sgdPos[i].z);
                }
            }
            var etas = new List<float>(ExpoSchedule(d_max*d_max, t_max, eps));
            var zMags = new List<float>(ExpoSchedule(1, t_max, zMagMin));
            for (int t=0; t<t_max; t++)
            {
                float eta = etas[t];
                float zMag = zMags[t];
                // UnityEngine.Debug.Log($"{eta} {zMag}");

                FYShuffle(sgdTerms, sgdRand);
                foreach (var term in sgdTerms)
                {
                    Vector3 X_ij = sgdPos[term.i] - sgdPos[term.j];

                    // float mag = X_ij.magnitude;
                    // float mag = Mathf.Sqrt(X_ij.x*X_ij.x + X_ij.y*X_ij.y + zMag*X_ij.z*X_ij.z);
                    float mag = Mathf.Sqrt(X_ij.x*X_ij.x + X_ij.y*X_ij.y);

                    float mu = Math.Min(term.w * eta, 1f);
                    Vector3 r = ((mag-term.d)/2f) * (X_ij/mag);
                    if (sgdKeepY) {
                        r.y = 0;
                    }

                    Assert.IsFalse(float.IsNaN(r.x) || float.IsNaN(r.y), $"r=NaN for SGD term {term.i}:{term.j}");
                   
                    sgdPos[term.i] -= mu * r;
                    sgdPos[term.j] += mu * r;
                }

                // // clamp back y position
                // if (keepY) {
                //     for (int i=0; i<sgdPos.Count; i++) {
                //         sgdPos[i] = new Vector3(sgdPos[i].x, Mathf.Lerp(sgdPos[i].y, sgdKeptY[i], yLerp), sgdPos[i].z);
                //     }
                // }
            }
        }
        static readonly float zMagMin=.001f, yLerp=1f;


        private static IEnumerable<float> ExpoSchedule(float eta_max, int t_max, float eps)
        {
            float lambda = Mathf.Log(eta_max/eps) / (t_max-1);
            for (int t=0; t<t_max; t++)
            {
                yield return eta_max * Mathf.Exp(-lambda * t);
            }
        }
        public static void RewriteSGD(Action<int, Vector2> SetPos)
        {
            for (int i=0; i<sgdPos.Count; i++)
            {
                SetPos(sgdUnsquished[i], sgdPos[i]);
            }
            UnityEngine.Debug.Log("TODO: do a procrustes analysis forgetting about the old connected components and using only sgdPos");
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
