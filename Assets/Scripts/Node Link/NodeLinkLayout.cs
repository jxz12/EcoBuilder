using System;
using System.Linq;
using System.Collections.Generic;
using SparseMatrix;
using UnityEngine;

namespace EcoBuilder.NodeLink
{
    public partial class NodeLink
    { 
        [SerializeField] float focusHeight;
        [SerializeField] float layoutSmoothTime;

        Vector3 nodesVelocity, graphVelocity;
        void TweenNodes()
        {
            if (!tweenNodes)
                return;

            if (focusState != FocusState.SuperFocus)
            {
                // get average of all positions, and center
                Vector3 centroid;
                if (focusState == FocusState.Focus)
                {
                    centroid = focusedNode.StressPos;

                    nodesParent.localPosition = Vector3.SmoothDamp(nodesParent.localPosition, -Vector3.up*centroid.y, ref nodesVelocity, layoutSmoothTime);
                    graphParent.localPosition = Vector3.SmoothDamp(graphParent.localPosition, Vector3.up*focusHeight, ref graphVelocity, layoutSmoothTime);
                }
                else // if not focus
                {
                    centroid = Vector3.zero;
                    foreach (Node no in nodes)
                    {
                        Vector3 pos = no.StressPos;
                        centroid += pos;
                    }
                    centroid /= nodes.Count;

                    nodesParent.localPosition = Vector3.SmoothDamp(nodesParent.localPosition, Vector3.zero, ref nodesVelocity, layoutSmoothTime);
                    graphParent.localPosition = Vector3.SmoothDamp(graphParent.localPosition, graphParentUnfocused, ref graphVelocity, layoutSmoothTime);
                }

                if (ConstrainTrophic)
                {
                    centroid.y = 0;
                    foreach (Node no in nodes)
                        no.StressPos -= centroid;
                }
                else
                {
                    float minY = float.MaxValue;
                    foreach (Node no in nodes)
                        minY = Mathf.Min(no.StressPos.y, minY);

                    centroid.y = minY;
                    foreach (Node no in nodes)
                        no.StressPos -= centroid;
                }
            }
            else // superfocused
            {
                nodesParent.localPosition =
                    Vector3.SmoothDamp(nodesParent.localPosition, -focusedNode.FocusPos,
                                       ref nodesVelocity, layoutSmoothTime);
                graphParent.localPosition =
                    Vector3.SmoothDamp(graphParent.localPosition, Vector3.up*focusHeight,
                                    ref graphVelocity, layoutSmoothTime);
            }
            
            foreach (Node no in nodes)
            {
                no.TweenPos(layoutSmoothTime);
                no.GetComponent<HealthBar>().TweenHealth(1f * Time.deltaTime);
            }
        }

        void TweenZoom()
        {
            if (focusState == FocusState.Unfocus || focusState == FocusState.Frozen)
            {
                // adjust zoom target and pan when unfocused
                float maxError = float.MinValue;
                foreach (Node no in nodes)
                {
                    // make sure that all nodes fit on screen
                    var viewportPos = Camera.main.WorldToViewportPoint(no.transform.localPosition * graphScaleTarget) - new Vector3(.5f,.5f);
                    maxError = Mathf.Max(maxError, Mathf.Abs(viewportPos.x) - .4f);
                    maxError = Mathf.Max(maxError, Mathf.Abs(viewportPos.y) - .4f);
                }
                graphScaleTarget -= maxError*.1f; // TODO: magic number
                graphScaleTarget = Mathf.Min(graphScaleTarget, 1); // don't scale too much
                graphScale = Mathf.SmoothDamp(graphScale, graphScaleTarget, ref graphScaleVelocity, zoomSmoothTime);

                // reset pan
                transform.localPosition = Vector3.SmoothDamp(transform.localPosition, Vector3.zero, ref panVelocity, layoutSmoothTime);
            }
            else if (focusState == FocusState.Focus)
            {
                // graphScale = Mathf.SmoothDamp(graphScale, 1.2f, ref graphScaleVelocity, zoomSmoothTime);
            }
            else if (focusState == FocusState.SuperFocus)
            {
                graphScale = Mathf.SmoothDamp(graphScale, 1, ref graphScaleVelocity, zoomSmoothTime);
            }
            graphParent.localScale = graphScale * Vector3.one;
            // TODO: magic numbers
        }
        [SerializeField] float zoomSmoothTime;
        float graphScale=1, graphScaleTarget=1, graphScaleVelocity=0;
        Vector3 panVelocity;


        float xDefaultRotation;
        float yRotation=0, yTargetRotation=0, yRotationVelocity=0;
        float xRotation=0, xTargetRotation=0, xRotationVelocity=0;
        private void MomentumRotate()
        {
            if (dragging)
            {
                yRotation = Mathf.SmoothDamp(yRotation, yTargetRotation, ref yRotationVelocity, .05f);
                xRotation = Mathf.SmoothDamp(xRotation, xTargetRotation, ref xRotationVelocity, .05f);
            }
            else
            {
                yTargetRotation = 0;
                yRotation = Mathf.SmoothDamp(yRotation, yTargetRotation, ref yRotationVelocity, .5f);
                xTargetRotation = xDefaultRotation;
                xRotation = Mathf.SmoothDamp(xRotation, xTargetRotation, ref xRotationVelocity, .5f);
            }
            nodesParent.transform.localRotation = Quaternion.Euler(0,yRotation,0);
            graphParent.transform.localRotation = Quaternion.Euler(xRotation,0,0);
        }

        /////////////////////////////
        // for stress-based layout //
        /////////////////////////////
        private Queue<int> todoBFS = new Queue<int>();

        private void LayoutNextQueuedNode()
        {
            if (nodes.Count == 0 || focusState == FocusState.SuperFocus)
                return;

            int i = todoBFS.Dequeue(); // only do one vertex at a time
            var d_j = ShortestPathsBFS(i);

            if (ConstrainTrophic && !LaplacianDetZero)
            {
                TrophicGaussSeidel();
                LayoutMajorizationHorizontal(i, d_j);
            }
            else
            {
                LayoutMajorization(i, d_j);
            }
            todoBFS.Enqueue(i);
        }
        [SerializeField] float centeringMultiplier;
        private void LayoutMajorization(int i, Dictionary<int, int> d_j)
        {
            Vector3 X_i = nodes[i].StressPos;
            float topSumX = 0, topSumY = 0, topSumZ = 0, botSum=0;
            foreach (int j in nodes.Indices)
            {
                if (i == j)
                    continue;
                
                Vector3 X_j = nodes[j].StressPos;
                float mag = (X_i - X_j).magnitude;
                if (d_j.ContainsKey(j))
                {
                    float d_ij = d_j[j];
                    float w_ij = 1f / (d_ij * d_ij);

                    topSumX += w_ij * (X_j.x + (d_ij*(X_i.x - X_j.x))/mag);
                    topSumY += w_ij * (X_j.y + (d_ij*(X_i.y - X_j.y))/mag);
                    topSumZ += w_ij * (X_j.z + (d_ij*(X_i.z - X_j.z))/mag);
                    botSum += w_ij;
                }
                else if (mag < 1) // apply dwyer separation constraint
                {
                    Vector3 r = ((mag-1)/2) * ((X_i-X_j)/mag);
                    nodes[i].StressPos -= r;
                    nodes[j].StressPos += r;
                }
            }
            if (botSum > 0)
            {
                nodes[i].StressPos = new Vector3(topSumX/botSum, topSumY/botSum, topSumZ/botSum) * centeringMultiplier;
            }
        }
        private void LayoutMajorizationHorizontal(int i, Dictionary<int, int> d_j)
        {
            float topSumX = 0, /*topSumY = 0,*/ topSumZ = 0, botSum=0;
            Vector3 X_i = nodes[i].StressPos;
            foreach (int j in nodes.Indices)
            {
                if (i == j)
                    continue;
                
                Vector3 X_j = nodes[j].StressPos;
                float mag = (X_i - X_j).magnitude;
                if (d_j.ContainsKey(j))
                {
                    float d_ij = d_j[j];
                    float w_ij = 1f / (d_ij * d_ij);

                    topSumX += w_ij * (X_j.x + (d_ij*(X_i.x - X_j.x))/mag);
                    // topSumY += w_ij * (X_j.y + (d_ij*(X_i.y - X_j.y))/mag);
                    topSumZ += w_ij * (X_j.z + (d_ij*(X_i.z - X_j.z))/mag);
                    botSum += w_ij;
                }
                else if (mag < 1) // apply dwyer separation constraint
                {
                    Vector3 r = ((mag-1)/2) * ((X_i-X_j)/mag);
                    r.y = 0; // constrain y position
                    nodes[i].StressPos -= r;
                    nodes[j].StressPos += r;
                }
            }
            if (botSum > 0)
            {
                nodes[i].StressPos = new Vector3(topSumX/botSum * centeringMultiplier, nodes[i].StressPos.y, topSumZ/botSum * centeringMultiplier);
            }
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
        public static void FYShuffle<T>(List<T> deck, int seed=0)
        {
            var rand = new System.Random(seed);
            int n = deck.Count;
            for (int i=0; i<n-1; i++)
            {
                int j = rand.Next(i, n);
                T temp = deck[j];
                deck[j] = deck[i];
                deck[i] = temp;
            }
        }

        ////////////////////////////////////
        // for trophic level calculation

        public bool ConstrainTrophic { get; set; }
        private SparseVector<float> trophicA = new SparseVector<float>(); // we can assume all matrix values are equal, so only need a vector here
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

        // optimised gauss-seidel iteration because of the simplicity of the laplacian
        void TrophicGaussSeidel()
        {
            SparseVector<float> temp = new SparseVector<float>();
            foreach (var ij in links.IndexPairs)
            {
                int res = ij.Item1, con = ij.Item2;
                temp[con] += trophicA[con] * trophicLevels[res];
            }
            float maxTrophic = 0;
            foreach (int i in nodes.Indices)
            {
                trophicLevels[i] = (1 - temp[i]);
                maxTrophic = Mathf.Max(trophicLevels[i], maxTrophic);
            }
            float trophicScaling = 1;
            if (maxTrophic > MaxChain+1)
            {
                trophicScaling = (MaxChain+1) / maxTrophic;
            }
            foreach (Node no in nodes)
            {
                Vector3 newPos = no.StressPos;
                newPos.y = Mathf.Lerp(newPos.y, trophicScaling * (trophicLevels[no.Idx]-1), .1f);
                no.StressPos = newPos;
            }
        }
    }
}