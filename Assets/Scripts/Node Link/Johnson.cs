using UnityEngine.Assertions;
using System;
using System.Collections.Generic;

namespace EcoBuilder.NodeLink
{
    public static class Johnson
    {
        ///////////////////////////////////
        // loops with Johnson's algorithm

        static Dictionary<int, HashSet<int>> johnsonIncoming = new Dictionary<int, HashSet<int>>();
        static Dictionary<int, HashSet<int>> johnsonOutgoing = new Dictionary<int, HashSet<int>>();
        public static void InitJohnson(IEnumerable<int> indices, Func<int, IEnumerable<int>> Targets)
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

        public static List<int> LongestLoop {
            get {
                johnsonLongestLoop.Reverse(); // because made from stack
                return new List<int>(johnsonLongestLoop);
            }
        }
        public static int MaxLoop {
            get { return LongestLoop.Count; }
        }
        public static int NumMaxLoop {
            get { return johnsonNumLongest; }
        }



        // from https://github.com/mission-peace/interview/blob/master/src/com/interview/graph/AllCyclesInDirectedGraphJohnson.java
        // can be very slow, so run async if possible
        static List<int> johnsonLongestLoop = new List<int>();
        static int johnsonNumLongest = 0;
        public static void JohnsonsAlgorithm()
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
    }
}