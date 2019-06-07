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
            if (focus != null)
                Destroy(focus.gameObject.GetComponent<cakeslice.Outline>());

            GetComponent<Animator>().SetInteger("Middle Left Focus", 2);
            focus = nodes[idx];
            focus.gameObject.AddComponent<cakeslice.Outline>();
        }
        public void Unfocus()
        {
            if (focus != null)
            {
                Destroy(focus.gameObject.GetComponent<cakeslice.Outline>());
                focus = null;
            }
            GetComponent<Animator>().SetInteger("Middle Left Focus", 0);
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
        float yRotationMomentum = 0;
        private void Rotate()
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
            potentialHold = true;
            doLayout = false;
            StartCoroutine(WaitForHold(holdThreshold, ped));
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
                print("TODO: delete held");
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
            }
            print("TODO: deal with clicking more than one mouse button at a time");
            if (Input.touchCount < 2)
                doLayout = true;
        }

        // Node dummyNode;
        // Link dummyLink;
        public void OnBeginDrag(PointerEventData ped)
        {
            potentialHold = false;
            if (ped.pointerId==0 || ped.pointerId==-1) // only drag on left-click or one touch
            {
                Node draggedNode = ped.rawPointerPress.GetComponent<Node>();
                if (draggedNode != null)
                {
                    // dummyNode = Instantiate(nodePrefab)
                    print("TODO: instantiate a temporary link here to show the user that they are making one");
                }
            }
        }
        public void OnDrag(PointerEventData ped)
        {
            // if single touch or left-click
            if (ped.pointerId==-1 || (ped.pointerId>=0 && Input.touchCount < 2))
            {
                Node draggedNode = ped.rawPointerPress.GetComponent<Node>();
                if (draggedNode != null)
                {
                    GameObject hoveredObject = ped.pointerCurrentRaycast.gameObject;
                    Node hoveredNode = hoveredObject==null? null : hoveredObject.GetComponent<Node>();
                    if (hoveredNode != null)
                    {
                        print("TODO: snap to hovered node");
                    }
                }
                else
                {
                    // spin the whole graph
                    float ySpin = -ped.delta.x * rotationMultiplier;
                    nodesParent.Rotate(Vector3.up, ySpin);

                    yRotationMomentum = ySpin;
                    yMinRotation = Mathf.Abs(yMinRotation) * Mathf.Sign(yRotationMomentum);

                    float xSpin = ped.delta.y * rotationMultiplier;
                    graphParent.Rotate(Vector3.right, xSpin);
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
            print("TODO: deal with clicking more than one mouse button at a time");
            if (Input.touchCount < 2)
                doLayout = true;
        }
        public void OnScroll(PointerEventData ped)
        {
            Zoom(ped.scrollDelta.y);
            print("TODO: use scrollDelta.x here for rotation too");
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
                    Node draggedNode = ped.rawPointerPress.GetComponent<Node>();
                    GameObject droppedOnObject = ped.pointerCurrentRaycast.gameObject;
                    Node droppedOnNode = droppedOnObject==null? null : droppedOnObject.GetComponent<Node>();
                    if (droppedOnNode != null)
                    {
                        if (draggedNode != null && droppedOnNode != null)
                        {
                            // add a new link
                            int i=draggedNode.Idx, j=droppedOnNode.Idx;
                            if (i != j && !draggedNode.IsSinkOnly && !droppedOnNode.IsSourceOnly)
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
        }
	}
}