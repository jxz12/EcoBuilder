using System.Collections.Generic;
using SparseMatrix;
using UnityEngine;

namespace EcoBuilder.NodeLink
{
    public partial class NodeLink
    { 
        [SerializeField] float maxHeight=3f;
        [SerializeField] float layoutSmoothTime=.5f;//, sizeTween=.05f;

        Vector3 nodesVelocity, graphVelocity;
        Vector3 rotationCenter;
        void TweenNodes()
        {
            if (focusState != FocusState.SuperFocus && focusState != FocusState.SuperAntifocus)
            {
                // get average of all positions, and center
                Vector3 centroid;

                if (focusState == FocusState.Unfocus)
                {
                    centroid = Vector3.zero;
                    foreach (Node no in nodes)
                    {
                        Vector3 pos = no.StressPos;
                        centroid += pos;
                    }
                    centroid /= nodes.Count;

                    nodesParent.localPosition =
                        Vector3.SmoothDamp(nodesParent.localPosition, Vector3.zero, ref nodesVelocity, layoutSmoothTime);
                    graphParent.localPosition =
                        Vector3.SmoothDamp(graphParent.localPosition, graphParentUnfocused, ref graphVelocity, layoutSmoothTime);
                }
                else // if (focusState == FocusState.Focus)
                {
                    centroid = focusedNode.StressPos;

                    nodesParent.localPosition =
                        Vector3.SmoothDamp(nodesParent.localPosition, -Vector3.up*centroid.y, ref nodesVelocity, layoutSmoothTime);
                    graphParent.localPosition =
                        Vector3.SmoothDamp(graphParent.localPosition, Vector3.up*maxHeight/2, ref graphVelocity, layoutSmoothTime);
                }

                if (constrainTrophic)
                {
                    // centroid = Vector3.zero;
                    // float height = Mathf.Min(MaxChain, maxHeight);
                    float height = MaxChain;
                    float trophicScaling = MaxTrophic>1? height / (MaxTrophic-1) : 1;
                    foreach (Node no in nodes)
                    {
                        centroid.y = no.StressPos.y - (trophicScaling * (trophicLevels[no.Idx]-1));
                        no.StressPos -= centroid;
                    }
                    rotationCenter = Vector3.up * height/2;
                }
                else
                {
                    float sqradius = 0;
                    foreach (Node no in nodes)
                        sqradius = Mathf.Max(sqradius, (no.StressPos - centroid).sqrMagnitude);

                    float radius = Mathf.Sqrt(sqradius);
                    rotationCenter = Vector3.up * radius;
                    centroid.y -= radius;
                    foreach (Node no in nodes)
                        no.StressPos -= centroid;

                    // TODO: make the nodes bigger here
                }
            }
            else // superfocused
            {
                nodesParent.localPosition =
                    Vector3.SmoothDamp(nodesParent.localPosition, -focusedNode.FocusPos,
                                       ref nodesVelocity, layoutSmoothTime);
                graphParent.localPosition =
                    Vector3.SmoothDamp(graphParent.localPosition, Vector3.up*maxHeight/2,
                                    ref graphVelocity, layoutSmoothTime);
                
                rotationCenter = Vector3.up * maxHeight/2;
            }

            foreach (Node no in nodes)
            {
                no.TweenPos(layoutSmoothTime);
                no.GetComponent<HealthBar>().TweenHealth(.1f);
            }
        }


        /////////////////////////////////
        // for stress-based layout

        private Queue<int> toBFS = new Queue<int>();

        // SGD
        private void LayoutSGD(int i, Dictionary<int, int> d_j, float eta)
        {
            foreach (int j in FYShuffle(nodes.Indices))
            {
                if (i != j)
                {
                    Vector3 X_ij = nodes[i].StressPos - nodes[j].StressPos;
                    float mag = X_ij.magnitude;

                    if (d_j.ContainsKey(j)) // if there is a path between the two
                    {
                        int d_ij = d_j[j];
                        float mu = Mathf.Min(eta * (1f/(d_ij*d_ij)), 1); // w = 1/d^2

                        Vector3 r = ((mag-d_ij)/2) * (X_ij/mag);
                        nodes[i].StressPos -= mu * r;
                        nodes[j].StressPos += mu * r;
                    }
                    else if (mag < 1) // otherwise push away if too close (jakobsen)
                    {
                        Vector3 r = ((mag-1)/2) * (X_ij/mag);
                        nodes[i].StressPos -= r;
                        nodes[j].StressPos += r;
                    }
                }
            }
        }
        private void LayoutSGDHorizontal(int i, Dictionary<int, int> d_j, float eta)
        {
            foreach (int j in FYShuffle(nodes.Indices))
            {
                if (i != j)
                {
                    Vector3 X_ij = nodes[i].StressPos - nodes[j].StressPos;
                    float mag = X_ij.magnitude;

                    if (d_j.ContainsKey(j)) // if there is a path between the two
                    {
                        int d_ij = d_j[j];
                        float mu = Mathf.Min(eta * (1f/(d_ij*d_ij)), 1); // w = 1/d^2

                        Vector3 r = ((mag-d_ij)/2) * (X_ij/mag);
                        r.y = 0; // constrain y position
                        nodes[i].StressPos -= mu * r;
                        nodes[j].StressPos += mu * r;
                    }
                    else if (mag < 1) // otherwise push away if too close (jakobsen)
                    {
                        Vector3 r = ((mag-1)/2) * (X_ij/mag);
                        r.y = 0; // constrain y position
                        nodes[i].StressPos -= r;
                        nodes[j].StressPos += r;
                    }
                }
            }
        }

        private Dictionary<int, int> ShortestPathsBFS(int source)
        {
            var visited = new Dictionary<int, int>(); // ugly, but reuse here to ease GC

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

        public static IEnumerable<int> FYShuffle(IEnumerable<int> toShuffle)
        {
            var shuffled = new List<int>();
            foreach (int i in toShuffle)
                shuffled.Add(i);
            
            int n = shuffled.Count;
            for (int i=0; i<n-1; i++)
            {
                int j = UnityEngine.Random.Range(i, n);
                // i = j
                yield return shuffled[j];
                // then j = i
                shuffled[j] = shuffled[i];
            }
            yield return shuffled[n-1];
        }


        ////////////////////////////////////
        // for trophic level calculation

        [SerializeField] bool constrainTrophic;
        private SparseVector<float> trophicA = new SparseVector<float>(); // we can assume all matrix values are equal, so only need a vector
        private SparseVector<float> trophicLevels = new SparseVector<float>();

        // update the system of linear equations (Laplacian)
        // return set of roots to the tree
        private HashSet<int> BuildTrophicEquations()
        {
            foreach (int i in nodes.Indices)
                trophicA[i] = 0;

            foreach (var ij in links.IndexPairs)
            {
                int res=ij.Item1, con=ij.Item2;
                trophicA[con] += 1f;
            }

            var basal = new HashSet<int>();
            foreach (int i in nodes.Indices)
            {
                if (trophicA[i] != 0)
                    trophicA[i] = -1f / trophicA[i]; // ensures diagonal dominance
                else
                    basal.Add(i);
            }
            return basal;
        }

        // simplified gauss-seidel iteration because of the simplicity of the laplacian
        void TrophicGaussSeidel()
        {
            SparseVector<float> temp = new SparseVector<float>();
            foreach (var ij in links.IndexPairs)
            {
                int res = ij.Item1, con = ij.Item2;
                temp[con] += trophicA[con] * trophicLevels[res];
            }
            MaxTrophic = 0;
            foreach (int i in nodes.Indices)
            {
                trophicLevels[i] = (1 - temp[i]);
                MaxTrophic = Mathf.Max(trophicLevels[i], MaxTrophic);
            }
        }

        public static void Majorization(Vector3[] X, int[,] d)
        {
            int n = X.Length;
            for (int i=0; i<n; i++) {

                float topSumX = 0, topSumY = 0, topSumZ = 0, botSum=0;
                for (int j=0; j<n; j++) {
                    if (i!=j) {
                        int d_ij = d[i,j];
                        float w_ij = 1f / (d_ij * d_ij);
                        float magnitude = (X[i] - X[j]).magnitude;

                        topSumX += w_ij * (X[j].x + (d_ij*(X[i].x - X[j].x))/magnitude);
                        topSumY += w_ij * (X[j].y + (d_ij*(X[i].y - X[j].y))/magnitude);
                        topSumZ += w_ij * (X[j].z + (d_ij*(X[i].z - X[j].z))/magnitude);
                        botSum += w_ij;
                    }
                }

                float newX = topSumX / botSum;
                float newY = topSumY / botSum;
                float newZ = topSumZ / botSum;

                X[i] = new Vector3(newX, newY, newZ);
            }

        }
    }
}