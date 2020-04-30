using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace EcoBuilder.NodeLink
{
    ///////////////////////////////////////////////////////////////////////
    // for trophic level calculation (and chain length as a consequence)
    public static class Trophic
    {
        public static bool LaplacianDetZero { get; private set; } = false;
        public static int MaxChain { get; private set; } = 0;
        public static IEnumerable<int> MaxChainIndices {
            get { return tallestNodes; }
        }
        public static int NumMaxChain {
            get { return tallestNodes.Count; }
        }
        public static float MaxTrophicLevel { get; private set; } = 0;

        private static List<int> tallestNodes = new List<int>();
        private static Dictionary<int,int> chainLengths = new Dictionary<int, int>();

        private static IEnumerable<int> indices;
        private static Func<int, IEnumerable<int>> Targets;

        private static SparseVector<float> trophicA = new SparseVector<float>(); // we can assume all matrix values are equal, so only need a vector here
        private static SparseVector<float> trophicLevels = new SparseVector<float>();
        public static void Init(IEnumerable<int> nodeIndices, Func<int, IEnumerable<int>> GetTargets)
        {
            indices = nodeIndices;
            Targets = GetTargets;

            // update the system of linear equations (Laplacian)
            int n = 0;
            foreach (int i in indices)
            {
                trophicA[i] = 0;
                n += 1;
            }
            
            foreach (int i in indices) 
            {
                foreach (var j in Targets(i))
                {
                    trophicA[j] += 1f;
                }
            }
            // finalise the off-diagonals and also get basal nodes
            var basal = new HashSet<int>();
            foreach (int i in indices)
            {
                if (trophicA[i] != 0) {
                    trophicA[i] = -1f / trophicA[i]; // ensures diagonal dominance
                } else {
                    basal.Add(i);
                }
            }

            // then use basal species as roots to chain 
            var q = new Queue<int>();
            chainLengths.Clear();
            foreach (int source in basal)
            {
                q.Enqueue(source);
                chainLengths[source] = 0;
            }
            MaxChain = 0;
            while (q.Count > 0)
            {
                int prev = q.Dequeue();
                foreach (int next in Targets(prev))
                {
                    if (!chainLengths.ContainsKey(next))
                    {
                        q.Enqueue(next);
                        chainLengths[next] = chainLengths[prev] + 1;
                        MaxChain = Math.Max(MaxChain, chainLengths[next]);
                    }
                }
            }
            LaplacianDetZero = (chainLengths.Count != n);

            tallestNodes.Clear();
            foreach (int idx in chainLengths.Keys) {
                if (chainLengths[idx] == MaxChain) {
                    tallestNodes.Add(idx);
                }
            }
        }
        // public to allow outside to force convergence
        private static float trophicScaling = 1;
        public static void SolveTrophic(float eps=.01f)
        {
            if (LaplacianDetZero) {
                return;
            }
            bool converged = false;
            int iter=0;
            while (!converged) {
                converged = TrophicGaussSeidel(eps);
                iter++;
            }
#if UNITY_EDITOR
            if (iter >= 10) {
                Debug.Log($"gsIter: {iter}");
            }
#endif
            if (MaxTrophicLevel-1 > MaxChain) {
                trophicScaling = MaxChain / (MaxTrophicLevel-1);
            } else {
                trophicScaling = 1;
            }
        }
        public static void IterateTrophic(Action<int, float> SetY)
        {
            SolveTrophic(float.MaxValue); // ensures only 1 iteration
        }

        // tightly optimised gauss-seidel iteration because of the simplicity of the laplacian
        // returns if converged
        private static bool TrophicGaussSeidel(float eps)
        {
            Assert.IsNotNull(indices);
            Assert.IsNotNull(Targets);
            SparseVector<float> temp = new SparseVector<float>();
            foreach (int i in indices)
            {
                foreach (int j in Targets(i))
                {
                    temp[j] += trophicA[j] * trophicLevels[i];
                }
            }
            bool converged = true;
            MaxTrophicLevel = 0;
            foreach (int i in indices)
            {
                float newTrophicLevel = (1-temp[i]);
                converged &= Mathf.Abs(trophicLevels[i]-newTrophicLevel) < eps;

                trophicLevels[i] = (1 - temp[i]);
                MaxTrophicLevel = Mathf.Max(trophicLevels[i], MaxTrophicLevel);
            }
            return converged;
        }
        public static float GetScaledTrophicLevel(int idx)
        {
            return trophicScaling * (trophicLevels[idx]-1);
        }
        public static int GetChainLength(int idx)
        {
            Assert.IsTrue(chainLengths.ContainsKey(idx));
            return chainLengths[idx];
        }

        /////////////////////////
        // for trophic coherence

        public static float CalculateOmnivory()
        {
            Assert.IsNotNull(indices);
            Assert.IsNotNull(Targets);
            int L = 0;
            float sum_x_sqr = 0;
            foreach (int i in indices)
            {
                foreach (int j in Targets(i))
                {
                    L += 1;
                    // int res = li.Source.Idx, con = li.Target.Idx;
                    float x = trophicLevels[i] - trophicLevels[j];
                    sum_x_sqr += x * x;
                }
            }
            return Mathf.Sqrt(sum_x_sqr - 1);
        }
    }
}