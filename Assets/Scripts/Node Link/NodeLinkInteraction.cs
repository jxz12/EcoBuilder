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

        [SerializeField] float rotationMultiplier=.9f, zoomMultiplier=.005f;
        [SerializeField] float yMinRotation=.4f, yRotationDrag=.1f;
        [SerializeField] float xDefaultRotation=-15, xRotationTween=.2f;
        [SerializeField] float holdThreshold = .2f;

        bool potentialHold = false;
        bool dragging = false;
        public void OnPointerDown(PointerEventData ped)
        {
            potentialHold = true;
            dragging = true;
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
                if (focus != null && focus != held)
                {
                    int i=held.Idx, j=focus.Idx;
                    if (i != j && !held.IsSinkOnly && !focus.IsSourceOnly)
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
        public void OnPointerUp(PointerEventData ped)
        {
            if (potentialHold) // if click
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
            if (Input.touchCount < 2)
                dragging = false;
        }

        Node focus=null;
        public void FocusNode(int idx)
        {
            if (focus != null)
                Destroy(focus.gameObject.GetComponent<cakeslice.Outline>());
            // else
            //     GetComponent<Animator>().SetTrigger("Focus");

            focus = nodes[idx];
            focus.gameObject.AddComponent<cakeslice.Outline>();
        }
        public void Unfocus()
        {
            if (focus != null)
            {
                Destroy(focus.gameObject.GetComponent<cakeslice.Outline>());
                // GetComponent<Animator>().SetTrigger("Middle");
                focus = null;
            }
        }
        public void FlashNode(int idx)
        {
            nodes[idx].Flash();
        }
        public void IdleNode(int idx)
        {
            nodes[idx].Idle();
        }

        float yRotationMomentum = 0;
        public void OnBeginDrag(PointerEventData ped)
        {
            potentialHold = false;
        }
        public void OnEndDrag(PointerEventData ped)
        {
            yRotationMomentum = -ped.delta.x * rotationMultiplier;
            if (Input.touchCount < 2)
                dragging = false;
        }
        public void OnDrag(PointerEventData ped)
        {
            if (Input.touchCount < 2) // a bit hacky to not use ped, but isn't all of Unity?
            {
                float ySpin = -ped.delta.x * rotationMultiplier;
                nodesParent.Rotate(Vector3.up, ySpin);
                yRotationMomentum = ySpin;
                yMinRotation = Mathf.Abs(yMinRotation) * Mathf.Sign(yRotationMomentum);

                float xSpin = ped.delta.y * rotationMultiplier;
                graphParent.Rotate(Vector3.right, xSpin);
            }
            else if (Input.touchCount == 2)
            {
                Touch t1 = Input.touches[0];
                Touch t2 = Input.touches[1];

                float dist = (t1.position - t2.position).magnitude;
                float prevDist = ((t1.position-t1.deltaPosition) - (t2.position-t2.deltaPosition)).magnitude;
                Zoom(dist - prevDist);
            }
        }
        public void OnScroll(PointerEventData ped)
        {
            Zoom(ped.scrollDelta.y);
        }
        void Zoom(float amount)
        {
            float zoom = amount * zoomMultiplier;
            zoom = Mathf.Min(zoom, .5f);
            zoom = Mathf.Max(zoom, -.5f);

            graphParent.localScale *= 1 + zoom;
        }
        public void OnDrop(PointerEventData ped)
        {
            if (ped.pointerDrag != this.gameObject)
                OnDroppedOn.Invoke();
        }
        private void Rotate()
        {
            yRotationMomentum += (yMinRotation - yRotationMomentum) * yRotationDrag;
            nodesParent.Rotate(Vector3.up, yRotationMomentum);

            var graphParentGoal = Quaternion.Euler(xDefaultRotation, 0, 0);
            var lerped = Quaternion.Lerp(graphParent.transform.localRotation, graphParentGoal, xRotationTween);
            graphParent.transform.localRotation = lerped;
        }

        public void MoveLeft()
        {
            GetComponent<Animator>().SetTrigger("Left");
        }
        public void MoveMiddle()
        {
            GetComponent<Animator>().SetTrigger("Middle");
        }
		public void Finish()
		{
			GetComponent<Animator>().SetTrigger("Finish");
		}
	}
}