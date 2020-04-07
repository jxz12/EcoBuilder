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

        // for components and procrustes
        private static List<int> sgdComponents = new List<int>();

        static List<Vector2> sgdPos = new List<Vector2>();
        static List<Vector2> sgdPosOld = new List<Vector2>();
        static System.Random sgdRand;
        public static void Init(Dictionary<int, HashSet<int>> undirected, int seed=0)
        {
            sgdSquished.Clear();
            sgdUnsquished.Clear();
            sgdComponents.Clear();

            // reset node positions
            sgdPosOld.Clear();
            var temp = sgdPosOld;
            sgdPosOld = sgdPos;
            sgdPos = temp;

            sgdRand = new System.Random(seed);
            foreach (int i in undirected.Keys)
            {
                sgdSquished[i] = sgdUnsquished.Count;
                sgdUnsquished.Add(i);

                sgdPos.Add(new Vector3((float)sgdRand.NextDouble(), (float)sgdRand.NextDouble(), (float)sgdRand.NextDouble()));
                sgdComponents.Add(-1);
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

            // clean up excess in case nodes are remoed
            sgdPos.TrimExcess();
            sgdUnsquished.TrimExcess();
            sgdSources.TrimExcess();
            sgdTargets.TrimExcess();
        }

        struct StressTerm {
            public int i, j;
            public float d, w;
        }
        static List<StressTerm> sgdTerms = new List<StressTerm>();
        public static void SolveStress(int t_max, float eps, Func<int, float> YConstraint=null)
        {
            FindStressTerms();
            FindConnectedComponents();

            var etas = new List<float>(ExpoSchedule(d_max*d_max, t_max, eps));
            PerformSGD(etas);
            SeparateConnectedComponents();
            Procrustes();
        }
        static int d_max;
        static void FindStressTerms()
        {
            // calculate terms with BFS
            sgdTerms.Clear();
            d_max = 0;
            var q = new Queue<int>();
            var d = new Dictionary<int, int>();
            for (int source=0; source<sgdSources.Count-1; source++)
            {
                // BFS for each node
                d.Clear();
                d[source] = 0;
                q.Enqueue(source);
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

            // in case nodes are deleted
            sgdTerms.TrimExcess();
        }
        public static int NumComponents { get; private set; } = 0;
        static void FindConnectedComponents()
        {
            // calculate connected components
            int ncc = 0;
            for (int source=0; source<sgdSources.Count-1; source++)
            {
                if (sgdComponents[source] != -1) {
                    continue;
                }
                sgdComponents[source] = ncc;

                var q = new Queue<int>();
                q.Enqueue(source);
                while (q.Count > 0)
                {
                    int prev = q.Dequeue();
                    for (int i=sgdSources[prev]; i<sgdSources[prev+1]; i++)
                    {
                        int next = sgdTargets[i];
                        if (sgdComponents[next] == -1) // if not seen yet
                        {
                            // Assert.IsFalse(componentMap.ContainsKey(next), $"{next} already in explored component");
                            sgdComponents[next] = ncc;
                            q.Enqueue(next);
                        }
                    }
                }
                ncc += 1;
            }
            NumComponents = ncc;
        }
        private static IEnumerable<float> ExpoSchedule(float eta_max, int t_max, float eps)
        {
            float lambda = Mathf.Log(eta_max/eps) / (t_max-1);
            for (int t=0; t<t_max; t++)
            {
                yield return eta_max * Mathf.Exp(-lambda * t);
            }
        }
        static readonly float muMax=1f;
        static void PerformSGD(List<float> etas)
        {
            int t_max = etas.Count;
            for (int t=0; t<t_max; t++)
            {
                float eta = etas[t];

                FYShuffle(sgdTerms, sgdRand);
                foreach (var term in sgdTerms)
                {
                    Vector2 X_ij = sgdPos[term.i] - sgdPos[term.j];
                    float mag = X_ij.magnitude;

                    float mu = Math.Min(term.w * eta, muMax);
                    Vector2 r = ((mag-term.d)/2f) * (X_ij/mag);

                    Assert.IsFalse(float.IsNaN(r.x) || float.IsNaN(r.y), $"r=NaN for SGD term {term.i}:{term.j}");
                   
                    sgdPos[term.i] -= mu * r;
                    sgdPos[term.j] += mu * r;
                }
            }
        }

        static readonly float zMagMin=.01f;
        static void PerformSGDConstrained(List<float> etas, Func<int, float> YConstraint)
        {
            /*
            int t_max = etas.Count;
            // init y-position if constrained
            float yMultiplier = 1;
            if (YConstraint != null) {
                for (int i=0; i<sgdPos.Count; i++) {
                    sgdPos[i] = new Vector3(sgdPos[i].x, YConstraint(i), sgdPos[i].z);
                }
                yMultiplier = 0;
            }
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
                    float mag = Mathf.Sqrt(X_ij.x*X_ij.x + X_ij.y*X_ij.y + zMag*X_ij.z*X_ij.z);
                    // float mag = Mathf.Sqrt(X_ij.x*X_ij.x + X_ij.y*X_ij.y);

                    float mu = Math.Min(term.w * eta, muMax);
                    Vector3 r = ((mag-term.d)/2f) * (X_ij/mag);

                    // for constraining y
                    r.y *= yMultiplier;

                    Assert.IsFalse(float.IsNaN(r.x) || float.IsNaN(r.y), $"r=NaN for SGD term {term.i}:{term.j}");
                   
                    sgdPos[term.i] -= mu * r;
                    sgdPos[term.j] += mu * r;
                }
            }
            */
        }
        public static void RewriteSGD(Action<int, Vector2> SetPos)
        {
            for (int i=0; i<sgdPos.Count; i++)
            {
                SetPos(sgdUnsquished[i], sgdPos[i]);
            }
        }
        private static void Procrustes()
        {

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


        //////////////////////////////////////////////////////////////
        // to separate components in layout and calculate disjointness

        private static void SeparateConnectedComponents()
        {
            int ncc = NumComponents;
            if (ncc <= 1) { // don't change layout if ncc is 0 or 1
                return;
            }

            // min and max for each component
            var ranges = new float[ncc, 2];
            for (int i=0; i<ncc; i++)
            {
                ranges[i,0] = float.MaxValue;
                ranges[i,1] = float.MinValue;
            }
            // foreach (int idx in componentMap.Keys)
            for (int i=0; i<sgdComponents.Count; i++)
            {
                int cc = sgdComponents[i];
                var pos = sgdPos[i];
                ranges[cc,0] = Mathf.Min(ranges[cc,0], pos.x);
                ranges[cc,1] = Mathf.Max(ranges[cc,1], pos.x);
            }
            var offsets = new float[ncc];
            offsets[0] = -ranges[0,0]; // move first CC to start at x=0
            float cumul = ranges[0,1] - ranges[0,0]; // end of CC range
            for (int i=1; i<ncc; i++)
            {
                offsets[i] = cumul - ranges[i,0] + 1;
                cumul += ranges[i,1] - ranges[i,0] + 1;
            }

            // place in order on x axis
            for (int i=0; i<sgdComponents.Count; i++)
            {
                int cc = sgdComponents[i];
                sgdPos[i] += new Vector2(offsets[cc], 0);
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
