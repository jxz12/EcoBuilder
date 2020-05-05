using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

namespace EcoBuilder.NodeLink
{
    public partial class Graph :
        IPointerDownHandler, IPointerUpHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        IScrollHandler
    {
        [SerializeField] Tooltip tooltip;
        [SerializeField] float rotationPerInch;
        // [SerializeField] float zoomPerInch, panPerInch;

        public bool AllowSuperfocus { get; set; }
        public enum FocusState { Unfocus, Focus, SuperFocus };
        public FocusState FocusedState { get; private set; } = FocusState.Unfocus;
        Node focusedNode = null;
        private void TapNode(int idx)
        {
            Assert.IsNotNull(nodes[idx], $"no spawned node with index {idx}");

            if (focusedNode != nodes[idx])
            {
                nodes[idx].PushOutline(cakeslice.Outline.Colour.Yellow);
                nodes[idx].Highlight();
                if (focusedNode != null) // focus switched
                {
                    focusedNode.PopOutline();
                    focusedNode.Unhighlight();
                }
            }

            if (FocusedState == FocusState.Unfocus)
            {
                FocusedState = FocusState.Focus;
            }
            else if (FocusedState == FocusState.Focus)
            {
                if (focusedNode == nodes[idx])
                {
                    if (AllowSuperfocus)
                    {
                        SuperFocus(idx);
                        FocusedState = FocusState.SuperFocus;
                    }
                    else
                    {
                        TapEmpty();
                        return;
                    }
                }
            }
            else if (FocusedState == FocusState.SuperFocus)
            {
                if (focusedNode == nodes[idx]) {
                    TapEmpty();
                } else { // switch superfocused node
                    SuperFocus(idx);
                }
            }
            focusedNode = nodes[idx];
            OnNodeTapped?.Invoke(idx);
        }
        private void TapEmpty()
        {
            if (FocusedState == FocusState.Unfocus)
            {
                OnEmptyTapped?.Invoke();
            }
            else if (FocusedState == FocusState.Focus)
            {
                focusedNode.PopOutline();
                focusedNode.Highlight();
                int idx = focusedNode.Idx;
                focusedNode = null;

                FocusedState = FocusState.Unfocus;
                OnUnfocused?.Invoke();
            }
            else if (FocusedState == FocusState.SuperFocus)
            {
                FocusedState = FocusState.Focus;
                foreach (Node no in nodes)
                {
                    no.State = Node.PositionState.Stress;
                    no.SetNewParentKeepPos(yAxle);
                }
                foreach (Link li in links) {
                    li.Show(true);
                }
            }
        }
        public void ForceUnfocus()
        {
            while (FocusedState != FocusState.Unfocus) {
                TapEmpty();
            }
        }
        // for external
        public void ForceFocus(int idx)
        {
            if (focusedNode != nodes[idx]) {
                TapNode(idx);
            }
        }

        private void SuperFocus(int focusIdx)
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
                    no.SetNewParentKeepPos(yAxle);
                }
                // uninteracting
                else if (links[focusIdx,no.Idx] == null && links[no.Idx,focusIdx] == null)
                {
                    unrelated.Add(no);
                    no.State = Node.PositionState.Stress;
                    no.SetNewParentKeepPos(unfocusParent);
                }
                else if (links[focusIdx,no.Idx] != null && links[no.Idx,focusIdx] == null)
                {
                    consumers.Add(no);
                    no.State = Node.PositionState.Focus;
                    no.SetNewParentKeepPos(yAxle);
                }
                else if (links[focusIdx,no.Idx] == null && links[no.Idx,focusIdx] != null)
                {
                    resources.Add(no);
                    no.State = Node.PositionState.Focus;
                    no.SetNewParentKeepPos(yAxle);
                }
                // mutual consumption
                else if (links[focusIdx,no.Idx] != null && links[no.Idx,focusIdx] != null)
                {
                    Debug.LogError("Bidirectional Links should be disabled");
                    both.Add(no);
                    no.State = Node.PositionState.Focus;
                    no.SetNewParentKeepPos(yAxle);
                }
            }
            foreach (Link li in links)
            {
                if (li.Source != nodes[focusIdx] && li.Target != nodes[focusIdx]) {
                    li.Show(false);
                } else {
                    li.Show(true);
                }
            }

            // arrange in circle around focus in middle
            nodes[focusIdx].FocusPos = Vector3.up * focusHeight;

            // both resources and consumers on right
            int right = System.Math.Max(consumers.Count, resources.Count)*2;
            int left = both.Count;

            if (right+left == 0) {
                return;
            }
            float range = Mathf.PI * ((float)(right) / (right+left));
            float angle = 0;
            foreach (Node no in consumers.OrderBy(c=>-c.StressPos.x))
            {
                angle += 1f / (consumers.Count+1) * range;
                no.FocusPos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle),.2f) * focusHeight
                              + nodes[focusIdx].FocusPos;
            }
            angle = 0;
            foreach (Node no in resources.OrderBy(r=>-r.StressPos.x))
            {
                angle -= 1f / (resources.Count+1) * range;
                no.FocusPos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle),.2f) * focusHeight
                              + nodes[focusIdx].FocusPos;
            }
            angle -= 1f / (resources.Count+1) * range;
            range = 2 * Mathf.PI * ((float)(left) / (right+left));
            foreach (Node no in both.OrderBy(b=>b.StressPos.y))
            {
                angle -= 1f / (both.Count+1) * range;
                no.FocusPos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle),.2f) * focusHeight
                              + nodes[focusIdx].FocusPos;
            }
        }


        ///////////////////////////////////////
        // user-interaction rotation and zoom

        float DPI { get {  return Screen.dpi==0? 72 : Screen.dpi; } }

        Vector3 xAxleUnfocused = Vector3.zero;
        public void MoveHorizontal(float xCoord)
        {
            xAxleUnfocused.x = xCoord;
        }

        void UserRotate(Vector2 pixelDelta)
        {
            Vector2 inchesDelta = pixelDelta / DPI; // convert to inches
            Vector2 rotation = inchesDelta * rotationPerInch; // convert to degrees
            yTargetRotation -= rotation.x;
            xTargetRotation += rotation.y;
        }
        /*
        void UserZoom(float pixelDelta)
        {
            float inchesDelta = pixelDelta / DPI; // convert to inches
            float zoom = inchesDelta * zoomPerInch;
            zoom = Mathf.Min(zoom, .5f);
            zoom = Mathf.Max(zoom, -.5f);

            graphParent.localScale *= 1 + zoom;
        }
        IEnumerator TweenZoom(Vector3 endZoom, float duration)
        {
            Vector3 startZoom = graphParent.localScale;
            float startTime = Time.time;
            while (Time.time < startTime + duration)
            {
                float t = (Time.time-startTime) / duration * 2;
                if (t < 1) {
                    graphParent.localScale = startZoom + (endZoom-startZoom)/2f*t*t;
                } else {
                    graphParent.localScale = startZoom - (endZoom-startZoom)/2f*((t-1)*(t-3)-1);
                }
                yield return null;
            }
            graphParent.localScale = endZoom;
        }
        void UserPan(Vector2 pixelDelta)
        {
            Vector2 inchesDelta = pixelDelta / DPI; // convert to inches
            transform.localPosition += (Vector3)inchesDelta * panPerInch;
        }
        IEnumerator TweenPan(Vector3 endPan, float duration)
        {
            Vector3 startZoom = transform.localPosition;
            float startTime = Time.time;
            while (Time.time < startTime + duration)
            {
                float t = (Time.time-startTime) / duration * 2;
                if (t < 1) {
                    transform.localPosition = startZoom + (endPan-startZoom)/2f*t*t;
                } else {
                    transform.localPosition = startZoom - (endPan-startZoom)/2f*((t-1)*(t-3)-1);
                }
                yield return null;
            }
            transform.localPosition = endPan;
        }
        */

        ///////////////////////////////
        // Event Systems (link adding)

        Node pressedNode;
        bool tweenNodes = true;
        public void OnPointerDown(PointerEventData ped)
        {
            // if (ped.pointerId==-1 || (Input.touchCount==1 && ped.pointerId==0))
            if (ped.pointerId==-1 || ped.pointerId==0)
            {
                tweenNodes = false;

                pressedNode = ClosestSnappedNode(ped);
                if (pressedNode != null)
                {
                    tooltip.Show();
                    tooltip.SetPos(mainCam.WorldToScreenPoint(pressedNode.transform.position));
                    pressedNode.PushOutline(cakeslice.Outline.Colour.Orange);
                    if (pressedNode.CanBeFocused) {
                        tooltip.ShowInspect();
                    } else {
                        tooltip.ShowBanned();
                    }
                }
            }
        }
        // called before OnEndDrag
        public void OnPointerUp(PointerEventData ped)
        {
            if (ped.pointerId==-1 || ped.pointerId==0)
            {
                if (pressedNode != null) {
                    pressedNode.PopOutline(); // remove initial orange press outline
                }
                if (!ped.dragging) // if click
                {
                    if (pressedNode != null)
                    {
                        if (pressedNode.CanBeFocused) {
                            TapNode(pressedNode.Idx);
                        }
                        pressedNode = null;
                    }
                    else
                    {
                        TapEmpty();
                    }
                }
                tweenNodes = true;
                tooltip.Show(false);
            }
        }

        Node dummyTarget;
        Link dummyLink;
        Node potentialTarget;
        Link potentialLink;
        public bool DragFromTarget { get; set; }
        bool dragging = false;
        public void OnBeginDrag(PointerEventData ped)
        {
            dragging = true;
            if (ped.pointerId==-1 || ped.pointerId==0)
            {
                if (pressedNode != null)
                {
                    // tooltip.Enable();

                    if (//pressedNode.Interactable &&
                        ((pressedNode.CanBeSource && !DragFromTarget) ||
                         (pressedNode.CanBeTarget && DragFromTarget)))
                    {
                        dummyTarget = Instantiate(nodePrefab, yAxle);
                        dummyTarget.transform.localScale = Vector3.zero;
                        dummyTarget.transform.position = pressedNode.transform.position + Vector3.back;
                        dummyTarget.enabled = false;

                        dummyLink = Instantiate(linkPrefab, transform);
                        dummyLink.Source = pressedNode;
                        dummyLink.Target = dummyTarget;
                        dummyLink.TileSpeed = DragFromTarget? -minLinkFlow : minLinkFlow;

                        pressedNode.PushOutline(cakeslice.Outline.Colour.Orange);
                        dummyLink.PushOutline(cakeslice.Outline.Colour.Orange);
                    }
                    else
                    {
                        tooltip.ShowBanned();
                    }
                }
                else
                {
                    tweenNodes = true;
                    tooltip.Show(false);
                }
            }
        }
        // [SerializeField] float inversePanPerDragInch;
        public void OnDrag(PointerEventData ped)
        {
            if ((ped.pointerId==-1 || ped.pointerId==0)
                && dummyTarget != null) // checks if snapping should even be tried
            {
                if (potentialTarget != null) // remove previous outline if needed
                {
                    pressedNode.PopOutline();
                    potentialTarget.PopOutline();
                    potentialLink.PopOutline();
                    potentialTarget = null;
                    potentialLink = null;
                }
                Node snappedNode = ClosestSnappedNode(ped);

                // a load of crap to determine if a snap occurs
                if (snappedNode!=null && snappedNode!=pressedNode &&
                    // snappedNode.Interactable &&
                    (((!DragFromTarget && snappedNode.CanBeTarget)
                         && ((links[snappedNode.Idx, pressedNode.Idx]==null &&
                              links[pressedNode.Idx, snappedNode.Idx]==null) ||
                             (links[pressedNode.Idx, snappedNode.Idx]!=null &&
                              links[pressedNode.Idx, snappedNode.Idx].Removable)))
                     || ((DragFromTarget && snappedNode.CanBeSource)
                         && ((links[snappedNode.Idx, pressedNode.Idx]==null &&
                              links[pressedNode.Idx, snappedNode.Idx]==null) ||
                             (links[snappedNode.Idx, pressedNode.Idx]!=null &&
                              links[snappedNode.Idx, pressedNode.Idx].Removable))))
                   )
                {
                    tooltip.SetPos(mainCam.WorldToScreenPoint(snappedNode.transform.position));
                    potentialTarget = snappedNode;

                    Link linkToRemove = DragFromTarget? links[snappedNode.Idx, pressedNode.Idx]
                                                      : links[pressedNode.Idx, snappedNode.Idx];
                    if (linkToRemove == null)
                    {
                        dummyLink.Target = snappedNode;
                        potentialLink = dummyLink;

                        pressedNode.PushOutline(cakeslice.Outline.Colour.Green);
                        potentialTarget.PushOutline(cakeslice.Outline.Colour.Green);
                        potentialLink.PushOutline(cakeslice.Outline.Colour.Green);

                        tooltip.ShowAddLink();
                    }
                    else // link to be deleted
                    {
                        dummyLink.Target = pressedNode; // hide dummyLink
                        potentialLink = linkToRemove;

                        pressedNode.PushOutline(cakeslice.Outline.Colour.Red);
                        potentialTarget.PushOutline(cakeslice.Outline.Colour.Red);
                        potentialLink.PushOutline(cakeslice.Outline.Colour.Red);

                        tooltip.ShowUnlink();
                    }
                }
                else // no snap
                {
                    Vector3 screenPoint = ped.position;
                    screenPoint.z = pressedNode.transform.position.z - mainCam.transform.position.z;
                    dummyTarget.transform.position = mainCam.ScreenToWorldPoint(screenPoint + Vector3.back);

                    tooltip.SetPos(screenPoint);
                    if (snappedNode!=null && snappedNode!=pressedNode)
                    {
                        dummyLink.Target = pressedNode; // hide dummyLink
                        tooltip.ShowBanned();
                    }
                    else
                    {
                        dummyLink.Target = dummyTarget; // show dangling link
                        tooltip.ShowLink();
                    }
                }
            }
            if (pressedNode == null)
            {
                // Rotate the whole graph accordingly
                if (ped.pointerId == -1) {
                    UserRotate(ped.delta);
                } else if (Input.touchCount == 1) {
                    UserRotate(ped.delta);
                }
            }
            if (Input.touchCount == 2) // if pinch/pan
            {
                Touch t1 = Input.touches[0];
                Touch t2 = Input.touches[1];

                if (Vector2.Dot(t1.deltaPosition, t2.deltaPosition) <= 0) // if touches moved in opposite directions
                {
                    float dist = (t1.position - t2.position).magnitude;
                    float prevDist = ((t1.position-t1.deltaPosition) - (t2.position-t2.deltaPosition)).magnitude;
                    // UserZoom(dist - prevDist);
                }
                else
                {
                    // UserPan((t1.deltaPosition + t2.deltaPosition) / 2);
                }
            }
            if (ped.pointerId < -1) // or right/middle click
            {
                // UserPan(ped.delta);
                // Zoom(ped.delta.y);
            }
        }
        public void OnEndDrag(PointerEventData ped)
        {
            if (ped.pointerId==-1 || ped.pointerId==0)
            {
                if (potentialTarget != null)
                {
                    // add/remove a new link
                    int i, j;
                    if (!DragFromTarget)
                    {
                        i = pressedNode.Idx;
                        j = potentialTarget.Idx;
                    }
                    else
                    {
                        i = potentialTarget.Idx;
                        j = pressedNode.Idx;
                    }
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

                    pressedNode.PopOutline();
                    potentialTarget.PopOutline();
                    potentialLink.PopOutline();
                    potentialTarget = null;
                    potentialLink = null;
                }

                if (dummyTarget != null)
                {
                    if (dummyLink != null) {
                        Destroy(dummyLink.gameObject);
                    }
                    if (dummyTarget != null) {
                        Destroy(dummyTarget.gameObject);
                    }

                    pressedNode.PopOutline(); // orange outline from drag
                    pressedNode = null;
                }
            }
            dragging = false;
        }
        public void OnScroll(PointerEventData ped)
        {
            // UserZoom(-ped.scrollDelta.y);
            UserRotate(new Vector3(ped.scrollDelta.x, 0));
        }


        /////////////
        // helpers

        // this returns any node within the snap radius
        // if more than one are in the radius, then return the closest to the camera.
        [SerializeField] float snapRadiusInches;
        private Node ClosestSnappedNode(PointerEventData ped)
        {
            GameObject hoveredObject = ped.pointerCurrentRaycast.gameObject;
            Node snappedNode = null;
            if (hoveredObject != null) {
                snappedNode = hoveredObject.GetComponent<Node>();
            }
            if (snappedNode != null)
            {
                return snappedNode;
            }
            else
            {
                Node closest = null;
                float closestDist = float.MaxValue;
                float snapRadiusPxls = snapRadiusInches * DPI;
                float sqRadius = snapRadiusPxls * snapRadiusPxls;
                foreach (Node no in nodes.Where(n=>n.gameObject.activeSelf))
                {
                    Vector2 screenPos = mainCam.WorldToScreenPoint(no.transform.position);

                    // if the click is within the clickable radius
                    float dist = (ped.position-screenPos).sqrMagnitude;
                    if (dist < sqRadius && dist < closestDist)
                    {
                        closest = no;
                        closestDist = dist;
                    }
                }
                return closest;
            }
        }
    }
}