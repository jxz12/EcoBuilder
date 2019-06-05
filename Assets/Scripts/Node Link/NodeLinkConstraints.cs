using System;
using System.Collections.Generic;

namespace EcoBuilder.NodeLink
{
	public partial class NodeLink
	{
        ////////////////////////
        // for basal/apex

        public HashSet<int> GetBasal()
        {
            var basal = new HashSet<int>();
            foreach (Node no in nodes)
                if (links.GetColumnDataCount(no.Idx) == 0) // slow, but WHATEVER
                    basal.Add(no.Idx);
            
            return basal;
        }
        public HashSet<int> GetApex()
        {
            var apex = new HashSet<int>();
            foreach (Node no in nodes)
                if (links.GetRowDataCount(no.Idx) == 0)
                    apex.Add(no.Idx);
            
            return apex;
        }


        //////////////////////////
        // for chain length

        private Dictionary<int, int> HeightBFS(IEnumerable<int> sources)
        {
            var visited = new Dictionary<int, int>();
            var q = new Queue<int>();
            foreach (int source in sources)
            {
                q.Enqueue(source);
                visited[source] = 0;
            }
            while (q.Count > 0)
            {
                int current = q.Dequeue();
                foreach (int next in links.GetColumnIndicesInRow(current))
                {
                    if (!visited.ContainsKey(next))
                    {
                        q.Enqueue(next);
                        visited[next] = visited[current] + 1;
                    }
                }
            }
            return visited;
        }

        public int MaxChainLength()
        {
            HashSet<int> basal = GetBasal();
            var heights = HeightBFS(basal);
            int maxChain = 0;
            foreach (int height in heights.Values)
                maxChain = Math.Max(maxChain, height);

            return maxChain;
        }

        ///////////////////////////////////
        // for loops

        public int LongestLoop()
        {
            var outgoingCopy = new Dictionary<int, HashSet<int>>();
            var incomingCopy = new Dictionary<int, HashSet<int>>();
            foreach (int i in nodes.Indices)
            {
                outgoingCopy[i] = new HashSet<int>();
                incomingCopy[i] = new HashSet<int>();
            }
            foreach (var ij in links.IndexPairs)
            {
                int i=ij.Item1, j=ij.Item2;
                outgoingCopy[i].Add(j);
                incomingCopy[j].Add(i);
            }

			int longestPath = 0;
            foreach (Node no in nodes)
            {
                int idx = no.Idx;
                // if the strongly connected component is bigger than just the vertex
                var scc = StronglyConnectedComponent(idx, outgoingCopy, incomingCopy);

                longestPath = Math.Max(longestPath, JohnsonSingleSource(idx, outgoingCopy).Count);

                // remove node from graph
                outgoingCopy.Remove(idx);
                incomingCopy.Remove(idx);
                foreach (var set in outgoingCopy.Values)
                    set.Remove(idx);
                foreach (var set in incomingCopy.Values)
                    set.Remove(idx);
            }
			return longestPath;
        }
        Stack<int> johnsonStack;
        HashSet<int> johnsonSet;
        Dictionary<int, HashSet<int>> johnsonMap;
		List<int> johnsonLongestPath;
        List<int> JohnsonSingleSource(int source, Dictionary<int, HashSet<int>> outgoing)
        {
            johnsonStack = new Stack<int>();
            johnsonSet = new HashSet<int>();
            johnsonMap = new Dictionary<int, HashSet<int>>();
			johnsonLongestPath = new List<int>();
			foreach (int i in outgoing.Keys)
				johnsonMap[i] = new HashSet<int>();

            JohnsonDFS(source, source, outgoing);
			return johnsonLongestPath;
        }
        bool JohnsonDFS(int source, int current, Dictionary<int, HashSet<int>> outgoing)
        {
            bool foundCycle = false;
            johnsonStack.Push(current);
            johnsonSet.Add(current);

            foreach (int next in outgoing[current])
            {
                if (next == source) // found cycle, so see if it is longest
                {
                    if (johnsonStack.Count > johnsonLongestPath.Count)
						johnsonLongestPath = new List<int>(johnsonStack);

                    foundCycle = true;
                }
                else if (!johnsonSet.Contains(next))
                {
                    // if a cycle is found in any descendants, then no need to search again
                    foundCycle |= JohnsonDFS(source, next, outgoing);
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
                foreach (int next in outgoing[current])
                {
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
			foreach (int toAlsoUnblock in johnsonMap[toUnblock])
			{
				if (johnsonSet.Contains(toAlsoUnblock))
					JohnsonUnblock(toAlsoUnblock);
			}
			johnsonMap[toUnblock].Clear();
        }


        // returns the next strongly connected component with more than one vertex
        Dictionary<int, HashSet<int>> StronglyConnectedComponent(int idx, Dictionary<int, HashSet<int>> outgoing, Dictionary<int, HashSet<int>> incoming)
        {
            var component1 = WeaklyConnectedComponent(idx, outgoing);
            var component2 = WeaklyConnectedComponent(idx, incoming);
            component1.IntersectWith(component2);

            var scc = new Dictionary<int, HashSet<int>>();
            foreach (int i in component1)
            {
                scc[i] = new HashSet<int>();
                foreach (int j in outgoing[i])
                {
                    if (component1.Contains(j))
                    {
                        scc[i].Add(j);
                    }
                }
            }
            return scc;
        }
        HashSet<int> WeaklyConnectedComponent(int idx, Dictionary<int, HashSet<int>> outgoing)
        {
            var q = new Queue<int>();
            var visited = new HashSet<int>();

            q.Enqueue(idx);
            visited.Add(idx);
            while (q.Count > 0)
            {
                int current = q.Dequeue();
                foreach (int next in outgoing[current])
                {
                    if (!visited.Contains(next))
                    {
                        q.Enqueue(next);
                        visited.Add(next);
                    }
                }
            }
            return visited;
        }

        /////////////////////////
        // for trophic coherence

        public float CalculateOmnivory()
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