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
        static Dictionary<int, int> idxSquished = new Dictionary<int, int>();
        static List<int> idxUnsquished = new List<int>();

        // positions will be rewritten into this dictionary
        static Dictionary<int, Vector2> posUnsquished = new Dictionary<int, Vector2>();

        // for BFS, SGD, NCC
        static List<int> sources = new List<int>();
        static List<int> targets = new List<int>();
        static List<Vector2> posSquished = new List<Vector2>();
        static List<int> componentMap = new List<int>();
        static System.Random rand;
        public static void Init(Dictionary<int, HashSet<int>> undirected, int seed=0)
        {
            idxSquished.Clear();
            idxUnsquished.Clear();

            // reset node positions
            posSquished.Clear();
            componentMap.Clear();
            rand = new System.Random(seed);
            foreach (int i in undirected.Keys)
            {
                idxSquished[i] = idxUnsquished.Count;
                idxUnsquished.Add(i);

                // posSquished.Add(new Vector3((float)sgdRand.NextDouble(), (float)sgdRand.NextDouble(), (float)sgdRand.NextDouble()));
                posSquished.Add(new Vector2((float)rand.NextDouble(), (float)rand.NextDouble()));
                componentMap.Add(-1);
            }
            sources.Clear();
            targets.Clear();
            foreach (int i in undirected.Keys)
            {
                sources.Add(targets.Count);
                foreach (int j in undirected[i])
                {
                    targets.Add(idxSquished[j]);
                }
            }
            sources.Add(targets.Count); // for iteration to next

            // clean up excess in case nodes are removed
            posSquished.TrimExcess();
            componentMap.TrimExcess();
            idxUnsquished.TrimExcess();
            sources.TrimExcess();
            targets.TrimExcess();
        }
        public static void SolveStress(int t_max, float eps, Func<int, float> YConstraint=null)
        {
            FindStressTerms();

            var etas = new List<float>(ExpoSchedule(d_max*d_max, t_max, eps));
            PerformSGD(etas);

            // move these functions into one, where the connected components are rotated based on a procrustes rotation between the intersection set of nodes between the previous layout
            FindConnectedComponents();
            MatchComponentsProcrustes();
            SeparateConnectedComponents();
        }
        public static void RewriteSGD(Action<int, Vector2> SetPos)
        {
            foreach (var kvp in posUnsquished)
            {
                SetPos(kvp.Key, kvp.Value);
            }
        }

        struct StressTerm {
            public int i, j;
            public float d, w;
        }
        static List<StressTerm> terms = new List<StressTerm>();
        static int d_max;
        static void FindStressTerms()
        {
            // calculate terms with BFS
            terms.Clear();
            d_max = 0;
            var q = new Queue<int>();
            var d = new Dictionary<int, int>();
            for (int source=0; source<sources.Count-1; source++)
            {
                // BFS for each node
                d.Clear();
                d[source] = 0;
                q.Enqueue(source);
                while (q.Count > 0)
                {
                    int prev = q.Dequeue();
                    for (int i=sources[prev]; i<sources[prev+1]; i++)
                    {
                        int next = targets[i];
                        if (!d.ContainsKey(next))
                        {
                            d[next] = d[prev] + 1;
                            q.Enqueue(next);

                            if (source < next) // only add every other term
                            {
                                terms.Add(new StressTerm () {
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
            terms.TrimExcess();
        }
        public static int NumComponents { get; private set; } = 0;
        static void FindConnectedComponents()
        {
            // calculate connected components
            int ncc = 0;
            for (int source=0; source<sources.Count-1; source++)
            {
                if (componentMap[source] != -1) {
                    continue;
                }
                componentMap[source] = ncc;

                var q = new Queue<int>();
                q.Enqueue(source);
                while (q.Count > 0)
                {
                    int prev = q.Dequeue();
                    for (int i=sources[prev]; i<sources[prev+1]; i++)
                    {
                        int next = targets[i];
                        if (componentMap[next] == -1) // if not seen yet
                        {
                            componentMap[next] = ncc;
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
                FYShuffle(terms, rand);
                foreach (var term in terms)
                {
                    Vector2 X_ij = posSquished[term.i] - posSquished[term.j];
                    float mag = X_ij.magnitude;

                    float mu = Math.Min(term.w * eta, muMax);
                    Vector2 r = ((mag-term.d)/2f) * (X_ij/mag);

                    Assert.IsFalse(float.IsNaN(r.x) || float.IsNaN(r.y), $"r=NaN for SGD term {term.i}:{term.j}");
                   
                    posSquished[term.i] -= mu * r;
                    posSquished[term.j] += mu * r;
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
                for (int i=0; i<posSquished.Count; i++) {
                    posSquished[i] = new Vector3(posSquished[i].x, YConstraint(i), posSquished[i].z);
                }
                yMultiplier = 0;
            }
            var zMags = new List<float>(ExpoSchedule(1, t_max, zMagMin));
            for (int t=0; t<t_max; t++)
            {
                float eta = etas[t];
                float zMag = zMags[t];
                // UnityEngine.Debug.Log($"{eta} {zMag}");

                FYShuffle(terms, sgdRand);
                foreach (var term in terms)
                {
                    Vector3 X_ij = posSquished[term.i] - posSquished[term.j];

                    // float mag = X_ij.magnitude;
                    float mag = Mathf.Sqrt(X_ij.x*X_ij.x + X_ij.y*X_ij.y + zMag*X_ij.z*X_ij.z);
                    // float mag = Mathf.Sqrt(X_ij.x*X_ij.x + X_ij.y*X_ij.y);

                    float mu = Math.Min(term.w * eta, muMax);
                    Vector3 r = ((mag-term.d)/2f) * (X_ij/mag);

                    // for constraining y
                    r.y *= yMultiplier;

                    Assert.IsFalse(float.IsNaN(r.x) || float.IsNaN(r.y), $"r=NaN for SGD term {term.i}:{term.j}");
                   
                    posSquished[term.i] -= mu * r;
                    posSquished[term.j] += mu * r;
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


        // reassigns positions, but flips components if it will preserve distances better
        static void MatchComponentsProcrustes()
        {
            // center the new positions first ([n,0] is old, [n,1] is new)
            var centroids = new Vector2[NumComponents, 2];
            var counts = new int[NumComponents];

            // calculate centroids
            for (int i=0; i<posSquished.Count; i++)
            {
                // only centroid idxs in both prev and new layout
                Vector2 prevPos;
                if (posUnsquished.TryGetValue(idxUnsquished[i], out prevPos))
                {
                    int cc = componentMap[i];
                    centroids[cc,0] += prevPos;
                    centroids[cc,1] += posSquished[i];
                    counts[cc] += 1;
                }
            }
            // take average
            for (int cc=0; cc<NumComponents; cc++)
            {
                if (counts[cc] > 0) {
                    centroids[cc,0] /= counts[cc];
                    centroids[cc,1] /= counts[cc];
                }
            }
            // center individual components
            for (int i=0; i<posSquished.Count; i++)
            {
                // center all idxs regardless of if they were included in centroid calculation
                int cc = componentMap[i];
                posSquished[i] -= centroids[cc,1];

                int idx = idxUnsquished[i];
                Vector2 oldPos;
                if (posUnsquished.TryGetValue(idx, out oldPos)) {
                    posUnsquished[idx] = oldPos - centroids[cc,0];
                }
            }

            // then find optimal rotation (procrustes)
            // this array initially contains top and botton sums, and then eventually contains sin/cos of rotations
            var procrustes = new float[NumComponents, 2];
            for (int i=0; i<posSquished.Count; i++)
            {
                int idx = idxUnsquished[i];
                Vector2 oldPos;
                if (posUnsquished.TryGetValue(idx, out oldPos))
                {
                    int cc = componentMap[i];
                    Vector2 newPos = posSquished[i];
                    procrustes[cc,0] += oldPos.x*newPos.y - oldPos.y*newPos.x;
                    procrustes[cc,1] += oldPos.x*newPos.x + oldPos.y*newPos.y;
                }
            }
            // get rotation matrix
            for (int cc=0; cc<NumComponents; cc++)
            {
                if (procrustes[cc,1] != 0) // prevent divide by zero
                {
                    float angle = Mathf.Atan2(-procrustes[cc,0], procrustes[cc,1]);
                    procrustes[cc,0] = Mathf.Cos(angle);
                    procrustes[cc,1] = Mathf.Sin(angle);
                }
                else
                {
                    // identity matrix
                    procrustes[cc,0] = 1;
                    procrustes[cc,1] = 0;
                }
            }
            // perform rotation
            for (int i=0; i<posSquished.Count; i++)
            {
                int cc = componentMap[i];
                float cosA = procrustes[cc,0];
                float sinA = procrustes[cc,1];
                Vector2 newPos = posSquished[i];
                posSquished[i] = new Vector2(newPos.x*cosA - newPos.y*sinA, newPos.x*sinA + newPos.y*cosA);

                Vector2 oldPos;
                posUnsquished.TryGetValue(idxUnsquished[i], out oldPos);
            }
        }

        private static void SeparateConnectedComponents()
        {
            if (NumComponents <= 0) {
                return;
            }
            // xMin, xMax, yMin for each component
            var minMaxes = new float[NumComponents, 3];
            for (int cc=0; cc<NumComponents; cc++)
            {
                minMaxes[cc,0] = float.MaxValue;
                minMaxes[cc,1] = float.MinValue;
                minMaxes[cc,2] = float.MaxValue;
            }
            for (int i=0; i<componentMap.Count; i++)
            {
                int cc = componentMap[i];
                var pos = posSquished[i];
                minMaxes[cc,0] = Mathf.Min(minMaxes[cc,0], pos.x);
                minMaxes[cc,1] = Mathf.Max(minMaxes[cc,1], pos.x);
                minMaxes[cc,2] = Mathf.Min(minMaxes[cc,2], pos.y);
            }
            var offsets = new float[NumComponents];
            offsets[0] = -minMaxes[0,0]; // move first CC to start at x=0
            float cumul = minMaxes[0,1] - minMaxes[0,0]; // end of CC range
            for (int cc=1; cc<NumComponents; cc++)
            {
                offsets[cc] = cumul - minMaxes[cc,0] + 1;
                cumul += minMaxes[cc,1] - minMaxes[cc,0] + 1;
            }

            // place in order on x axis
            posUnsquished.Clear();
            float radius = cumul / 2f;
            for (int i=0; i<componentMap.Count; i++)
            {
                int idx = idxUnsquished[i];
                int cc = componentMap[i];
                posUnsquished[idx] = new Vector2(posSquished[i].x + offsets[cc] - radius, posSquished[i].y - minMaxes[cc,2]);
            }
        }
    }
}
