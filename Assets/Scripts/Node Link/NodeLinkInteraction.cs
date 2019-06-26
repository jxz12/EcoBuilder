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
            GetComponent<Animator>().SetTrigger("Freeze");
            frozen = true;
        }


        public void FlashNode(int idx)
        {
            nodes[idx].Flash();
        }
        public void IdleNode(int idx)
        {
            nodes[idx].Idle();
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
        bool doLayout = true;
        public void OnPointerDown(PointerEventData ped)
        {
            if (ped.pointerId==-1 || (Input.touchCount==1 && ped.pointerId==0))
            {
                doLayout = false;
                if (!frozen)
                {
                    potentialHold = true;
                    StartCoroutine(WaitForHold(holdThreshold, ped));
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
            
            Node held = ped.rawPointerPress.GetComponent<Node>();
            if (held != null)
            {
                print("TODO: super focus mode (and on right click)");
                if (held.Removable)
                {
                    RemoveNode(held.Idx);
                }
            }
        }
        public void OnPointerUp(PointerEventData ped)
        {
            if (potentialHold) // if clickable
            {
                potentialHold = false;
                Node clicked = ped.rawPointerPress.GetComponent<Node>();
                if (clicked != null)
                {
                    FocusNode(clicked.Idx);
                    OnNodeFocused.Invoke(clicked.Idx);
                }
                else
                {
                    Unfocus();
                    OnUnfocused.Invoke();
                }
            }

            // release rotation
            if (ped.pointerId==-1 || (Input.touchCount==1 && ped.pointerId==0))
            {
                doLayout = true;
            }
        }

        Node dummySource;
        Link dummyLink;
        Node potentialSource, potentialTarget;
        [SerializeField] UI.Tooltip tooltip;
        public void OnBeginDrag(PointerEventData ped)
        {
            if (potentialHold == false) // already held and maybe deleted
                return;

            potentialHold = false;
            if (ped.pointerId==0 || ped.pointerId==-1) // only drag on left-click or one touch
            {
                Node draggedNode = ped.rawPointerPress.GetComponent<Node>();
                if (draggedNode != null && !frozen)
                {
                    potentialTarget = draggedNode;
                    // potentialTarget.Outline();

                    dummySource = Instantiate(nodePrefab, nodesParent);
                    dummySource.transform.localScale = Vector3.zero;
                    dummyLink = Instantiate(linkPrefab, linksParent);
                    dummyLink.Target = draggedNode;
                    dummyLink.Source = dummySource;

                    tooltip.Enable(true);
                }
                else
                {
                    doLayout = true;
                }
            }
        }
        public void OnDrag(PointerEventData ped)
        {
            // if single touch or left-click
            if (ped.pointerId==-1 || (ped.pointerId>=0 && Input.touchCount<2))
            {
                if (potentialTarget != null)
                {
                    if (potentialTarget.CanBeTarget)
                        tooltip.ShowLink();
                    else
                        tooltip.ShowNoLink();
                    

                    GameObject hoveredObject = ped.pointerCurrentRaycast.gameObject;
                    Node snappedNode = null;
                    if (hoveredObject != null)
                        snappedNode = hoveredObject.GetComponent<Node>();
                    if (snappedNode == null)
                        snappedNode = ClosestSnappedNode(ped.position);

                    if (snappedNode!=null && snappedNode!=potentialTarget && potentialTarget.CanBeTarget)
                    {
                        int i = snappedNode.Idx;
                        int j = potentialTarget.Idx;

                        tooltip.SetPos(Camera.main.WorldToScreenPoint(snappedNode.transform.position));
                        if (links[i,j] == null)
                        {
                            if (snappedNode.CanBeSource)
                            {
                                // if (potentialSource != null)
                                //     potentialSource.Unoutline();

                                dummyLink.Source = snappedNode;
                                potentialSource = snappedNode;
                                // potentialSource.Outline();

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
                            // if (potentialSource != null)
                            //     potentialSource.Unoutline();

                            dummyLink.Source = potentialTarget; // hide dummyLink

                            if (links[i,j].Removable)
                            {
                                potentialSource = snappedNode;
                                // potentialSource.Outline(2);

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
                            // potentialSource.Unoutline();
                            potentialSource = null;
                        }
                        Vector3 screenPoint = ped.position;
                        screenPoint.z = potentialTarget.transform.position.z - Camera.main.transform.position.z;

                        dummySource.transform.position = Camera.main.ScreenToWorldPoint(screenPoint);
                        dummyLink.Source = dummySource;

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
            if (potentialSource != null)
            {
                // potentialSource.Unoutline();
                potentialSource = null;
            }
            if (potentialTarget != null)
            {
                // potentialTarget.Unoutline();
                potentialTarget = null;
                Destroy(dummyLink.gameObject);
                Destroy(dummySource.gameObject);

                tooltip.Enable(false);
            }

            // release rotation
            if (
                // (ped.pointerId==-1 || (Input.touchCount==1 && ped.pointerId==0)))
                (ped.pointerId==-1 || ped.pointerId==0))
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
                else if (potentialSource != null && potentialTarget != null)
                {
                    // add/remove a new link
                    int i=potentialSource.Idx, j=potentialTarget.Idx;
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
        private Node ClosestSnappedNode(Vector2 pointerPos)
        {
            Node closest = null;
            float closestDist = float.MaxValue;
            float radius = snapRadius * snapRadius;
            foreach (Node node in nodes)
            {
                Vector2 screenPos = Camera.main.WorldToScreenPoint(node.transform.position);

                // if the click is within the clickable radius
                if ((pointerPos-screenPos).sqrMagnitude < radius)
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