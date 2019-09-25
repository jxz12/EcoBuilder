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

        [SerializeField] float rotationMultiplier, zoomMultiplier, panMultiplier;
        [SerializeField] float yMinRotationMomentum, yRotationDrag;
        [SerializeField] float xDefaultRotation, rotationTween;
        [SerializeField] float holdThreshold;

        Node focusedNode = null;
        bool superfocused = false;
        public void FocusNode(int idx)
        {
            if (focusedNode != nodes[idx] && focusedNode != null)
            {
                focusedNode.Unoutline();
            }

            if (!superfocused)
            {
                if (focusedNode == nodes[idx])
                {
                    SuperFocus();
                    superfocused = true;
                }
                else
                {
                    focusedNode = nodes[idx];
                    focusedNode.Outline();
                }
            }
            else
            {
                if (focusedNode == nodes[idx])
                {
                    // go back to previous focus
                    superfocused = false;
                    foreach (Link li in links)
                    {
                        li.SetTransparency(1f);
                    }
                }
                else // already superfocused, but switch
                {
                    focusedNode = nodes[idx];
                    focusedNode.Outline();
                    SuperFocus();
                }
            }
        }
        public void SwitchFocus(int idx) // urgh
        {
            if (focusedNode != nodes[idx] && nodes[idx] != null)
            {
                FocusNode(idx);
                OnNodeFocused.Invoke(idx);
            }
        }
        void Unfocus()
        {
            if (focusedNode != null)
            {
                if (!superfocused)
                {
                    focusedNode.Unoutline();
                    focusedNode = null;
                }
                else
                {
                    superfocused = false;
                    foreach (Link li in links)
                    {
                        li.SetTransparency(1f);
                    }
                }
            }
            // StartCoroutine(ResetPan(Vector2.zero, 1f));
            StartCoroutine(ResetZoom(Vector3.one, 1f));
        }
        public void FullUnfocus()
        {
            if (focusedNode != null)
            {
                focusedNode.Unoutline();
                focusedNode = null;
                if (superfocused)
                {
                    superfocused = false;
                    foreach (Link li in links)
                    {
                        li.SetTransparency(1f);
                    }
                }
            }
        }

        public void SuperFocus()
        {
            var unrelated = new List<Node>();
            var consumers = new List<Node>();
            var resources = new List<Node>();
            var both = new List<Node>();

            foreach (Node no in nodes)
            {
                if (no.Idx == focusedNode.Idx)
                {
                    continue;
                }
                // uninteracting
                if (links[focusedNode.Idx,no.Idx] == null && links[no.Idx,focusedNode.Idx] == null)
                {
                    unrelated.Add(no);
                }
                else if (links[focusedNode.Idx,no.Idx] != null && links[no.Idx,focusedNode.Idx] == null)
                {
                    consumers.Add(no);
                }
                else if (links[focusedNode.Idx,no.Idx] == null && links[no.Idx,focusedNode.Idx] != null)
                {
                    resources.Add(no);
                }
                // mutual consumption
                else if (links[focusedNode.Idx,no.Idx] != null && links[no.Idx,focusedNode.Idx] != null)
                {
                    both.Add(no);
                }
            }
            foreach (Link li in links)
            {
                if (li.Source != focusedNode && li.Target != focusedNode)
                {
                    li.SetTransparency(.05f);
                }
                else
                {
                    li.SetTransparency(1f);
                }
            }


            // both resources and consumers on right
            int right = System.Math.Max(consumers.Count, resources.Count)*2;
            int left = both.Count;

            // arrange in circle around focus in middle
            focusedNode.FocusPos = new Vector3(0, maxHeight/2, 0);
            foreach (Node no in unrelated)
            {
                no.FocusPos = no.StressPos + new Vector3(-4, -4, 25);
            }

            if (right+left == 0)
            {
                return;
            }
            float range = Mathf.PI * ((float)(right) / (right+left));
            float angle = 0;
            foreach (Node no in consumers.OrderBy(x=>-trophicLevels[x.Idx]))
            {
                angle += 1f / (consumers.Count+1) * range;
                no.FocusPos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle),.2f) * (maxHeight/2)
                              + focusedNode.FocusPos;
            }
            angle = 0;
            foreach (Node no in resources.OrderBy(x=>trophicLevels[x.Idx]))
            {
                angle -= 1f / (resources.Count+1) * range;
                no.FocusPos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle),.2f) * (maxHeight/2)
                              + focusedNode.FocusPos;
            }
            angle -= 1f / (resources.Count+1) * range;
            range = 2 * Mathf.PI * ((float)(left) / (right+left));
            foreach (Node no in both.OrderBy(x=>trophicLevels[x.Idx]))
            {
                angle -= 1f / (both.Count+1) * range;
                no.FocusPos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle),.2f) * (maxHeight/2)
                              + focusedNode.FocusPos;
            }
        }
        Vector3 graphParentUnfocused;
        public void MoveHorizontal(float xCoord)
        {
            graphParentUnfocused.x = xCoord;
        }
        bool frozen = false;
        public void Freeze()
        {
            focusedNode = null;
            // StartCoroutine(ResetPan(Vector2.zero, 1f));
            StartCoroutine(ResetZoom(Vector3.one, 1f));

            GetComponent<Animator>().SetTrigger("Freeze");
            frozen = true;
        }


        // [SerializeField] MeshRenderer disk;
        bool doLayout = true;

        void PauseLayout(bool paused)
        {
            if (doLayout != paused) // if already paused
                return;

            // Color c = disk.material.color;
            // if (paused)
            // {
            //     c *= 1.5f;
            // }
            // else
            // {
            //     c /= 1.5f;
            // }
            // disk.material.color = c;
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
            if (Input.touchCount == 2)
            {
                zoom *= zoomMultiplier; // TODO: not good
            }
            zoom = Mathf.Min(zoom, .5f);
            zoom = Mathf.Max(zoom, -.5f);

            graphParent.localScale *= 1 + zoom;
        }
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
        // void Pan(Vector2 amount)
        // {
        //     Vector2 toPan = amount * panMultiplier;
        //     GetComponent<RectTransform>().anchoredPosition += toPan;
        // }
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

        void Rotate(Vector2 amount)
        {
            float ySpin = -amount.x * rotationMultiplier;
            yRotationMomentum = ySpin;
            yMinRotationMomentum = Mathf.Abs(yMinRotationMomentum) * Mathf.Sign(ySpin);

            if (!superfocused)
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

        Node pressedNode;
        public void OnPointerDown(PointerEventData ped)
        {
            // if (ped.pointerId==-1 || (Input.touchCount==1 && ped.pointerId==0))
            if (ped.pointerId==-1 || ped.pointerId==0)
            {
                PauseLayout(true);

                pressedNode = ClosestSnappedNode(ped);
                if (pressedNode != null)
                {
                    pressedNode.Outline(1);
                    tooltip.Enable();
                    tooltip.ShowInspect();
                    tooltip.SetPos(Camera.main.WorldToScreenPoint(pressedNode.transform.position));
                }
            }
        }
        public void OnPointerUp(PointerEventData ped)
        {
            if (ped.pointerId==-1 || ped.pointerId==0)
            {
                PauseLayout(false);

                if (!ped.dragging) // if click
                {
                    if (pressedNode != null)
                    {
                        FocusNode(pressedNode.Idx);
                        OnNodeFocused.Invoke(pressedNode.Idx);

                        if (pressedNode == focusedNode)
                        {
                            pressedNode.Outline(0);
                        }
                        else
                        {
                            pressedNode.Unoutline();
                        }
                        pressedNode = null;
                        tooltip.Disable();
                    }
                    else if (!frozen)
                    {
                        if (focusedNode != null)
                        {
                            if (!superfocused)
                            {
                                OnUnfocused.Invoke();
                            }
                        }
                        else
                        {
                            OnEmptyPressed.Invoke();
                        }
                        Unfocus();
                    }
                }
                // else if (!ped.dragging && pressedNode != null) // not dragging but not deleted either
                // {
                //     FocusNode(pressedNode.Idx);
                //     OnNodeFocused.Invoke(pressedNode.Idx);

                //     if (pressedNode == focusedNode)
                //     {
                //         pressedNode.Outline(0);
                //     }
                //     else
                //     {
                //         pressedNode.Unoutline();
                //     }
                //     pressedNode = null;
                //     tooltip.Disable();
                // }
            }
        }

        [SerializeField] UI.Tooltip tooltip;
        Node dummySource;
        Link dummyLink;
        Node potentialSource;
        Link potentialLink;
        public void OnBeginDrag(PointerEventData ped)
        {
            if (ped.pointerId==-1 || ped.pointerId==0)
            {
                if (pressedNode != null)
                {
                    tooltip.Enable();

                    if (pressedNode.CanBeTarget)
                    {
                        dummySource = Instantiate(nodePrefab, nodesParent);
                        dummySource.transform.localScale = Vector3.zero;
                        dummySource.transform.position = pressedNode.transform.position;
                        dummyLink = Instantiate(linkPrefab, linksParent);
                        dummyLink.Target = pressedNode;
                        dummyLink.Source = dummySource;

                        potentialLink = dummyLink;
                    }
                    else
                    {
                        tooltip.ShowBanned();
                    }
                     
                }
                else
                {
                    PauseLayout(false);
                    tooltip.Disable();
                }
            }
        }
        public void OnDrag(PointerEventData ped)
        {
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
                    if (potentialSource != snappedNode) // if not same as previous
                    {
                        if (potentialSource != null)
                        {
                            if (potentialSource == focusedNode)
                            {
                                potentialSource.Outline(1);
                            }
                            else
                            {
                                potentialSource.Unoutline();
                            }
                        }
                        potentialSource = snappedNode;
                    }

                    Link snappedLink = links[snappedNode.Idx, pressedNode.Idx];
                    if (snappedLink == null) // link can be added
                    {
                        dummyLink.Source = snappedNode;
                        if (potentialLink != dummyLink)
                        {
                            potentialLink.Unoutline();
                            potentialLink = dummyLink;
                            if (potentialSource == focusedNode)
                            {
                                potentialSource.Outline(1);
                            }
                            else
                            {
                                potentialSource.Unoutline();
                            }
                        }
                        pressedNode.Outline(1);
                        potentialSource.Outline(1);
                        potentialLink.Outline(1);

                        tooltip.ShowAddLink();
                    }
                    else // link can be deleted
                    {
                        if (potentialLink != snappedLink)
                        {
                            potentialLink.Unoutline();
                        }
                        pressedNode.Outline(2);
                        dummyLink.Source = pressedNode; // hide dummyLink
                        potentialSource.Outline(2);
                        potentialLink = links[snappedNode.Idx, pressedNode.Idx];
                        potentialLink.Outline(2);

                        tooltip.ShowUnlink();
                    }
                }
                else // no snap
                {
                    if (potentialSource != null)
                    {
                        if (potentialSource == focusedNode)
                        {
                            potentialSource.Outline(0);
                        }
                        else
                        {
                            potentialSource.Unoutline();
                        }
                        potentialSource = null;
                    }
                    if (potentialLink != dummyLink)
                    {
                        potentialLink.Unoutline();
                        potentialLink = dummyLink;
                    }
                    pressedNode.Outline(1);
                    potentialLink.Outline(1);


                    Vector3 screenPoint = ped.position;
                    screenPoint.z = pressedNode.transform.position.z - Camera.main.transform.position.z;
                    dummySource.transform.position = Camera.main.ScreenToWorldPoint(screenPoint);

                    tooltip.SetPos(screenPoint);
                    if (snappedNode!=null && snappedNode!=pressedNode)
                    {
                        dummyLink.Source = pressedNode; // hide dummyLink
                        tooltip.ShowBanned();
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
                    Rotate(ped.delta * rotationMultiplier);
            }
            if (Input.touchCount == 2) // if pinch/pan
            {
                Touch t1 = Input.touches[0];
                Touch t2 = Input.touches[1];

                float dist = (t1.position - t2.position).magnitude;
                float prevDist = ((t1.position-t1.deltaPosition) - (t2.position-t2.deltaPosition)).magnitude;
                Zoom(dist - prevDist);
                // Pan((t1.deltaPosition + t2.deltaPosition) / 2);
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
                    if (pressedNode == focusedNode)
                    {
                        pressedNode.Outline(0);
                    }
                    else
                    {
                        pressedNode.Unoutline();
                    }
                    pressedNode = null;

                    if (potentialLink != null)
                    {
                        potentialLink.Unoutline();
                        potentialLink = null;
                    }
                    if (potentialSource != null)
                    {
                        if (potentialSource == focusedNode)
                        {
                            potentialSource.Outline(0);
                        }
                        else
                        {
                            potentialSource.Unoutline();
                        }
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

                    tooltip.Disable();
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
                    if (links[i,j] == null)
                    {
                        AddLink(i, j);
                        OnUserLinked.Invoke(i, j);
                    }
                    else
                    {
                        RemoveLink(i, j);
                        OnUserUnlinked.Invoke(i, j);
                    }
                }
            }
        }


        //////////////////////////////
        // helpers

        // this returns any node within the snap radius
        // if more than one are in the radius, then return the closest to the camera.
        [SerializeField] float snapRadius;
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
                foreach (int i in adjacency.Keys)
                {
                    Node node = nodes[i];
                    Vector2 screenPos = Camera.main.WorldToScreenPoint(node.transform.position);

                    // if the click is within the clickable radius
                    float dist = (ped.position-screenPos).sqrMagnitude;
                    if (dist < radius && dist < closestDist)
                    {
                        closest = node;
                        closestDist = dist;
                    }
                }
                return closest;
            }
        }

	}
}