using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace EcoBuilder.NodeLink
{
	public partial class NodeLink :
        IPointerDownHandler, IPointerUpHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
        IScrollHandler
	{
        ////////////////////////////////////
        // for user-interaction rotation

        [SerializeField] float rotationMultiplier=.9f, zoomMultiplier=.05f, panMultiplier=1.2f;
        [SerializeField] float yMinRotationMomentum=.4f, yRotationDrag=.1f;
        [SerializeField] float xDefaultRotation=-15, rotationTween=.2f;
        [SerializeField] float holdThreshold = .2f;

        Node focus=null;
        public void FocusNode(int idx)
        {
            if (focus != nodes[idx])
            {
                focus = nodes[idx];
            }
        }

        void SuperFocus()
        {
            foreach (Link li in links)
            {
                if (li.Source != focus && li.Target != focus)
                {
                    li.SetTransparency(.1f);
                }
                else
                {
                    li.SetTransparency(1f);
                }
            }
            // arrange nodes in a circle and stuff
            var left = new List<Node>();
            var right = new List<Node>();
            foreach (Node no in nodes.Where(x=> x.Idx!=focus.Idx))
            {
                if (links[focus.Idx,no.Idx]!=null || links[no.Idx,focus.Idx]!=null)
                {
                    left.Add(no);
                }
                else
                {
                    right.Add(no);
                }
            }

            focus.FocusPos = (Vector3.up*maxHeight/2);

            float leftAngleHop = Mathf.PI / (left.Count);
            float rightAngleHop = Mathf.PI / (right.Count);
            float angleHop = Mathf.Min(leftAngleHop, rightAngleHop);

            // left
            float angle = Mathf.PI + ((left.Count-1)/2f * angleHop);
            // foreach (Node no in left.OrderBy(x=>trophicLevels[x.Idx]))
            foreach (Node no in left.OrderBy(x=>(links[focus.Idx,x.Idx]!=null?links[focus.Idx,x.Idx].TileSpeed:0)
                                                + (links[focus.Idx,x.Idx]!=null?links[focus.Idx,x.Idx].TileSpeed:0)))
            {
                no.FocusPos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 1)
                               * (maxHeight/2) + (Vector3.up*maxHeight/2);
                angle -= angleHop;
            }
            // right
            angle = -(right.Count-1)/2f * angleHop;
            foreach (Node no in right.OrderBy(x=>trophicLevels[x.Idx]))
            {
                no.FocusPos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 1)
                               * (maxHeight/2) + (Vector3.up*maxHeight/2);
                angle += angleHop;
            }
        }
        public void Unfocus()
        {
            if (focus != null)
            {
                foreach (Link li in links)
                {
                    li.SetTransparency(1f);
                }

                focus = null;
            }
            // StartCoroutine(ResetPan(Vector2.zero, 1f));
            StartCoroutine(ResetZoom(Vector3.one, 1f));
        }
        bool frozen = false;
        public void Freeze()
        {
            focus = null;
            // StartCoroutine(ResetPan(Vector2.zero, 1f));
            StartCoroutine(ResetZoom(Vector3.one, 1f));

            GetComponent<Animator>().SetTrigger("Freeze");
            frozen = true;
        }
        public void MoveLeft()
        {}
        public void MoveMiddle()
        {}


        [SerializeField] MeshRenderer disk;
        bool doLayout = true;
        // TODO: make this nicer and smoother
        void PauseLayout(bool paused)
        {
            if (doLayout != paused) // if already paused
                return;

            Color c = disk.material.color;
            if (paused)
            {
                c *= 1.5f;
            }
            else
            {
                c /= 1.5f;
            }
            disk.material.color = c;
            doLayout = !paused;
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

            graphParent.localScale *= 1 + zoom;
        }
        void Pan(Vector2 amount)
        {
            Vector2 toPan = amount * panMultiplier;
            GetComponent<RectTransform>().anchoredPosition += toPan;
        }

        // IEnumerator ResetPan(Vector2 goalPan, float duration)
        // {
        //     var rt = GetComponent<RectTransform>();
        //     Vector2 startPan = rt.anchoredPosition;
        //     float startTime = Time.time;
        //     while (Time.time < startTime + duration)
        //     {
        //         rt.anchoredPosition = Vector3.Lerp(startPan, goalPan, (Time.time-startTime)/duration);
        //         // rt.anchoredPosition = Vector3.Lerp(rt.anchoredPosition, goalPan, .02f);
        //         yield return null;
        //     }
        //     rt.anchoredPosition = goalPan;
        // }
        IEnumerator ResetZoom(Vector3 endZoom, float duration)
        {
            Vector3 startZoom = graphParent.localScale;
            float startTime = Time.time;
            while (Time.time < startTime + duration)
            {
                // graphParent.localScale = Vector3.Lerp(startZoom, goalZoom, (Time.time-startTime)/duration);
                float t = (Time.time-startTime) / duration * 2;
                if (t < 1) 
                    graphParent.localScale = startZoom + (endZoom-startZoom)/2f*t*t;
                else
                    graphParent.localScale = startZoom - (endZoom-startZoom)/2f*((t-1)*(t-3)-1);

                yield return null;
            }
            graphParent.localScale = endZoom;
        }

        void Rotate(Vector2 amount)
        {
            float ySpin = -amount.x * rotationMultiplier;
            yRotationMomentum = ySpin;
            yMinRotationMomentum = Mathf.Abs(yMinRotationMomentum) * Mathf.Sign(ySpin);

            if (focus == null)
            {
                yRotation += ySpin;
                nodesParent.transform.localRotation = Quaternion.Euler(0,yRotation,0);
            }
            else
            {
                nodesParent.transform.Rotate(Vector3.up, ySpin);
            }

            float xSpin = amount.y * rotationMultiplier;
            graphParent.Rotate(Vector3.right, xSpin);
        }

        /////////////////////////////
        // eventsystems

        bool potentialHold = false;
        Node pressedNode;
        public void OnPointerDown(PointerEventData ped)
        {
            // if (ped.pointerId==-1 || (Input.touchCount==1 && ped.pointerId==0))
            if (ped.pointerId==-1 || ped.pointerId==0)
            {
                PauseLayout(true);
                potentialHold = true;

                pressedNode = ClosestSnappedNode(ped);
                if (pressedNode != null)
                {
                    pressedNode.Outline(0);
                    tooltip.Enable(true);
                    tooltip.SetPos(Camera.main.WorldToScreenPoint(pressedNode.transform.position));
                    if (pressedNode.Removable)
                    {
                        StartCoroutine(WaitThenDelete(holdThreshold, ped));
                        tooltip.ShowTrash();
                    }
                    else
                    {
                        tooltip.ShowNoTrash();
                    }
                }
            }
        }
        IEnumerator WaitThenDelete(float seconds, PointerEventData ped)
        {
            pressedNode.Shake(true);
            float endTime = Time.time + seconds;
            while (Time.time < endTime)
            {
                if (potentialHold == false)
                {
                    yield break;
                }
                else
                {
                    yield return null;
                }
            }
            potentialHold = false;
            RemoveNode(pressedNode.Idx);
            pressedNode = null;
            tooltip.Enable(false);
        }
        public void OnPointerUp(PointerEventData ped)
        {
            // if (ped.pointerId==-1 || (Input.touchCount==1 && ped.pointerId==0))
            if (ped.pointerId==-1 || ped.pointerId==0)
            {
                PauseLayout(false);

                if (potentialHold) // if in the time window for a click
                {
                    potentialHold = false;
                    if (pressedNode != null)
                    {
                        pressedNode.Shake(false);
                        FocusNode(pressedNode.Idx);
                        OnNodeFocused.Invoke(pressedNode.Idx);

                        pressedNode.Unoutline();
                        pressedNode = null;
                        tooltip.Enable(false);
                    }
                    else if (!frozen)
                    {
                        Unfocus();
                        OnEmptyPressed.Invoke();
                    }
                }
                else if (!ped.dragging && pressedNode != null) // not dragging but not deleted either
                {
                    FocusNode(pressedNode.Idx);
                    OnNodeFocused.Invoke(pressedNode.Idx);

                    pressedNode.Unoutline();
                    pressedNode = null;
                    tooltip.Enable(false);
                }
            }
        }

        Node dummySource;
        Link dummyLink;
        Node potentialSource;
        Link potentialLink;
        [SerializeField] UI.Tooltip tooltip;
        public void OnBeginDrag(PointerEventData ped)
        {
            // if (ped.pointerId==-1 || (Input.touchCount==1 && ped.pointerId==0))
            if (ped.pointerId==-1 || ped.pointerId==0)
            {
                potentialHold = false;
                if (pressedNode != null)
                {
                    pressedNode.Shake(false);
                    tooltip.Enable(true);

                    if (pressedNode.CanBeTarget)
                    {
                        dummySource = Instantiate(nodePrefab, nodesParent);
                        dummySource.transform.localScale = Vector3.zero;
                        dummyLink = Instantiate(linkPrefab, linksParent);
                        dummyLink.Target = pressedNode;
                        dummyLink.Source = dummySource;

                        potentialLink = dummyLink;
                        potentialLink.Outline(0);
                    }
                    else
                    {
                        tooltip.ShowNoLink();
                    }
                     
                }
                else
                {
                    PauseLayout(false);
                    tooltip.Enable(false);
                }
            }
        }
        public void OnDrag(PointerEventData ped)
        {
            // if (ped.pointerId==-1 || Input.touchCount==1 && ped.pointerId==0)
            if ((ped.pointerId==-1 || ped.pointerId==0)
                && pressedNode!=null && pressedNode.CanBeTarget)
            {
                Node snappedNode = ClosestSnappedNode(ped);
                if (snappedNode!=null && snappedNode!=pressedNode
                    && snappedNode.CanBeSource
                    && (links[snappedNode.Idx, pressedNode.Idx]==null
                        || links[snappedNode.Idx, pressedNode.Idx].Removable))
                {
                    tooltip.SetPos(Camera.main.WorldToScreenPoint(snappedNode.transform.position));
                    if (potentialSource != snappedNode)
                    {
                        if (potentialSource != null)
                        {
                            potentialSource.Unoutline();
                        }
                        potentialSource = snappedNode;
                    }

                    Link snappedLink = links[snappedNode.Idx, pressedNode.Idx];
                    if (snappedLink == null)
                    {
                        dummyLink.Source = snappedNode;
                        if (potentialLink != dummyLink)
                        {
                            potentialLink.Unoutline();
                            potentialLink = dummyLink;
                        }
                        potentialSource.Outline(1);
                        potentialLink.Outline(1);

                        tooltip.ShowAddLink();
                    }
                    else
                    {
                        if (potentialLink != snappedLink)
                        {
                            potentialLink.Unoutline();
                            potentialLink = dummyLink;
                        }
                        dummyLink.Source = pressedNode; // hide dummyLink
                        potentialSource.Outline(2);
                        potentialLink = links[snappedNode.Idx, pressedNode.Idx];
                        potentialLink.Outline(2);

                        tooltip.ShowUnlink();
                    }
                }
                else
                {
                    if (potentialSource != null)
                    {
                        potentialSource.Unoutline();
                        potentialSource = null;
                    }
                    if (potentialLink != dummyLink)
                    {
                        potentialLink.Unoutline();
                        potentialLink = dummyLink;
                    }
                    potentialLink.Outline(0);


                    var tipPos = Camera.main.WorldToScreenPoint(dummySource.transform.position);
                    tooltip.SetPos(tipPos);
                    Vector3 screenPoint = ped.position;
                    screenPoint.z = pressedNode.transform.position.z - Camera.main.transform.position.z;
                    dummySource.transform.position = Camera.main.ScreenToWorldPoint(screenPoint);
                    if (snappedNode!=null && snappedNode!=pressedNode)
                    {
                        dummyLink.Source = pressedNode; // hide dummyLink
                        if (links[snappedNode.Idx, pressedNode.Idx] == null)
                        {
                            tooltip.ShowNoAddLink();
                        }
                        else
                        {
                            tooltip.ShowNoUnlink();
                        }
                    }
                    else
                    {
                        dummyLink.Source = dummySource;
                        tooltip.ShowLink();
                    }
                }
            }
            if (pressedNode == null)
            {
                // Rotate the whole graph accordingly
                if (ped.pointerId == -1)
                    Rotate(ped.delta);
                else if (Input.touchCount == 1)
                    Rotate(ped.delta * .5f); // TODO: magic number
            }
            if (Input.touchCount == 2) // if pinch/pan
            {
                Touch t1 = Input.touches[0];
                Touch t2 = Input.touches[1];

                float dist = (t1.position - t2.position).magnitude;
                float prevDist = ((t1.position-t1.deltaPosition) - (t2.position-t2.deltaPosition)).magnitude;
                Zoom(.05f * (dist - prevDist)); // TODO: magic number
                // Pan(.5f * (t1.deltaPosition + t2.deltaPosition) / 2);
            }
            if (ped.pointerId == -3) // or middle click
            {
                // Pan(ped.delta);
            }
        }
        public void OnEndDrag(PointerEventData ped)
        {
            if (ped.pointerId==-1 || ped.pointerId==0)
            {
                if (pressedNode != null)
                {
                    pressedNode.Unoutline();
                    pressedNode = null;

                    if (potentialLink != null)
                    {
                        potentialLink.Unoutline();
                        potentialLink = null;
                    }
                    if (potentialSource != null)
                    {
                        potentialSource.Unoutline();
                        potentialSource = null;
                    }
                    if (dummyLink != null)
                    {
                        Destroy(dummyLink.gameObject);
                    }
                    if (dummySource != null)
                    {
                        Destroy(dummySource.gameObject);
                    }

                    tooltip.Enable(false);
                }
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
                if (potentialSource != null && pressedNode != null)
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
            if (frozen)
                return null;

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