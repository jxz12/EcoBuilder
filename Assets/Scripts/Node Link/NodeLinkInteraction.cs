using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

namespace EcoBuilder.NodeLink
{
	public partial class NodeLink :
        IPointerDownHandler, IPointerUpHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
	{
        ////////////////////////////////////
        // for user-interaction rotation

        [SerializeField] float rotationMultiplier=.9f, zoomMultiplier=.005f;
        [SerializeField] float yMinRotation=.4f, yRotationDrag=.1f;
        [SerializeField] float xDefaultRotation=-15, xRotationTween=.2f;
        [SerializeField] float holdThreshold = .3f;

        // TODO: might want to change this into colliders
        [SerializeField] float clickRadius=15;
        private Node ClosestNodeToPointer(Vector2 pointerPos)
        {
            Node closest = null;
            float closestDist = float.MaxValue;
            float radius = clickRadius * clickRadius;
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
            // TODO: change this to actual object that raised the event
            Node held = ClosestNodeToPointer(ped.position);
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
                // else
                // {
                //     FocusNode(held.Idx);
                // }
            }
        }
        public void OnPointerUp(PointerEventData ped)
        {
            if (potentialHold) // if click
            {
                potentialHold = false;
                Node clicked = ClosestNodeToPointer(ped.position);
                if (clicked != null)
                {
                    FocusNode(clicked.Idx);
                }
                else
                {
                    Unfocus();
                }
            }
            dragging = false;
        }

        Node focus=null;
        public void FocusNode(int idx)
        {
            focus = nodes[idx];
            OnNodeFocused.Invoke(idx);
        }
        public void Unfocus()
        {
            focus = null;
            OnUnfocused.Invoke();
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
            dragging = false;
        }
        public void OnDrag(PointerEventData ped)
        {
            if (ped.button == PointerEventData.InputButton.Left)
            {
                float ySpin = -ped.delta.x * rotationMultiplier;
                nodesParent.Rotate(Vector3.up, ySpin);
                yRotationMomentum = ySpin;
                yMinRotation = Mathf.Abs(yMinRotation) * Mathf.Sign(yRotationMomentum);

                float xSpin = ped.delta.y * rotationMultiplier;
                graphParent.Rotate(Vector3.right, xSpin);
            }
            // TODO: make this two fingers instead
            else if (ped.button == PointerEventData.InputButton.Right)
            {
                float zoom = ped.delta.y * zoomMultiplier;
                if (zoom > .5f)
                    zoom = .5f;
                if (zoom < -.5f)
                    zoom = -.5f;

                nodesParent.localScale *= 1 + zoom;
                // graphParent.localScale *= 1 + zoom;
            }
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
        public void MoveRight()
        {
            GetComponent<Animator>().SetTrigger("Right");
        }
		public void Finish()
		{
			GetComponent<Animator>().SetTrigger("Finish");
		}
	}
}