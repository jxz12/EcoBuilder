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
                // if (focus != null)
                // {
                //     focus.Unoutline();
                // }
                focus = nodes[idx];
                // focus.Outline();
                GetComponent<Animator>().SetInteger("Middle Left Focus", 2);
            }
        }
        public void Unfocus()
        {
            if (focus != null)
            {
                // focus.Unoutline();
                focus = null;
                GetComponent<Animator>().SetInteger("Middle Left Focus", 0);
            }
        }
        public void MoveLeft()
        {
            GetComponent<Animator>().SetInteger("Middle Left Focus", 1);
        }
        public void MoveMiddle()
        {
            GetComponent<Animator>().SetInteger("Middle Left Focus", 0);
        }
		public void Finish()
		{
			GetComponent<Animator>().SetTrigger("Finish");
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

            graphParent.localScale *= 1 + zoom;
        }
        void Pan(Vector2 amount)
        {
            Vector3 toPan = (Vector3)amount * panMultiplier;
            graphParent.localPosition += toPan;
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

            var graphParentGoal = Quaternion.Euler(xDefaultRotation, 0, 0);
            var lerped = Quaternion.Slerp(graphParent.transform.localRotation, graphParentGoal, xRotationTween);
            graphParent.transform.localRotation = lerped;
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
                potentialHold = true;
                StartCoroutine(WaitForHold(holdThreshold, ped));
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
            
            // add links if possible on hold
            Node held = ped.rawPointerPress.GetComponent<Node>();
            if (held != null)
            {
                print("TODO: super focus mode");
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
                    print("TODO: zoom to centroid");
                    Unfocus();
                    OnUnfocused.Invoke();
                }

                // release rotation
                if (ped.pointerId==-1 || (Input.touchCount==1 && ped.pointerId==0))
                {
                    doLayout = true;
                }
            }
        }

        Node dummyTarget;
        Link dummyLink;
        Node potentialSource, potentialTarget;
        public void OnBeginDrag(PointerEventData ped)
        {
            potentialHold = false;
            if (ped.pointerId==0 || ped.pointerId==-1) // only drag on left-click or one touch
            {
                Node draggedNode = ped.rawPointerPress.GetComponent<Node>();
                if (draggedNode != null && !draggedNode.IsTargetOnly)
                {
                    potentialSource = draggedNode;
                    draggedNode.Outline();

                    dummyTarget = Instantiate(nodePrefab, nodesParent);
                    dummyTarget.transform.localScale = Vector3.zero;
                    dummyLink = Instantiate(linkPrefab, linksParent);
                    dummyLink.Source = draggedNode;
                    dummyLink.Target = dummyTarget;
                }
            }
        }
        public void OnDrag(PointerEventData ped)
        {
            // if single touch or left-click
            if (ped.pointerId==-1 || (ped.pointerId>=0 && Input.touchCount<2))
            {
                if (potentialSource != null)
                {
                    GameObject hoveredObject = ped.pointerCurrentRaycast.gameObject;
                    Node snappedNode = hoveredObject==null? null : hoveredObject.GetComponent<Node>();
                    if (snappedNode == null)
                        snappedNode = ClosestSnappedNode(ped.position);

                    // if nothing to snap to
                    if (snappedNode == null || snappedNode==potentialSource || snappedNode.IsSourceOnly)
                    {
                        if (potentialTarget != null)
                        {
                            potentialTarget.Unoutline();
                            potentialTarget = null;
                            dummyLink.Target = dummyTarget;
                        }
                        Vector3 screenPoint = ped.position;
                        screenPoint.z = potentialSource.transform.position.z - Camera.main.transform.position.z;
                        dummyTarget.transform.position = Camera.main.ScreenToWorldPoint(screenPoint);
                    }
                    else
                    {
                        // if not already snapped to
                        if (snappedNode != potentialTarget)
                        {
                            if (potentialTarget != null)
                                potentialTarget.Unoutline();

                            dummyLink.Target = snappedNode;
                            potentialTarget = snappedNode;
                            potentialTarget.Outline();
                        }
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
            if (potentialTarget != null)
            {
                potentialTarget.Unoutline();
                potentialTarget = null;
            }
            if (potentialSource != null)
            {
                potentialSource.Unoutline();
                potentialSource = null;
                Destroy(dummyLink.gameObject);
                Destroy(dummyTarget.gameObject);
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
                else
                {
                    if (potentialSource != null && potentialTarget != null)
                    {
                        // add a new link
                        int i=potentialSource.Idx, j=potentialTarget.Idx;
                        if (i != j && !potentialSource.IsTargetOnly && !potentialTarget.IsSourceOnly)
                        {
                            if (links[i,j] != null)
                            {
                                RemoveLink(i, j);
                                OnLinkRemoved.Invoke(i,j);
                            }
                            else
                            {
                                AddLink(i, j);
                                OnLinkAdded.Invoke(i, j);
                            }
                        }
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