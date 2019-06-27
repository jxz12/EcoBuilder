using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

namespace EcoBuilder.NodeLink
{
	public partial class NodeLink :
        IPointerDownHandler, IPointerUpHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
        IScrollHandler
	{
        ////////////////////////////////////
        // for user-interaction rotation

        [SerializeField] float rotationMultiplier=.9f, zoomMultiplier=.005f, panMultiplier=1.2f;
        [SerializeField] float yMinRotation=.4f, yRotationDrag=.1f;
        [SerializeField] float xDefaultRotation=-15, xRotationTween=.2f;
        [SerializeField] float holdThreshold = .2f;

        Node focus=null;
        public void FocusNode(int idx)
        {
            if (focus != nodes[idx])
            {
                focus = nodes[idx];
            }
        }
        public void Unfocus()
        {
            if (focus != null)
            {
                focus = null;
            }
            else
            {
                StartCoroutine(ResetPan(Vector2.zero, .5f));
                StartCoroutine(ResetZoom(Vector3.one, .5f));
            }
        }
        bool frozen = false;
        public void Freeze()
        {
            focus = null;
            StartCoroutine(ResetPan(Vector2.zero, .5f));
            StartCoroutine(ResetZoom(Vector3.one, .5f));

            GetComponent<Animator>().SetTrigger("Freeze");
            frozen = true;
        }


        public void FlashNode(int idx)
        {
            nodes[idx].Flash(true);
        }
        public void UnflashNode(int idx)
        {
            nodes[idx].Flash(false);
        }

        void Zoom(float amount)
        {
            float zoom = amount * zoomMultiplier;
            zoom = Mathf.Min(zoom, .5f);
            zoom = Mathf.Max(zoom, -.5f);

            GetComponent<RectTransform>().localScale *= 1 + zoom;
        }
        void Pan(Vector2 amount)
        {
            Vector2 toPan = amount * panMultiplier;
            GetComponent<RectTransform>().anchoredPosition += toPan;
        }

        IEnumerator ResetPan(Vector2 goalPan, float duration)
        {
            var rt = GetComponent<RectTransform>();
            Vector2 startPan = rt.anchoredPosition;
            float startTime = Time.time;
            while (Time.time < startTime + duration)
            {
                rt.anchoredPosition = Vector3.Lerp(startPan, goalPan, (Time.time-startTime)/duration);
                // rt.anchoredPosition = Vector3.Lerp(rt.anchoredPosition, goalPan, .02f);
                yield return null;
            }
            // rt.anchoredPosition = goalPan;
        }
        IEnumerator ResetZoom(Vector3 goalZoom, float duration)
        {
            var rt = GetComponent<RectTransform>();
            Vector3 startZoom = rt.localScale;
            float startTime = Time.time;
            while (Time.time < startTime + duration)
            {
                rt.localScale = Vector3.Lerp(startZoom, goalZoom, (Time.time-startTime)/duration);
                // rt.localScale = Vector3.Lerp(rt.localScale, goalZoom, layoutTween);
                yield return null;
            }
            rt.localScale = goalZoom;
        }

        void Rotate(Vector2 amount)
        {
            float ySpin = -amount.x * rotationMultiplier;
            nodesParent.Rotate(Vector3.up, ySpin);

            yRotationMomentum = ySpin;
            yMinRotation = Mathf.Abs(yMinRotation) * Mathf.Sign(yRotationMomentum);

            float xSpin = amount.y * rotationMultiplier;
            graphParent.Rotate(Vector3.right, xSpin);
        }
        float yRotationMomentum = 0;
        private void RotateMomentum()
        {
            yRotationMomentum += (yMinRotation - yRotationMomentum) * yRotationDrag;
            nodesParent.Rotate(Vector3.up, yRotationMomentum);

            // var graphParentGoal = Quaternion.Euler(xDefaultRotation, 0, 0);
            // var lerped = Quaternion.Slerp(graphParent.transform.localRotation, graphParentGoal, xRotationTween);
            // graphParent.transform.localRotation = lerped;
        }

        /////////////////////////////
        // eventsystems

        bool potentialHold = false;
        Node pressedNode;
        bool doLayout = true;
        public void OnPointerDown(PointerEventData ped)
        {
            // if (ped.pointerId==-1 || (Input.touchCount==1 && ped.pointerId==0))
            if (ped.pointerId==-1 || ped.pointerId==0)
            {
                potentialHold = true;
                doLayout = false;
                StartCoroutine(WaitForHold(holdThreshold, ped));

                if (!frozen)
                {
                    pressedNode = ClosestSnappedNode(ped);
                    if (pressedNode != null)
                    {
                        tooltip.Enable(true);
                        tooltip.SetPos(Camera.main.WorldToScreenPoint(pressedNode.transform.position));
                        if (pressedNode.Removable)
                        {
                            // prepare for deletion
                            pressedNode.Shake(true);
                            tooltip.ShowTrash();
                        }
                        else
                        {
                            tooltip.ShowNoTrash();
                        }
                    }
                }
            }
        }
        IEnumerator WaitForHold(float seconds, PointerEventData ped)
        {
            float endTime = Time.time + seconds;
            while (Time.time < endTime)
            {
                if (potentialHold == false)
                    yield break;
                else
                    yield return null;
            }
            potentialHold = false;
            
            if (!frozen && pressedNode != null && pressedNode.Removable)
            {
                RemoveNode(pressedNode.Idx);
                tooltip.Enable(false);
            }
        }
        public void OnPointerUp(PointerEventData ped)
        {
            // if (ped.pointerId==-1 || (Input.touchCount==1 && ped.pointerId==0))
            if (ped.pointerId==-1 || ped.pointerId==0)
            {
                if (potentialHold) // if in the time window for a click
                {
                    potentialHold = false;

                    if (pressedNode != null)
                    {
                        pressedNode.Shake(false);
                        FocusNode(pressedNode.Idx);
                        OnNodeFocused.Invoke(pressedNode.Idx);
                        tooltip.Enable(false);
                        pressedNode = null;
                    }
                    else
                    {
                        Unfocus();
                        OnUnfocused.Invoke();
                    }
                }
                else if (!ped.dragging && pressedNode != null) // not dragging but not deleted either
                {
                    FocusNode(pressedNode.Idx);
                    OnNodeFocused.Invoke(pressedNode.Idx);
                    tooltip.Enable(false);
                    pressedNode = null;
                }
            }

            // release rotation, only if everything is released
            if (ped.pointerId==-1 || Input.touchCount==1)
            {
                doLayout = true;
            }
        }

        Node dummySource;
        Link dummyLink;
        Node potentialSource;
        [SerializeField] UI.Tooltip tooltip;
        public void OnBeginDrag(PointerEventData ped)
        {
            potentialHold = false;
            if (pressedNode != null)
            {
                pressedNode.Outline();
                pressedNode.Shake(false);

                dummySource = Instantiate(nodePrefab, nodesParent);
                dummySource.transform.localScale = Vector3.zero;
                dummyLink = Instantiate(linkPrefab, linksParent);
                dummyLink.Target = pressedNode;
                dummyLink.Source = dummySource;

                tooltip.Enable(true);
            }
            else
            {
                tooltip.Enable(false);
            }
        }
        Link outlinedLink;
        Node outlinedSource;
        public void OnDrag(PointerEventData ped)
        {
            // if single touch or left-click
            if (ped.pointerId==-1 || (ped.pointerId>=0 && Input.touchCount<2))
            {
                if (pressedNode != null)
                {
                    if (pressedNode.CanBeTarget)
                        tooltip.ShowLink();
                    else
                        tooltip.ShowNoLink();

                    if (outlinedLink != null)
                    {
                        outlinedLink.Unoutline();
                        outlinedLink = null;
                    }
                    if (outlinedSource != null)
                    {
                        outlinedSource.Unoutline();
                        outlinedSource = null;
                    }

                    Node snappedNode = ClosestSnappedNode(ped);

                    if (snappedNode!=null && snappedNode!=pressedNode && pressedNode.CanBeTarget)
                    {
                        int i = snappedNode.Idx;
                        int j = pressedNode.Idx;

                        tooltip.SetPos(Camera.main.WorldToScreenPoint(snappedNode.transform.position));
                        if (links[i,j] == null)
                        {
                            if (snappedNode.CanBeSource)
                            {
                                dummyLink.Outline(1);
                                outlinedLink = dummyLink;
                                snappedNode.Outline(1);
                                outlinedSource = snappedNode;

                                dummyLink.Source = snappedNode;
                                potentialSource = snappedNode;

                                tooltip.ShowAddLink();
                            }
                            else
                            {
                                potentialSource = null;

                                tooltip.ShowNoLink();
                            }
                        }
                        else
                        {
                            dummyLink.Source = pressedNode; // hide dummyLink

                            if (links[i,j].Removable)
                            {
                                links[i,j].Outline(2);
                                outlinedLink = links[i,j];
                                snappedNode.Outline(2);
                                outlinedSource = snappedNode;

                                potentialSource = snappedNode;

                                tooltip.ShowUnLink();
                            }
                            else
                            {
                                potentialSource = null;

                                tooltip.ShowNoLink();
                            }

                        }
                    }
                    else
                    {
                        if (potentialSource != null)
                        {
                            potentialSource = null;
                        }
                        Vector3 screenPoint = ped.position;
                        screenPoint.z = pressedNode.transform.position.z - Camera.main.transform.position.z;

                        dummySource.transform.position = Camera.main.ScreenToWorldPoint(screenPoint);
                        dummyLink.Source = dummySource;

                        dummyLink.Outline();
                        outlinedLink = dummyLink;

                        // var tipPos = .5f * Camera.main.WorldToScreenPoint(potentialTarget.transform.position);
                        // tipPos += .5f * Camera.main.WorldToScreenPoint(dummySource.transform.position);

                        var tipPos = Camera.main.WorldToScreenPoint(dummySource.transform.position);
                        tooltip.SetPos(tipPos);
                    }
                }
                else
                {
                    // Rotate the whole graph accordingly
                    Rotate(ped.delta);
                }
            }
            // if double touch or middle-click
            else if (Input.touchCount == 2)
            {
                Touch t1 = Input.touches[0];
                Touch t2 = Input.touches[1];

                float dist = (t1.position - t2.position).magnitude;
                float prevDist = ((t1.position-t1.deltaPosition) - (t2.position-t2.deltaPosition)).magnitude;
                Zoom(dist - prevDist);
                Pan((t1.deltaPosition + t2.deltaPosition) / 2);
            }
            else if (ped.pointerId == -3)
            {
                Pan(ped.delta);
            }
        }
        public void OnEndDrag(PointerEventData ped)
        {
            if (pressedNode != null)
            {
                pressedNode.Unoutline();
                pressedNode = null;
                outlinedLink.Unoutline();

                if (potentialSource != null)
                {
                    outlinedSource.Unoutline();
                    potentialSource = null;
                }

                Destroy(dummyLink.gameObject);
                Destroy(dummySource.gameObject);
                tooltip.Enable(false);
            }

            // release rotation, only if everything is released
            if (ped.pointerId==-1 || Input.touchCount==1)
            {
                doLayout = true;
            }
        }
        public void OnScroll(PointerEventData ped)
        {
            Zoom(ped.scrollDelta.y);
            Rotate(new Vector3(ped.scrollDelta.x, 0));
        }
        // OnDrop gets called before OnEndDrag
        public void OnDrop(PointerEventData ped)
        {
            if (ped.pointerId==0 || ped.pointerId==-1)
            {
                if (ped.pointerDrag != gameObject) // if the drag comes from another object
                {
                    OnDroppedOn.Invoke();
                }
                else if (potentialSource != null && pressedNode != null)
                {
                    // add/remove a new link
                    int i=potentialSource.Idx, j=pressedNode.Idx;
                    if (links[i,j] != null)
                    {
                        RemoveLink(i, j);
                    }
                    else
                    {
                        AddLink(i, j);
                    }
                }
            }
        }


        //////////////////////////////
        // helpers

        // this returns any node within the snap radius
        // if more than one are in the radius, then return the closest to the camera.
        [SerializeField] float snapRadius=40;
        private Node ClosestSnappedNode(PointerEventData ped)
        {
            GameObject hoveredObject = ped.pointerCurrentRaycast.gameObject;
            Node snappedNode = null;
            if (hoveredObject != null)
                snappedNode = hoveredObject.GetComponent<Node>();

            if (snappedNode != null)
            {
                return snappedNode;
            }
            else
            {
                Node closest = null;
                float closestDist = float.MaxValue;
                float radius = snapRadius * snapRadius;
                foreach (Node node in nodes)
                {
                    Vector2 screenPos = Camera.main.WorldToScreenPoint(node.transform.position);

                    // if the click is within the clickable radius
                    if ((ped.position-screenPos).sqrMagnitude < radius)
                    {
                        // choose the node closer to the screen
                        float dist = (Camera.main.transform.position - node.transform.position).sqrMagnitude;
                        if (dist < closestDist)
                        {
                            closest = node;
                            closestDist = dist;
                        }
                    }
                }
                return closest;
            }
        }

	}
}