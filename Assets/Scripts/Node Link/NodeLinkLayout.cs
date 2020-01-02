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

                    nodesParent.localPosition =
                        Vector3.SmoothDamp(nodesParent.localPosition, -Vector3.up*centroid.y, ref nodesVelocity, layoutSmoothTime);
                    graphParent.localPosition =
                        Vector3.SmoothDamp(graphParent.localPosition, Vector3.up*focusHeight, ref graphVelocity, layoutSmoothTime);
                }
                else // if (focusState != FocusState.Focus)
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

                if (ConstrainTrophic)
                {
                    centroid.y = 0;
                    foreach (Node no in nodes)
                    {
                        no.StressPos -= centroid;
                    }
                }
                else
                {
                    float minY = float.MaxValue;
                    foreach (Node no in nodes)
                        minY = Mathf.Min(no.StressPos.y, minY);

                    centroid.y = minY;
                    foreach (Node no in nodes)
                    {
                        no.StressPos -= centroid;
                    }
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
            // TODO: measure the width and height of the layout and scale according to that

            foreach (Node no in nodes)
            {
                no.TweenPos(layoutSmoothTime);
                no.GetComponent<HealthBar>().TweenHealth(.1f);
            }
        }
        float xDefaultRotation;
        float yRotation=0, yTargetRotation=0, yRotationVelocity=-Mathf.Epsilon; // spin other way initially
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
        void FixRotation(ref float rotation)
        {
            while (rotation < -180)
                rotation += 360;
            while (rotation > 180)
                rotation -= 360;
        }


        /////////////////////////////////
        // for stress-based layout

        private Queue<int> todoBFS = new Queue<int>();

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
                nodes[i].StressPos = new Vector3(topSumX/botSum, topSumY/botSum, topSumZ/botSum);
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
                nodes[i].StressPos = new Vector3(topSumX/botSum, nodes[i].StressPos.y, topSumZ/botSum);
            }
        }
        private void PushToFront(int i)
        {
            float zMin = 0;
            foreach (var node in nodes)
            {
                zMin = Mathf.Min(zMin, node.StressPos.z);
            }
            if (nodes[i].StressPos.z < zMin)
            {
                nodes[i].StressPos = new Vector3(nodes[i].StressPos.z, nodes[i].StressPos.y, zMin);
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
            if (maxTrophic-1 > MaxChain)
            {
                trophicScaling = MaxChain / maxTrophic;
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