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
    }
}
