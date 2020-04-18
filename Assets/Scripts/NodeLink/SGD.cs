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

        // store indices and their components
        static Dictionary<int, int> sgdSquished = new Dictionary<int, int>();
        static List<int> sgdUnsquished = new List<int>();

        // positions will be rewritten into this dictionary
        static Dictionary<int, Vector2> sgdResult = new Dictionary<int, Vector2>();

        // for BFS, SGD, NCC
        static List<int> sgdSources = new List<int>();
        static List<int> sgdTargets = new List<int>();
        static List<Vector2> sgdPos = new List<Vector2>();
        static List<int> sgdComponents = new List<int>();
        static System.Random sgdRand;
        public static void Init(Dictionary<int, HashSet<int>> undirected, int seed=0)
        {
            sgdSquished.Clear();
            sgdUnsquished.Clear();
            sgdComponents.Clear();

            // reset node positions
            sgdPos.Clear();
            sgdRand = new System.Random(seed);
            foreach (int i in undirected.Keys)
            {
                sgdSquished[i] = sgdUnsquished.Count;
                sgdUnsquished.Add(i);

                // sgdPos.Add(new Vector3((float)sgdRand.NextDouble(), (float)sgdRand.NextDouble(), (float)sgdRand.NextDouble()));
                sgdPos.Add(new Vector2((float)sgdRand.NextDouble(), (float)sgdRand.NextDouble()));
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

            // clean up excess in case nodes are removed
            sgdPos.TrimExcess();
            sgdUnsquished.TrimExcess();
            sgdSources.TrimExcess();
            sgdTargets.TrimExcess();
        }
        public static void SolveStress(int t_max, float eps, Func<int, float> YConstraint=null)
        {
            FindStressTerms();
            FindConnectedComponents();

            var etas = new List<float>(ExpoSchedule(d_max*d_max, t_max, eps));
            PerformSGD(etas);

            // move these functions into one, where the connected components are rotated based on a procrustes rotation between the intersection set of nodes between the previous layout
            MatchComponentsProcrustes();
            SeparateConnectedComponents();
        }
        public static void RewriteSGD(Action<int, Vector2> SetPos)
        {
            foreach (var kvp in sgdResult)
            {
                SetPos(kvp.Key, kvp.Value);
            }
        }

        struct StressTerm {
            public int i, j;
            public float d, w;
        }
        static List<StressTerm> sgdTerms = new List<StressTerm>();
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
        static void PerformSGD(IEnumerable<float> etas)
        {
            foreach (float eta in etas)
            {
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

        // reassigns positions, but flips components if it will preserve distances better
        static void MatchComponentsProcrustes()
        {
            // center the new positions first
            var newCentroids = new List<Vector2>();
            var newCounts = new List<int>();
            var oldCentroids = new List<Vector2>();
            var oldCounts = new List<int>();

            for (int cc=0; cc<NumComponents; cc++)
            {
                newCentroids.Add(Vector2.zero);
                newCounts.Add(0);
                oldCentroids.Add(Vector2.zero);
                oldCounts.Add(0);
            }
            for (int i=0; i<sgdPos.Count; i++)
            {
                int cc = sgdComponents[i];
                newCentroids[cc] += sgdPos[i];
                newCounts[cc] += 1;

                Vector2 prevPos;
                if (sgdResult.TryGetValue(sgdUnsquished[i], out prevPos))
                {
                    oldCentroids[cc] += prevPos;
                    oldCounts[cc] += 1;
                }
            }
            for (int cc=0; cc<NumComponents; cc++)
            {
                newCentroids[cc] /= newCounts[cc];
                if (oldCounts[cc] > 0) {
                    oldCentroids[cc] /= oldCounts[cc];
                }
            }
            for (int i=0; i<sgdPos.Count; i++)
            {
                int cc = sgdComponents[i];
                sgdPos[i] -= newCentroids[cc];

                int idx = sgdUnsquished[i];
                Vector2 oldPos;
                if (sgdResult.TryGetValue(idx, out oldPos)) {
                    sgdResult[idx] = oldPos - oldCentroids[cc];
                }
            }

            // then find optimal rotation (procrustes)
            float topSum = 0, botSum = 0;
            for (int i=0; i<sgdPos.Count; i++)
            {
                int idx = sgdUnsquished[i];
                Vector2 oldPos;
                if (sgdResult.TryGetValue(idx, out oldPos))
                {
                    Vector2 newPos = sgdPos[i];
                    topSum += oldPos.x*newPos.y - oldPos.y*newPos.x;
                    botSum += oldPos.x*newPos.x + oldPos.y*newPos.y;
                }
            }
            float angle = Mathf.Atan2(topSum, botSum);

            float sinA = Mathf.Sin(angle);
            float cosA = Mathf.Cos(angle);
            for (int i=0; i<sgdPos.Count; i++)
            {
                Vector2 newPos = sgdPos[i];
                sgdPos[i] = new Vector2(newPos.x*cosA - newPos.y*sinA, newPos.x*sinA + newPos.y*cosA);
            }
        }

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
            float yMin = float.MaxValue;
            for (int i=0; i<sgdComponents.Count; i++)
            {
                int cc = sgdComponents[i];
                var pos = sgdPos[i];
                ranges[cc,0] = Mathf.Min(ranges[cc,0], pos.x);
                ranges[cc,1] = Mathf.Max(ranges[cc,1], pos.x);
                yMin = Mathf.Min(yMin, pos.y);
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
                int idx = sgdUnsquished[i];
                int cc = sgdComponents[i];
                sgdResult[idx] = new Vector2(sgdPos[i].x + offsets[cc] - cumul/2, sgdPos[i].y - yMin);
            }
        }
    }
}
