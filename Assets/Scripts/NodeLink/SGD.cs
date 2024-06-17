using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace EcoBuilder.NodeLink
{
    public class SGD
    {
        ////////////////////////////
        // layout with SGD

        // store indices and their components
        Dictionary<int, int> idxSquished = new Dictionary<int, int>();
        List<int> idxUnsquished = new List<int>();

        // positions will be rewritten into this dictionary
        Dictionary<int, Vector2> posUnsquished = new Dictionary<int, Vector2>();

        // undirected graph data for BFS, SGD, NCC
        List<int> sources = new List<int>();
        List<int> targets = new List<int>();
        Vector2[] posSquished = new Vector2[0];
        List<int> componentMap = new List<int>();
        List<int> componentCounts = new List<int>();
        System.Random rand;
        public SGD()
        {
        }
        public void Init(Dictionary<int, HashSet<int>> undirected, int seed=0)
        {
            idxSquished.Clear();
            idxUnsquished.Clear();

            // reset node positions
            if (undirected.Count != posSquished.Length) {
                posSquished = new Vector2[undirected.Count];
            }
            rand = new System.Random(seed);
            foreach (int idx in undirected.Keys)
            {
                int squished = idxUnsquished.Count;
                idxSquished[idx] = squished;
                idxUnsquished.Add(idx);

                var randPos = new Vector2((float)rand.NextDouble(), (float)rand.NextDouble());
                // posUnsquished.TryGetValue(idx, out var initPos);
                // posSquished[squished] = initPos + randPos;
                posSquished[squished] = randPos;
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
            idxUnsquished.TrimExcess();
            sources.TrimExcess();
            targets.TrimExcess();
        }
        public void SolveStress(int t_max, float eps, Func<int, float> YConstraint)
        {
            FindConnectedComponents();
            FindStressTerms();

            bool yFixed = YConstraint != null;
            var etas = new List<float>(ExpoSchedule(d_max*d_max, t_max, eps));
            if (!yFixed) {
                PerformSGD(etas);
            } else {
                InitYConstraint(YConstraint);
                PerformSGDConstrained(etas);
            }
            MatchComponentsProcrustes(!yFixed);
            SeparateAndUnsquishComponents(!yFixed);
        }
        public void RewriteSGD(Action<int, Vector2> SetPos)
        {
            foreach (var kvp in posUnsquished) {
                SetPos(kvp.Key, kvp.Value);
            }
        }
        public void Clear()
        {
            posUnsquished.Clear();
        }

        struct StressTerm {
            public int i, j;
            public float d, w;
        }
        List<StressTerm> terms = new List<StressTerm>();
        int d_max;
        void FindStressTerms()
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
        public int NumComponents { get; private set; } = 0;
        void FindConnectedComponents()
        {
            // componentMap.Clear();
            // for (int i=0; i<posSquished.Length; i++) {
            //     componentMap.Add(i);
            // }
            // componentMap.TrimExcess();
            // // sort by x position to make sure components don't jump all over the place
            // componentMap.Sort((i,j)=> posSquished[i].x.CompareTo(posSquished[j].x));

            // calculate connected components
            componentMap.Clear();
            for (int i=0; i<posSquished.Length; i++) {
                componentMap.Add(-1);
            }
            componentMap.TrimExcess();

            componentCounts.Clear();
            int component = 0;
            for (int source=0; source<componentMap.Count; source++)
            {
                if (componentMap[source] >= 0) {
                    continue;
                }
                componentMap[source] = component;

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
                            componentMap[next] = component;
                            q.Enqueue(next);
                        }
                    }
                }
                component += 1;
                componentCounts.Add(0); // init to 0, count later
            }
            
            for (int i=0; i<componentMap.Count; i++) {
                componentCounts[componentMap[i]] += 1;
            }
            componentCounts.TrimExcess();
            NumComponents = component;
        }
        private IEnumerable<float> ExpoSchedule(float eta_max, int t_max, float eps)
        {
            float lambda = Mathf.Log(eta_max/eps) / (t_max-1);
            for (int t=0; t<t_max; t++)
            {
                yield return eta_max * Mathf.Exp(-lambda * t);
            }
        }
        const float muMax=1.1f;
        void PerformSGD(IList<float> etas)
        {
            int t_max = etas.Count;
            for (int t=-t_init; t<t_max; t++)
            {
                float eta = etas[Math.Max(0,t)];
                FYShuffle(terms, rand);
                foreach (var term in terms)
                {
                    Vector2 X_ij = posSquished[term.i] - posSquished[term.j];
                    float mag = X_ij.magnitude;

                    Vector2 r = ((mag-term.d)/2f) * (X_ij/mag);

                    Assert.IsFalse(float.IsNaN(r.x) || float.IsNaN(r.y), $"r=NaN for SGD term {term.i}:{term.j}");
                   
                    float mu = Math.Min(term.w * eta, muMax);
                    r *= mu;
                    posSquished[term.i] -= r;
                    posSquished[term.j] += r;
                }
            }
        }

        void InitYConstraint(Func<int, float> YConstraint)
        {
            for (int i=0; i<idxUnsquished.Count; i++)
            {
                posSquished[i] = new Vector2(posSquished[i].x, YConstraint(idxUnsquished[i]));
            }
        }
        const int t_init = 5;
        void PerformSGDConstrained(IList<float> etas)
        {
            int t_max = etas.Count;

            for (int t=-t_init; t<t_max; t++)
            {
                float eta = etas[Math.Max(0,t)];

                FYShuffle(terms, rand);
                foreach (var term in terms)
                {
                    Vector2 X_ij = posSquished[term.i] - posSquished[term.j];
                    float mag = X_ij.magnitude;

                    float rx = ((mag-term.d)/2f) * (X_ij.x/mag);
                    
                    // TODO: this is NaN in level 18
                    Assert.IsFalse(float.IsNaN(rx), $"rx=NaN for SGD term {term.i}:{term.j}");
                   
                    float mu = Math.Min(term.w * eta, muMax);
                    rx *= mu;
                    posSquished[term.i].x -= rx;
                    posSquished[term.j].x += rx;
                }
            }
        }
        public float CalculateStress()
        {
            float stress = 0;
            foreach (var term in terms)
            {
                var error = (posSquished[term.i] - posSquished[term.j]).magnitude;
                error -= term.d;
                stress += term.w * error*error;
            }
            return stress;
        }
        public void FYShuffle<T>(List<T> deck, System.Random rand)
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

        // optimal rotation by procrustes analysis
        void MatchComponentsProcrustes(bool rotate)
        {
            // center the new positions first ([n,0] is old, [n,1] is new)
            var centroids = new Vector2[NumComponents, 2];
            var overlapCounts = new int[NumComponents];

            // calculate centroids
            for (int i=0; i<componentMap.Count; i++)
            {
                // only centroid idxs in both prev and new layout
                Vector2 prevPos;
                if (posUnsquished.TryGetValue(idxUnsquished[i], out prevPos))
                {
                    int cc = componentMap[i];
                    centroids[cc,0] += prevPos;
                    centroids[cc,1] += posSquished[i];
                    overlapCounts[cc] += 1;
                }
            }
            // take average
            for (int cc=0; cc<NumComponents; cc++)
            {
                if (overlapCounts[cc] > 0) {
                    centroids[cc,0] /= overlapCounts[cc];
                    centroids[cc,1] /= overlapCounts[cc];
                }
            }
            // center individual components
            for (int i=0; i<componentMap.Count; i++)
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

            if (rotate)
            {
                // then find optimal rotation (procrustes)
                // this array initially contains top and botton sums, and then eventually contains sin/cos of rotations
                var procrustes = new float[NumComponents, 2];
                var setsurcorp = new float[NumComponents, 2]; // reflected angle
                for (int i=0; i<componentMap.Count; i++)
                {
                    int idx = idxUnsquished[i];
                    Vector2 oldPos; // oldPos is used as procrustes 'reference'
                    if (posUnsquished.TryGetValue(idx, out oldPos))
                    {
                        int cc = componentMap[i];
                        Vector2 newPos = posSquished[i];
                        procrustes[cc,0] += newPos.x*oldPos.y - newPos.y*oldPos.x;
                        procrustes[cc,1] += newPos.x*oldPos.x + newPos.y*oldPos.y;

                        newPos.x = -newPos.x; // reflect
                        setsurcorp[cc,0] += newPos.x*oldPos.y - newPos.y*oldPos.x;
                        setsurcorp[cc,1] += newPos.x*oldPos.x + newPos.y*oldPos.y;
                    }
                }
                // get rotation matrix
                for (int cc=0; cc<NumComponents; cc++)
                {
                    if (procrustes[cc,1] != 0) // prevent divide by zero
                    {
                        float angle = Mathf.Atan2(procrustes[cc,0], procrustes[cc,1]);
                        procrustes[cc,0] = Mathf.Cos(angle);
                        procrustes[cc,1] = Mathf.Sin(angle);

                        angle = Mathf.Atan2(setsurcorp[cc,0], setsurcorp[cc,1]);
                        setsurcorp[cc,0] = Mathf.Cos(angle);
                        setsurcorp[cc,1] = Mathf.Sin(angle);
                    }
                    else
                    {
                        // identity matrix
                        procrustes[cc,0] = 1;
                        procrustes[cc,1] = 0;

                        setsurcorp[cc,0] = 1;
                        setsurcorp[cc,1] = 0;
                    }
                }
                // compare reflected and non-reflected errors
                var proDistances = new float[NumComponents, 2]; // [i,0] is unreflected, [i,1] is reflected
                for (int i=0; i<componentMap.Count; i++)
                {
                    int idx = idxUnsquished[i];
                    Vector2 oldPos; // oldPos is used as procrustes 'reference'
                    if (posUnsquished.TryGetValue(idx, out oldPos))
                    {
                        int cc = componentMap[i];
                        float cosA = procrustes[cc,0];
                        float sinA = procrustes[cc,1];
                        Vector2 newPos = posSquished[i];
                        Vector2 rotated = new Vector2(newPos.x*cosA - newPos.y*sinA, newPos.x*sinA + newPos.y*cosA);
                        proDistances[cc,0] += (oldPos-rotated).sqrMagnitude;

                        cosA = setsurcorp[cc,0];
                        sinA = setsurcorp[cc,1];
                        newPos.x = -newPos.x; // reflect
                        rotated = new Vector2(newPos.x*cosA - newPos.y*sinA, newPos.x*sinA + newPos.y*cosA);
                        proDistances[cc,1] += (oldPos-rotated).sqrMagnitude;
                    }
                }
                // choose either reflected or non-reflected rotation matrices
                for (int i=0; i<componentMap.Count; i++)
                {
                    int cc = componentMap[i];
                    float cosA, sinA;
                    Vector2 unrotated = posSquished[i];
                    if (proDistances[cc,0] <= proDistances[cc,1])
                    {
                        cosA = procrustes[cc,0];
                        sinA = procrustes[cc,1];
                    }
                    else
                    {
                        cosA = setsurcorp[cc,0];
                        sinA = setsurcorp[cc,1];
                        unrotated.x = -unrotated.x;
                    }
                    posSquished[i] = new Vector2(unrotated.x*cosA - unrotated.y*sinA, unrotated.x*sinA + unrotated.y*cosA);
                }
            }
            else
            {
                var proDistances = new float[NumComponents, 2]; // [i,0] is unreflected, [i,1] is reflected (around y-axis)
                for (int i=0; i<componentMap.Count; i++)
                {
                    int idx = idxUnsquished[i];
                    Vector2 oldPos; // oldPos is used as procrustes 'reference'
                    if (posUnsquished.TryGetValue(idx, out oldPos))
                    {
                        int cc = componentMap[i];
                        Vector2 newPos = posSquished[i];
                        proDistances[cc,0] += (oldPos-newPos).sqrMagnitude;

                        newPos = new Vector2(-newPos.x, newPos.y); // reflect
                        proDistances[cc,1] += (oldPos-newPos).sqrMagnitude;
                    }
                }
                // choose either reflected or non-reflected versions
                for (int i=0; i<componentMap.Count; i++)
                {
                    int cc = componentMap[i];
                    if (proDistances[cc,1] < proDistances[cc,0])
                    {
                        posSquished[i] = new Vector2(-posSquished[i].x, posSquished[i].y); // reflect
                    }
                }
            }
        }

        private void SeparateAndUnsquishComponents(bool fixK2)
        {
            if (NumComponents <= 0) {
                return;
            }
            if (fixK2)
            {
                for (int i=0; i<componentMap.Count; i++)
                {
                    int cc = componentMap[i];
                    if (componentCounts[cc] == 2)
                    {
                        posSquished[i] = new Vector2(-.35355f,-.35355f);
                        componentCounts[cc] = -2;
                    }
                    else if (componentCounts[cc] == -2) // hacky way to determine second vertex in cc
                    {
                        posSquished[i] = new Vector2(.35355f,.35355f);
                        componentCounts[cc] = 2;
                    }
                }
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
                offsets[cc] = cumul - minMaxes[cc,0] + .75f;
                cumul += minMaxes[cc,1] - minMaxes[cc,0] + .75f;
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
