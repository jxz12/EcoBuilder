using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace EcoBuilder.NodeLink
{
    public partial class Graph
    { 
        [SerializeField] float focusHeight;
        [SerializeField] float layoutSmoothTime;

        Vector3 nodesVelocity, graphVelocity;
        void TweenNodesToStress()
        {
            if (FocusedState != FocusState.SuperFocus)
            {
                // // get bounds of layout and center to middle
                // Vector2 minPos = new Vector2(float.MaxValue, float.MaxValue);
                // Vector2 maxPos = new Vector2(float.MinValue, float.MinValue);
                // foreach (Node no in nodes)
                // {
                //     minPos.x = Mathf.Min(no.StressPos.x, minPos.x);
                //     minPos.y = Mathf.Min(no.StressPos.y, minPos.y);
                //     maxPos.x = Mathf.Max(no.StressPos.x, maxPos.x);
                //     maxPos.y = Mathf.Max(no.StressPos.y, maxPos.y);
                // }
                // Vector2 centroid = (minPos + maxPos) / 2;

                // if (ConstrainTrophic)
                // {
                //     centroid.y = 0;
                //     foreach (Node no in nodes) {
                //         no.StressPos -= centroid;
                //     }
                // }
                // else
                // {
                //     float minY = float.MaxValue;
                //     foreach (Node no in nodes) {
                //         minY = Mathf.Min(no.StressPos.y, minY);
                //     }
                //     centroid.y = minY;
                //     foreach (Node no in nodes) {
                //         no.StressPos -= centroid;
                //     }
                // }

                yAxle.localPosition = Vector3.SmoothDamp(yAxle.localPosition, Vector3.zero, ref nodesVelocity, layoutSmoothTime);
                xAxle.localPosition = Vector3.SmoothDamp(xAxle.localPosition, xAxleUnfocused, ref graphVelocity, layoutSmoothTime);
            }
            else // superfocused
            {
                yAxle.localPosition =
                    Vector3.SmoothDamp(yAxle.localPosition, -focusedNode.FocusPos,
                                       ref nodesVelocity, layoutSmoothTime);
                xAxle.localPosition =
                    Vector3.SmoothDamp(xAxle.localPosition, Vector3.up*focusHeight,
                                    ref graphVelocity, layoutSmoothTime);
            }
            
            foreach (Node no in nodes)
            {
                no.TweenPos(layoutSmoothTime);
            }
        }

        [SerializeField] float zoomSmoothTime;
        float graphScale=1, graphScaleTarget=1, graphScaleVelocity=0;
        Vector3 panVelocity;
        void TweenZoomToFit()
        {
            if (FocusedState != FocusState.SuperFocus)
            {
                // adjust zoom target and pan when unfocused
                float maxError = float.MinValue;
                foreach (Node no in nodes)
                {
                    // make sure that all nodes fit on screen
                    // var viewportPos = mainCam.WorldToViewportPoint(no.transform.localPosition * graphScaleTarget) - new Vector3(.5f,.5f);
                    var viewportPos = mainCam.WorldToViewportPoint(no.StressPos * graphScaleTarget) - new Vector3(.5f,.5f);
                    maxError = Mathf.Max(maxError, Mathf.Abs(viewportPos.x) - .375f);
                    maxError = Mathf.Max(maxError, Mathf.Abs(viewportPos.y) - .3f);
                }
                graphScaleTarget -= maxError*.02f;
                graphScaleTarget = Mathf.Min(graphScaleTarget, 1); // don't scale too much
                graphScale = Mathf.SmoothDamp(graphScale, graphScaleTarget, ref graphScaleVelocity, zoomSmoothTime);

                // reset pan
                transform.localPosition = Vector3.SmoothDamp(transform.localPosition, Vector3.zero, ref panVelocity, layoutSmoothTime);
            }
            else // superfocus
            {
                graphScale = Mathf.SmoothDamp(graphScale, 1, ref graphScaleVelocity, zoomSmoothTime);
            }
            xAxle.localScale = graphScale * Vector3.one;
        }

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
            yAxle.transform.localRotation = Quaternion.Euler(0,yRotation,0);
            xAxle.transform.localRotation = Quaternion.Euler(xRotation,0,0);
        }

        /////////////////////////////////////////////
        // fine tune with majorization every frame

        private Queue<int> todoBFS = new Queue<int>();
        private void FineTuneLayout()
        {
            if (nodes.Count==0 || FocusedState==FocusState.SuperFocus) {
                return;
            }
            int idx = todoBFS.Dequeue(); // only do one vertex at a time
            if (ConstrainTrophic)
            {
                trophicSolver.IterateTrophic((i,y)=> nodes[i].StressPos.y = y);
                LocalMajorizationHorizontal(idx);
            }
            else
            {
                LocalMajorization(idx);
            }
            todoBFS.Enqueue(idx);
        }
        private void LocalMajorization(int i)
        {
            Vector2 X_i = nodes[i].StressPos;
            float topSumX = 0, topSumY = 0, botSum=0;
            foreach (var d_j in ShortestPathsBFS(i))
            {
                int j = d_j.Key;
                float d_ij = d_j.Value;
                Vector2 X_j = nodes[j].StressPos;
                float mag = (X_i - X_j).magnitude;
                float w_ij = 1f / (d_ij * d_ij);

                topSumX += w_ij * (X_j.x + (d_ij*(X_i.x - X_j.x))/mag);
                topSumY += w_ij * (X_j.y + (d_ij*(X_i.y - X_j.y))/mag);
                botSum += w_ij;
            }
            if (botSum != 0)
            {
                nodes[i].StressPos = new Vector2(topSumX/botSum, topSumY/botSum);
            }
        }
        private void LocalMajorizationHorizontal(int i)
        {
            float topSumX = 0, /*topSumY = 0,*/ botSum=0;
            Vector2 X_i = nodes[i].StressPos;
            foreach (var d_j in ShortestPathsBFS(i))
            {
                int j = d_j.Key;
                float d_ij = d_j.Value;

                Vector2 X_j = nodes[j].StressPos;
                float mag = (X_i - X_j).magnitude;
                float w_ij = 1f / (d_ij * d_ij);

                topSumX += w_ij * (X_j.x + (d_ij*(X_i.x - X_j.x))/mag);
                // topSumY += w_ij * (X_j.y + (d_ij*(X_i.y - X_j.y))/mag);
                botSum += w_ij;
            }
            if (botSum != 0)
            {
                // nodes[i].StressPos = new Vector2(topSumX/botSum, topSumY/botSum);
                nodes[i].StressPos.x = topSumX / botSum;
            }
        }
        private static Dictionary<int, int> visitedBFS = new Dictionary<int, int>();
        private IEnumerable<KeyValuePair<int, int>> ShortestPathsBFS(int source)
        {
            visitedBFS.Clear();

            visitedBFS[source] = 0;
            var q = new Queue<int>();
            q.Enqueue(source);

            while (q.Count > 0)
            {
                int current = q.Dequeue();
                foreach (int next in undirected[current])
                {
                    if (!visitedBFS.ContainsKey(next))
                    {
                        q.Enqueue(next);
                        visitedBFS[next] = visitedBFS[current] + 1;
                    }
                }
            }
            visitedBFS.Remove(source);
            return visitedBFS;
        }

    }
}