using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

// for heavy calculations
using System.Threading.Tasks;

namespace EcoBuilder.NodeLink
{
	public partial class NodeLink
	{
        // TODO: option to highlight tallest species or longest loop on selection
        public bool Disjoint { get; private set; } = false;
        public int NumEdges { get; private set; } = 0;
        public bool LaplacianDetZero { get; private set; } = false;

        private List<int> TallestNodes { get; set; }
        public int MaxChain { get; private set; }
        private List<int> LongestLoop { get; set; }
        public int MaxLoop { get { return LongestLoop==null? 0 : LongestLoop.Count; } }

        public bool IsCalculating { get; private set; } = false;

        public async void ConstraintsAsync()
        {
            IsCalculating = true;

            RefreshTrophicAndFindChain();
            await Task.Run(()=> LayoutSGD());

            var inout = JohnsonInOut(); // not async to ensure synchronize state
            LongestLoop = await Task.Run(()=> JohnsonsAlgorithm(inout.Item1, inout.Item2));

            IsCalculating = false;
            OnConstraints.Invoke();
        }
        public void ConstraintsSync()
        {
            RefreshTrophicAndFindChain();
            LayoutSGD();

            var inout = JohnsonInOut();
            LongestLoop = JohnsonsAlgorithm(inout.Item1, inout.Item2);

            OnConstraints.Invoke();
        }

        ////////////////////////////
        // refresh layout with SGD

        private void LayoutSGD()
        {
            var terms = new List<Tuple<int,int,int>>();
            int d_max = 0;
            foreach (int i in nodes.Indices)
            {
                var d_j = ShortestPathsBFS(i);
                foreach (var d in d_j.Where(foo=> foo.Key!=i))
                {
                    terms.Add(Tuple.Create(i, d.Key, d.Value));
                    d_max = Math.Max(d.Value, d_max);
                }
            }
            foreach (float eta in ExpoSchedule(d_max))
            {
                FYShuffle(terms);
                foreach (var term in terms)
                {
                    int i = term.Item1, j = term.Item2, d_ij = term.Item3;
                    Vector3 X_ij = nodes[i].StressPos - nodes[j].StressPos;

                    float mag = X_ij.magnitude;
                    float mu = Mathf.Min(eta * (1f/(d_ij*d_ij)), 1); // w = 1/d^2
                    Vector3 r = ((mag-d_ij)/2) * (X_ij/mag);
                    nodes[i].StressPos -= mu * r;
                    nodes[j].StressPos += mu * r;
                }
            }
        }
        private IEnumerable<float> ExpoSchedule(int d_max)
        {
            yield return d_max*d_max;
            // float eta_max = d_max*d_max;
            // float lambda = Mathf.Log(eta_max) / 9;
            // for (int t=0; t<10; t++)
            // {
            //     yield return eta_max * Mathf.Exp(-lambda * t);
            // }
        }

        private Dictionary<int, int> ShortestPathsBFS(int source)
        {
            var visited = new Dictionary<int, int>();

            visited[source] = 0;
            var q = new Queue<int>();
            q.Enqueue(source);

            while (q.Count > 0)
            {
                int current = q.Dequeue();
                foreach (int next in adjacency[current])
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
        public static void FYShuffle<T>(List<T> deck)
        {
            var rand = new System.Random();
            int n = deck.Count;
            for (int i=0; i<n-1; i++)
            {
                int j = rand.Next(i, n);
                T temp = deck[j];
                deck[j] = deck[i];
                deck[i] = temp;
            }
        }

        ///////////////////////////////////////////////////////
        // for disjoint, chain length, invalidness

        bool CheckDisjoint()
        {
            if (nodes.Count == 0)
                return false;

            // pick a random vertex
            int source = nodes.Indices.First();
            var q = new Queue<int>();
            var visited = new HashSet<int>();

            q.Enqueue(source);
            visited.Add(source);
            while (q.Count > 0)
            {
                int current = q.Dequeue();
                foreach (int next in adjacency[current])
                {
                    if (!visited.Contains(next))
                    {
                        q.Enqueue(next);
                        visited.Add(next);
                    }
                }
            }
            if (visited.Count != nodes.Count)
                return true;
            else 
                return false;
        }

        private void RefreshTrophicAndFindChain()
        {
            Disjoint = CheckDisjoint();
            NumEdges = links.Count();

            HashSet<int> basal = BuildTrophicEquations();
            var heights = HeightBFS(basal);
            LaplacianDetZero = (heights.Count != nodes.Count);

            // if (focusState == FocusState.SuperFocus) // reorder in case trophic order changes
            //     SuperFocus(focusedNode.Idx);

            foreach (int height in heights.Values)
                MaxChain = Math.Max(height, MaxChain);

            TallestNodes = new List<int>();
            foreach (int idx in heights.Keys)
                if (heights[idx] == MaxChain)
                    TallestNodes.Add(idx);
        }

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


        ////////////////////////
        // for basal/apex

        HashSet<int> GetSources()
        {
            var sources = new HashSet<int>();
            foreach (Node no in nodes)
                if (links.GetColumnDataCount(no.Idx) == 0) // slow, but WHATEVER
                    sources.Add(no.Idx);
            
            return sources;
        }
        HashSet<int> GetSinks()
        {
            var sinks = new HashSet<int>();
            foreach (Node no in nodes)
                if (links.GetRowDataCount(no.Idx) == 0)
                    sinks.Add(no.Idx);
            
            return sinks;
        }


        ///////////////////////////////////
        // loops with Johnson's algorithm

        // ignores 'graveyard' species
        Tuple<Dictionary<int, HashSet<int>>, Dictionary<int, HashSet<int>>> JohnsonInOut()
        {
            var incomingCopy = new Dictionary<int, HashSet<int>>();
            var outgoingCopy = new Dictionary<int, HashSet<int>>();
            foreach (int i in nodes.Indices)
            {
                incomingCopy[i] = new HashSet<int>();
                outgoingCopy[i] = new HashSet<int>();
            }
            foreach (var ij in links.IndexPairs)
            {
                int i=ij.Item1, j=ij.Item2;
                incomingCopy[j].Add(i);
                outgoingCopy[i].Add(j);
            }
            return Tuple.Create(incomingCopy, outgoingCopy);
        }

        // very slow, so run async if possible
        List<int> JohnsonsAlgorithm(Dictionary<int, HashSet<int>> incoming, Dictionary<int, HashSet<int>> outgoing)
        {
            int longestPathLength = 0;
            List<int> longestPath = null;
            foreach (Node no in nodes)
            {
                int idx = no.Idx;
                // if the strongly connected component is bigger than just the vertex
                var scc = StronglyConnectedComponent(idx, outgoing, incoming);

                List<int> johnsonPath = JohnsonSingleSource(idx, outgoing);
                if (johnsonPath.Count > longestPathLength)
                {
                    longestPathLength = johnsonPath.Count;
                    longestPath = johnsonPath;
                }

                // remove node from graph
                outgoing.Remove(idx);
                incoming.Remove(idx);
                foreach (var set in outgoing.Values)
                    set.Remove(idx);
                foreach (var set in incoming.Values)
                    set.Remove(idx);
            }
			return longestPath;
        }
        Stack<int> johnsonStack = new Stack<int>();
        HashSet<int> johnsonSet = new HashSet<int>();
        Dictionary<int, HashSet<int>> johnsonMap = new Dictionary<int, HashSet<int>>();
		List<int> johnsonLongestPath;
        List<int> JohnsonSingleSource(int source, Dictionary<int, HashSet<int>> outgoing)
        {
            johnsonStack.Clear();
            johnsonSet.Clear();
            johnsonMap.Clear();
			foreach (int i in outgoing.Keys)
				johnsonMap[i] = new HashSet<int>();

			johnsonLongestPath = new List<int>(); // do not use clear as we want to not overwrite best
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
