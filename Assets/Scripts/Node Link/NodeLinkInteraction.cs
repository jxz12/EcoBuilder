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
        [SerializeField] UI.Tooltip tooltip;
        [SerializeField] float rotationMultiplier, zoomMultiplier, panMultiplier;
        [SerializeField] float yMinRotationVelocity, yRotationVelocityTween;
        [SerializeField] float xDefaultRotation, xRotationTween;
        [SerializeField] float holdThreshold;

        enum FocusState { Unfocus, Focus, SuperFocus, Frozen };
        FocusState focusState = FocusState.Unfocus;
        Node focusedNode = null;
        public void FocusNode(int idx) // called on click
        {
            if (focusedNode != nodes[idx] && focusedNode != null)
                focusedNode.Unoutline();

            if (focusState == FocusState.Unfocus)
            {
                focusState = FocusState.Focus;
            }
            else if (focusState == FocusState.Focus)
            {
                if (focusedNode == nodes[idx])
                {
                    SuperFocus(idx);
                    focusState = FocusState.SuperFocus;
                }
            }
            else if (focusState == FocusState.SuperFocus)
            {
                if (focusedNode == nodes[idx])
                {
                    Unfocus();
                }
                else // switch superfocused node
                {
                    SuperFocus(idx);
                }
            }

            focusedNode = nodes[idx];
            nodes[idx].Outline(3);
        }
        public void SwitchFocus(int idx) // urgh, this is needed because of undoing
        {
            if (focusedNode != nodes[idx] && nodes[idx] != null)
            {
                FocusNode(idx);
                OnNodeFocused?.Invoke(idx);
            }
        }
        void Unfocus()
        {
            if (focusState == FocusState.Unfocus)
            {
                // StartCoroutine(ResetPan(Vector2.zero, 1f));
                StartCoroutine(ResetZoom(Vector3.one, 1f));
            }
            else if (focusState == FocusState.Focus)
            {
                focusedNode.Unoutline();
                focusedNode = null;
                focusState = FocusState.Unfocus;
            }
            else if (focusState == FocusState.SuperFocus)
            {
                focusState = FocusState.Focus;
                foreach (Node no in nodes)
                {
                    no.State = Node.PositionState.Stress;
                    no.SetNewParent(nodesParent);
                }
                foreach (Link li in links)
                    li.Show(true);
            }
        }
        public void FullUnfocus()
        {
            if (focusState != FocusState.Unfocus)
            {
                focusedNode.Unoutline();
                focusedNode = null;
                if (focusState == FocusState.SuperFocus)
                {
                    foreach (Node no in nodes)
                    {
                        no.State = Node.PositionState.Stress;
                        no.SetNewParent(nodesParent);
                    }
                    foreach (Link li in links)
                        li.Show(true);
                }
                focusState = FocusState.Unfocus;
            }
        }

        public void SuperFocus(int focusIdx)
        {
            var unrelated = new List<Node>();
            var consumers = new List<Node>();
            var resources = new List<Node>();
            var both = new List<Node>();

            foreach (Node no in nodes)
            {

                if (no.Idx == focusIdx)
                {
                    no.State = Node.PositionState.Focus;
                    no.SetNewParent(nodesParent);
                }
                // uninteracting
                else if (links[focusIdx,no.Idx] == null && links[no.Idx,focusIdx] == null)
                {
                    unrelated.Add(no);
                    no.State = Node.PositionState.Stress;
                    no.SetNewParent(unfocusParent);
                }
                else if (links[focusIdx,no.Idx] != null && links[no.Idx,focusIdx] == null)
                {
                    consumers.Add(no);
                    no.State = Node.PositionState.Focus;
                    no.SetNewParent(nodesParent);
                }
                else if (links[focusIdx,no.Idx] == null && links[no.Idx,focusIdx] != null)
                {
                    resources.Add(no);
                    no.State = Node.PositionState.Focus;
                    no.SetNewParent(nodesParent);
                }
                // mutual consumption
                else if (links[focusIdx,no.Idx] != null && links[no.Idx,focusIdx] != null)
                {
                    Debug.LogError("Bidirectional Links should be disabled");
                    both.Add(no);
                    no.State = Node.PositionState.Focus;
                    no.SetNewParent(nodesParent);
                }
            }
            foreach (Link li in links)
            {
                if (li.Source != nodes[focusIdx] && li.Target != nodes[focusIdx])
                    li.Show(false);
                else
                    li.Show(true);
            }


            // arrange in circle around focus in middle
            nodes[focusIdx].FocusPos = new Vector3(0, maxHeight/2, 0);

            // both resources and consumers on right
            int right = System.Math.Max(consumers.Count, resources.Count)*2;
            int left = both.Count;

            if (right+left == 0)
                return;

            float range = Mathf.PI * ((float)(right) / (right+left));
            float angle = 0;
            foreach (Node no in consumers.OrderBy(c=>-c.StressPos.x))
            {
                angle += 1f / (consumers.Count+1) * range;
                no.FocusPos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle),.2f) * (maxHeight/2)
                              + nodes[focusIdx].FocusPos;
            }
            angle = 0;
            foreach (Node no in resources.OrderBy(r=>-r.StressPos.x))
            {
                angle -= 1f / (resources.Count+1) * range;
                no.FocusPos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle),.2f) * (maxHeight/2)
                              + nodes[focusIdx].FocusPos;
            }
            angle -= 1f / (resources.Count+1) * range;
            range = 2 * Mathf.PI * ((float)(left) / (right+left));
            foreach (Node no in both.OrderBy(b=>b.StressPos.y))
            {
                angle -= 1f / (both.Count+1) * range;
                no.FocusPos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle),.2f) * (maxHeight/2)
                              + nodes[focusIdx].FocusPos;
            }
        }


        ///////////////////////////////////////
        // user-interaction rotation and zoom

        Vector3 graphParentUnfocused;
        public void MoveHorizontal(float xCoord)
        {
            graphParentUnfocused.x = xCoord;
        }
        public void Freeze()
        {
            if (focusedNode != null)
            {
                focusedNode.Unoutline();
                focusedNode = null;
            }
            focusState = FocusState.Unfocus;
            StartCoroutine(ResetZoom(Vector3.one, 1f));

            GetComponent<Animator>().SetTrigger("Freeze");
            focusState = FocusState.Frozen;
        }

        bool doLayout = true;
        void PauseLayout(bool paused)
        {
            if (doLayout != paused) // if already paused
                return;
            doLayout = !paused;
        }

        void Zoom(float amount)
        {
            float zoom = amount * zoomMultiplier;
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

        float yRotation = 0, yRotationVelocity = 0;
        void UserRotate(Vector2 delta)
        {
            var amount = delta / (Screen.dpi==0? 72:Screen.dpi);
            float ySpin = -amount.x * rotationMultiplier;
            float xSpin = amount.y * rotationMultiplier;
            yRotationVelocity = ySpin;

            if (focusState != FocusState.SuperFocus)
            {
                if (constrainTrophic)
                {
                    yRotation += ySpin;
                    nodesParent.transform.localRotation = Quaternion.Euler(0,yRotation,0);
                    graphParent.Rotate(Vector3.right, xSpin);
                }
                // else
                // {
                //     yRotation += ySpin;
                //     nodesParent.transform.localRotation = Quaternion.Euler(0,yRotation,0);
                //     Quaternion rotator =
                //         Quaternion.AngleAxis(-yRotation, Vector3.up) *
                //         Quaternion.AngleAxis(xSpin, Vector3.right) *
                //         Quaternion.AngleAxis(yRotation, Vector3.up);
                //     // Quaternion rotator =
                //     //     Quaternion.AngleAxis(ySpin, Vector3.up) * 
                //     //     Quaternion.AngleAxis(xSpin, Vector3.right);
                //     foreach (Node no in nodes)
                //     {
                //         no.StressPos -= rotationCenter;
                //         no.StressPos = rotator * no.StressPos;
                //         no.StressPos += rotationCenter;

                //         no.transform.localPosition -= rotationCenter;
                //         no.transform.localPosition = rotator * no.transform.localPosition;
                //         no.transform.localPosition += rotationCenter;
                //     }
                // }
            }
            else
            {
                nodesParent.transform.Rotate(Vector3.up, ySpin);
            }

        }
        private void MomentumRotate()
        {
            if (focusState != FocusState.SuperFocus)
            {
                if (constrainTrophic)
                {
                    while (yRotation < -180)
                        yRotation += 360;
                    while (yRotation > 180)
                        yRotation -= 360;

                    if (yRotation < -45) // TODO: magic numbers?
                        yRotationVelocity = Mathf.Lerp(yRotationVelocity, yMinRotationVelocity, yRotationVelocityTween);
                    else if (yRotation > 45)
                        yRotationVelocity = Mathf.Lerp(yRotationVelocity, -yMinRotationVelocity, yRotationVelocityTween);
                    else
                        yRotationVelocity = Mathf.Lerp(yRotationVelocity, Mathf.Sign(yRotationVelocity)*yMinRotationVelocity, yRotationVelocityTween);

                    // nodesParent.Rotate(Vector3.up, yRotationVelocity);
                    yRotation += yRotationVelocity;
                    nodesParent.localRotation = Quaternion.Euler(0,yRotation,0);
                    var graphParentGoal = Quaternion.Euler(xDefaultRotation, 0, 0);
                    graphParent.localRotation = Quaternion.Lerp(graphParent.transform.localRotation, graphParentGoal, xRotationTween);
                }
                // else
                // {
                    
                // }
            }
            else
            {
                nodesParent.localRotation = Quaternion.Lerp(nodesParent.localRotation, Quaternion.identity, xRotationTween);
                graphParent.transform.localRotation = Quaternion.Lerp(graphParent.transform.localRotation, Quaternion.identity, xRotationTween);
            }
        }

        ///////////////////////////////
        // Event Systems (link adding)

        Node dummySource;
        Link dummyLink;
        Node potentialSource;
        Link potentialLink;
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
                        OnNodeFocused?.Invoke(pressedNode.Idx);

                        if (pressedNode == focusedNode)
                            pressedNode.Outline(3);
                        else
                            pressedNode.Unoutline();

                        pressedNode = null;
                        tooltip.Disable();
                    }
                    else if (focusState != FocusState.Frozen)
                    {
                        if (focusState == FocusState.Unfocus)
                        {
                            OnEmptyPressed?.Invoke();
                        }
                        else if (focusState == FocusState.Focus)
                        {
                            OnUnfocused?.Invoke();
                        }
                        Unfocus();
                    }
                }
            }
        }
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
                        dummySource.enabled = false;
                        dummyLink = Instantiate(linkPrefab, linksParent);
                        dummyLink.Target = pressedNode;
                        dummyLink.Source = dummySource;
                        dummyLink.TileSpeed = minLinkFlow;

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
                    && ((   links[snappedNode.Idx, pressedNode.Idx]==null
                         && links[pressedNode.Idx, snappedNode.Idx]==null)
                        ||
                        (   links[snappedNode.Idx, pressedNode.Idx]!=null
                         && links[snappedNode.Idx, pressedNode.Idx].Removable)))
                {
                    tooltip.SetPos(Camera.main.WorldToScreenPoint(snappedNode.transform.position));
                    if (potentialSource != snappedNode) // if not same as previous
                    {
                        if (potentialSource != null)
                        {
                            if (potentialSource == focusedNode)
                                potentialSource.Outline(1);
                            else
                                potentialSource.Unoutline();
                        }
                        potentialSource = snappedNode;
                    }

                    Link snappedLink = links[snappedNode.Idx, pressedNode.Idx];
                    if (snappedLink == null) // link to be added
                    {
                        dummyLink.Source = snappedNode;
                        if (potentialLink != dummyLink)
                        {
                            potentialLink.Unoutline();
                            potentialLink = dummyLink;
                            if (potentialSource == focusedNode)
                                potentialSource.Outline(1);
                            else
                                potentialSource.Unoutline();
                        }
                        pressedNode.Outline(1);
                        potentialSource.Outline(1);
                        potentialLink.Outline(1);

                        tooltip.ShowAddLink();
                    }
                    else // link to be deleted
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
                            potentialSource.Outline(3);
                        else
                            potentialSource.Unoutline();

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
                    UserRotate(ped.delta);
                else if (Input.touchCount == 1)
                    UserRotate(ped.delta);
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
                Zoom(ped.delta.y);
            }
        }
        public void OnEndDrag(PointerEventData ped)
        {
            if (ped.pointerId==-1 || ped.pointerId==0)
            {
                if (pressedNode != null)
                {
                    if (pressedNode == focusedNode)
                        pressedNode.Outline(3);
                    else
                        pressedNode.Unoutline();

                    pressedNode = null;

                    if (potentialLink != null)
                    {
                        potentialLink.Unoutline();
                        potentialLink = null;
                    }
                    if (potentialSource != null)
                    {
                        if (potentialSource == focusedNode)
                            potentialSource.Outline(3);
                        else
                            potentialSource.Unoutline();

                        potentialSource = null;
                    }
                    if (dummyLink != null)
                        Destroy(dummyLink.gameObject);

                    if (dummySource != null)
                        Destroy(dummySource.gameObject);

                    tooltip.Disable();
                }
            }
        }
        public void OnScroll(PointerEventData ped)
        {
            Zoom(ped.scrollDelta.y);
            UserRotate(new Vector3(ped.scrollDelta.x, 0));
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
                        OnUserLinked?.Invoke(i, j);
                    }
                    else
                    {
                        RemoveLink(i, j);
                        OnUserUnlinked?.Invoke(i, j);
                    }
                }
            }
        }


        /////////////
        // helpers

        // this returns any node within the snap radius
        // if more than one are in the radius, then return the closest to the camera.
        [SerializeField] float snapRadius;
        private Node ClosestSnappedNode(PointerEventData ped)
        {
            if (focusState == FocusState.Frozen)
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
                radius /= Screen.dpi==0? 72:Screen.dpi;
                foreach (Node no in nodes)
                {
                    Vector2 screenPos = Camera.main.WorldToScreenPoint(no.transform.position);

                    // if the click is within the clickable radius
                    float dist = (ped.position-screenPos).sqrMagnitude;
                    if (dist < radius && dist < closestDist)
                    {
                        closest = no;
                        closestDist = dist;
                    }
                }
                return closest;
            }
        }


        ////////////
        // effects

        public void FlashNode(int idx)
        {
            nodes[idx].Flash(true);
        }
        public void UnflashNode(int idx)
        {
            nodes[idx].Flash(false);
        }
        public void SkullEffectNode(int idx)
        {
            Instantiate(skullPrefab, nodes[idx].transform);
        }
        public void HeartEffectNode(int idx)
        {
            Instantiate(heartPrefab, nodes[idx].transform);
        }
        public void BounceNode(int idx)
        {
            nodes[idx].Bounce();
        }

        // adds a gameobject to the point (0,-1,0)
        // like the jump shadow in mario bros.
        public void AddDropShadow(GameObject shadowPrefab)
        {
            if (shadowPrefab == null)
                return;

            var shadow = Instantiate(shadowPrefab);
            shadow.transform.SetParent(nodesParent, false);
            shadow.transform.localPosition = Vector3.down * 1f;
            shadow.transform.localRotation = Quaternion.AngleAxis(-45, Vector3.up);
        }
        public void SetNodeDefaultOutline(int idx, int colourIdx=0)
        {
            nodes[idx].DefaultOutline = colourIdx;
        }
        public void SetLinkDefaultOutline(int source, int target, int colourIdx=0)
        {
            links[source, target].DefaultOutline = colourIdx;
        }
        public void OutlineNode(int idx, int colourIdx=4)
        {
            nodes[idx].Outline(colourIdx);
        }
        public void UnoutlineNode(int idx)
        {
            nodes[idx].Unoutline();
        }
        public void TooltipNode(int idx, string msg)
        {
            tooltip.transform.position = Camera.main.WorldToScreenPoint(nodes[idx].transform.position);
            tooltip.ShowText(msg);
            tooltip.Enable();
            // frozen = true; // TODO: this might bite me in the ass
        }
        public void UntooltipNode(int idx)
        {
            tooltip.Disable();
            // frozen = false;
        }
	}
}