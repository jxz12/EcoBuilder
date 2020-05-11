using UnityEngine.Assertions;
using System;
using System.Collections.Generic;

namespace EcoBuilder.NodeLink
{
    public class Johnson
    {
        ///////////////////////////////////
        // loops with Johnson's algorithm

        Dictionary<int, HashSet<int>> johnsonIncoming = new Dictionary<int, HashSet<int>>();
        Dictionary<int, HashSet<int>> johnsonOutgoing = new Dictionary<int, HashSet<int>>();
        public Johnson()
        {

        }
        public void Init(IEnumerable<int> indices, Func<int, IEnumerable<int>> Targets)
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
            foreach (int i in indices)
            {
                foreach (int j in Targets(i))
                {
                    johnsonOutgoing[i].Add(j);
                    johnsonIncoming[j].Add(i);
                }
            }
        }
        public void Clear()
        {
            johnsonLongestLoop.Clear();
            johnsonNumLongest = 0;
        }

        public int MaxLoop {
            get { return johnsonLongestLoop.Count; }
        }
        public IReadOnlyList<int> MaxLoopIndices {
            get { return johnsonLongestLoop; }
        }
        public int NumMaxLoop {
            get { return johnsonNumLongest; }
        }


        // from https://github.com/mission-peace/interview/blob/master/src/com/interview/graph/AllCyclesInDirectedGraphJohnson.java
        // can be very slow, so run async if possible
        List<int> johnsonLongestLoop = new List<int>();
        int johnsonNumLongest = 0;
        public void SolveLoop()
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
        }
        Stack<int> johnsonStack = new Stack<int>();
        HashSet<int> johnsonSet = new HashSet<int>();
        Dictionary<int, HashSet<int>> johnsonMap = new Dictionary<int, HashSet<int>>();
        void JohnsonSingleSource(int source)
        {
            johnsonStack.Clear();
            johnsonSet.Clear();
            johnsonMap.Clear();
            foreach (int i in johnsonOutgoingSubset.Keys) {
                johnsonMap[i] = new HashSet<int>();
            }
            JohnsonDFS(source, source);
        }
        bool JohnsonDFS(int source, int current)
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
        void JohnsonUnblock(int toUnblock)
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
        Dictionary<int, HashSet<int>> johnsonOutgoingSubset = new Dictionary<int, HashSet<int>>();
        HashSet<int> johnsonComponentOut = new HashSet<int>();
        HashSet<int> johnsonComponentIn = new HashSet<int>();
        void JohnsonSCC(int idx)
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
        void JohnsonWCC(int idx, Dictionary<int, HashSet<int>> outgoing, HashSet<int> component)
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
    }
}