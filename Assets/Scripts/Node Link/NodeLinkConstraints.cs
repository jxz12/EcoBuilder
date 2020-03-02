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

        ///////////////////////////////////
        // loops with Johnson's algorithm

        static Dictionary<int, HashSet<int>> johnsonIncoming = new Dictionary<int, HashSet<int>>();
        static Dictionary<int, HashSet<int>> johnsonOutgoing = new Dictionary<int, HashSet<int>>();
        static void JohnsonInit(IEnumerable<int> indices, IEnumerable<Tuple<int, int>> indexPairs)
        {
            // a function to initialise outgoing and incoming edges for johnson's algorithm below
            // ignores 'graveyard' species

            Assert.IsTrue(johnsonOutgoing.Count == 0, "Johnson DFS did not clear its graph");
            Assert.IsTrue(johnsonIncoming.Count == 0, "Johnson DFS did not clear its graph");

            foreach (int i in indices)
            {
                johnsonOutgoing[i] = new HashSet<int>();
                johnsonIncoming[i] = new HashSet<int>();
            }
            foreach (var ij in indexPairs)
            {
                int i=ij.Item1, j=ij.Item2;
                johnsonOutgoing[i].Add(j);
                johnsonIncoming[j].Add(i);
            }
        }

        // from https://github.com/mission-peace/interview/blob/master/src/com/interview/graph/AllCyclesInDirectedGraphJohnson.java
        // can be very slow, so run async if possible
        static List<int> johnsonLongestLoop = new List<int>();
        static int johnsonNumLongest = 0;
        static List<int> JohnsonsAlgorithm()
        {
            johnsonLongestLoop = new List<int>(); // empty list is no loop
            var indices = new List<int>(johnsonOutgoing.Keys);
            foreach (int idx in indices)
            {
                // creates the outgoing subset
                JohnsonSCC(idx);

                // does the DFS on that subset
                JohnsonSingleSource(idx);

                // remove node from graph to not need to consider again
                johnsonOutgoing.Remove(idx);
                johnsonIncoming.Remove(idx);
                foreach (var set in johnsonOutgoing.Values) {
                    set.Remove(idx);
                }
                foreach (var set in johnsonIncoming.Values) {
                    set.Remove(idx);
                }
            }
            johnsonLongestLoop.Reverse();
            return new List<int>(johnsonLongestLoop);
        }
        static Stack<int> johnsonStack = new Stack<int>();
        static HashSet<int> johnsonSet = new HashSet<int>();
        static Dictionary<int, HashSet<int>> johnsonMap = new Dictionary<int, HashSet<int>>();
        static void JohnsonSingleSource(int source)
        {
            johnsonStack.Clear();
            johnsonSet.Clear();
            johnsonMap.Clear();
            foreach (int i in johnsonOutgoingSubset.Keys) {
                johnsonMap[i] = new HashSet<int>();
            }
            JohnsonDFS(source, source);
        }
        static bool JohnsonDFS(int source, int current)
        {
            bool foundCycle = false;
            johnsonStack.Push(current);
            johnsonSet.Add(current);

            foreach (int next in johnsonOutgoingSubset[current])
            {
                if (next == source) // found cycle, so see if it is longest
                {
                    if (johnsonStack.Count == johnsonLongestLoop.Count)
                    {
                        johnsonNumLongest += 1;
                    }
                    else if (johnsonStack.Count > johnsonLongestLoop.Count) {
                        johnsonLongestLoop = new List<int>(johnsonStack);
                        johnsonNumLongest = 1;
                    }
                    foundCycle = true;
                }
                else if (!johnsonSet.Contains(next))
                {
                    // if a cycle is found in any descendants, then no need to search again
                    foundCycle |= JohnsonDFS(source, next);
                }
            }
            // if found a path to source, then remove from set and (recursively) from map
            if (foundCycle)
            {
                JohnsonUnblock(current);
            } 
            else
            {
                // else do not unblock, add to map so that it will be unblocked in the future
                foreach (int next in johnsonOutgoingSubset[current]) {
                    johnsonMap[next].Add(current);
                }
            }
            johnsonStack.Pop();
            return foundCycle;
        }
        static void JohnsonUnblock(int toUnblock)
        {
            // recursively unblock everything on path that we are freeing up
            johnsonSet.Remove(toUnblock);
            foreach (int toAlsoUnblock in johnsonMap[toUnblock]) {
                if (johnsonSet.Contains(toAlsoUnblock)) {
                    JohnsonUnblock(toAlsoUnblock);
                }
            }
            johnsonMap[toUnblock].Clear();
        }


        // returns the strongly connected component including idx
        static Dictionary<int, HashSet<int>> johnsonOutgoingSubset = new Dictionary<int, HashSet<int>>();
        static HashSet<int> johnsonComponentOut = new HashSet<int>();
        static HashSet<int> johnsonComponentIn = new HashSet<int>();
        static void JohnsonSCC(int idx)
        {
            JohnsonWCC(idx, johnsonOutgoing, johnsonComponentOut);
            JohnsonWCC(idx, johnsonIncoming, johnsonComponentIn);
            johnsonComponentOut.IntersectWith(johnsonComponentIn);

            johnsonOutgoingSubset.Clear();
            foreach (int i in johnsonComponentOut)
            {
                johnsonOutgoingSubset[i] = new HashSet<int>();
                foreach (int j in johnsonOutgoing[i])
                {
                    if (johnsonComponentOut.Contains(j)) {
                        johnsonOutgoingSubset[i].Add(j);
                    }
                }
            }
        }
        static void JohnsonWCC(int idx, Dictionary<int, HashSet<int>> outgoing, HashSet<int> component)
        {
            var q = new Queue<int>();
            component.Clear();

            q.Enqueue(idx);
            component.Add(idx);
            while (q.Count > 0)
            {
                int current = q.Dequeue();
                foreach (int next in outgoing[current])
                {
                    if (!component.Contains(next))
                    {
                        q.Enqueue(next);
                        component.Add(next);
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
