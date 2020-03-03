using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Collections.Generic;

namespace EcoBuilder.NodeLink
{
    public partial class NodeLink
    {
        public int NumComponents { get; private set; } = 0;
        public int NumEdges { get; private set; } = 0;

        public float MaxTrophicLevel { get; private set; } = 0;
        public bool LaplacianDetZero { get; private set; } = false;

        private List<int> TallestNodes { get; set; }
        public int MaxChain { get; private set; } = 0;
        public int NumMaxChain { get; private set; } = 0;

        private List<int> LongestLoop { get; set; }
        public int MaxLoop { get { return LongestLoop==null? 0 : LongestLoop.Count; } }
        public int NumMaxLoop { get; private set; } = 0;


        //////////////////////////////////////////////////////////////
        // to separate components in layout and calculate disjointness

        private static Dictionary<int, int> componentMap = new Dictionary<int, int>();
        private void CountConnectedComponents()
        {
            componentMap.Clear();
            var q = new Queue<int>();
            int ncc = 0;
            foreach (int source in undirected.Keys)
            // foreach (int source in undirected.Keys.OrderBy(i=> nodes[i].StressPos.x))
            {
                if (componentMap.ContainsKey(source)) {
                    continue;
                }
                componentMap[source] = ncc;
                q.Clear();
                
                q.Enqueue(source);
                while (q.Count > 0)
                {
                    int prev = q.Dequeue();
                    foreach (int next in undirected[prev])
                    {
                        if (!componentMap.ContainsKey(next))
                        {
                            Assert.IsFalse(componentMap.ContainsKey(next), $"{next} already in explored component");
                            componentMap[next] = ncc;
                            q.Enqueue(next);
                        }
                    }
                }
                ncc += 1;
            }
            NumComponents = ncc;
        }
        private void SeparateConnectedComponents()
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
            foreach (int idx in componentMap.Keys)
            {
                int cc = componentMap[idx];
                var pos = nodes[idx].StressPos;
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
            foreach (int idx in componentMap.Keys)
            {
                int cc = componentMap[idx];
                nodes[idx].StressPos += new Vector2(offsets[cc], 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////
        // for trophic level calculation (and chain length as a consequence)

        private void RefreshTrophicAndFindChain(int nIterGS=0)
        {
            HashSet<int> basal = BuildTrophicEquations();
            for (int i=0; i<nIterGS; i++) {
                TrophicGaussSeidel();
            }
            MoveNodesToTrophicLevel();

            CalculateChainLengths(basal);
            LaplacianDetZero = (chainLengths.Count != nodes.Count);

            MaxChain = 0;
            NumMaxChain = 0;
            foreach (int height in chainLengths.Values)
            {
                if (height == MaxChain)
                {
                    NumMaxChain += 1;
                }
                else if (height > MaxChain)
                {
                    MaxChain = height;
                    NumMaxChain = 1;
                }
            }
            TallestNodes = new List<int>();
            foreach (int idx in chainLengths.Keys) {
                if (chainLengths[idx] == MaxChain) {
                    TallestNodes.Add(idx);
                }
            }
        }

        public bool ConstrainTrophic { private get; set; }
        private SparseVector<float> trophicA = new SparseVector<float>(); // we can assume all matrix values are equal, so only need a vector here
        private SparseVector<float> trophicLevels = new SparseVector<float>();

        // update the system of linear equations (Laplacian)
        // return set of roots to the tree
        private HashSet<int> BuildTrophicEquations()
        {
            foreach (int i in nodes.Indices) {
                trophicA[i] = 0;
            }
            foreach (var ij in links.IndexPairs)
            {
                int res=ij.Item1, con=ij.Item2;
                trophicA[con] += 1f;
            }

            var basal = new HashSet<int>();
            foreach (int i in nodes.Indices)
            {
                if (trophicA[i] != 0) {
                    trophicA[i] = -1f / trophicA[i]; // ensures diagonal dominance
                } else {
                    basal.Add(i);
                }
            }
            return basal;
        }

        // tightly optimised gauss-seidel iteration because of the simplicity of the laplacian
        private void TrophicGaussSeidel()
        {
            SparseVector<float> temp = new SparseVector<float>();
            foreach (var ij in links.IndexPairs)
            {
                int res = ij.Item1, con = ij.Item2;
                temp[con] += trophicA[con] * trophicLevels[res];
            }
            MaxTrophicLevel = 0;
            foreach (int i in nodes.Indices)
            {
                trophicLevels[i] = (1 - temp[i]);
                MaxTrophicLevel = Mathf.Max(trophicLevels[i], MaxTrophicLevel);
            }
        }

        // different BFS because it is directed
        private Dictionary<int, int> chainLengths = new Dictionary<int, int>();
        private void CalculateChainLengths(IEnumerable<int> sources)
        {
            chainLengths.Clear();
            var q = new Queue<int>();
            foreach (int source in sources)
            {
                q.Enqueue(source);
                chainLengths[source] = 0;
            }
            while (q.Count > 0)
            {
                int prev = q.Dequeue();
                foreach (int next in links.GetColumnIndicesInRow(prev))
                {
                    if (!chainLengths.ContainsKey(next))
                    {
                        q.Enqueue(next);
                        chainLengths[next] = chainLengths[prev] + 1;
                    }
                }
            }
        }


        /////////////////////////
        // for trophic coherence

        float CalculateOmnivory()
        {
            int L = 0;
            float sum_x_sqr = 0;
            foreach (Link li in links)
            {
                L += 1;
                int res = li.Source.Idx, con = li.Target.Idx;
                float x = trophicLevels[res] - trophicLevels[con];
                sum_x_sqr += x * x;
            }
            return (float)Math.Sqrt(sum_x_sqr - 1);
        }
    }
}
