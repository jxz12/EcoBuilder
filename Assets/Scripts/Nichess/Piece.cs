using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Animator))]
public class Piece : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler
{
    [SerializeField] MeshRenderer mr;
    LineRenderer lr;
    BoxCollider bc;
    Animator anim;

    public Color Col {
        get { return mr.material.color; }
        set { mr.material.color = value; }
    }
    public int Idx { get; private set; }
    public bool StaticPos { get; private set; }
    public bool StaticRange { get; private set; }

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        bc = GetComponent<BoxCollider>();
        anim = GetComponent<Animator>();
    }

    public void Init(int idx, bool staticPos, bool staticRange)
    {
        Idx = idx;
        name = idx.ToString();
        StaticPos = staticPos;
        StaticRange = staticRange;
    }

    public void Inspect()
    {
        // if (!dragging)
        //     anim.SetTrigger("Inspect");
        // else
        //     anim.SetTrigger("Drag");

    }
    public void Uninspect()
    {
        anim.SetTrigger("Idle");
    }

    public Square NichePos { get; private set; }
    Square nicheStart, nicheEnd;
    public Square NicheStart {
        get { return nicheStart; }
        set { nicheStart = value; DrawLasso(); }
    }
    public Square NicheEnd {
        get { return nicheEnd; }
        set { nicheEnd = value; DrawLasso(); }
    }

    public event Action ClickedEvent;
    public event Action DragStartedEvent;
    public event Action ColoredEvent;
    public void ParentToSquare(Square newParent)
    {
        NichePos = newParent;

        transform.parent = newParent.transform;
        transform.localScale = Vector3.one;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        Col = newParent.Col;
        lr.startColor = lr.endColor = Col;
        ColoredEvent.Invoke();
    }

    public void DrawLasso()
    {
        if (NicheStart != null && NicheEnd != null)
        {
            lr.enabled = true;

            int x1 = NicheStart.X, x2 = NicheEnd.X;
            int y1 = NicheStart.Y, y2 = NicheEnd.Y;
            float lassoStartLocalX = x1<=x2? -.5f : .5f;
            float lassoStartLocalY = y1<=y2? -.5f : .5f;

            Vector3 lassoStart = NicheStart.transform.TransformPoint(lassoStartLocalX,0,lassoStartLocalY);
            Vector3 lassoEnd = NicheEnd.transform.TransformPoint(-lassoStartLocalX,0,-lassoStartLocalY);
            lr.SetPosition(0, lassoStart);
            lr.SetPosition(2, lassoEnd);

            // some maths to calculate the adjacent corners of the lasso
            float nicheWidth = x1<=x2? x2-x1+1 : x2-x1-1;
            float nicheHeight = y1<=y2? y2-y1+1 : y2-y1-1;
            float rotationAngle = -2 * Mathf.Atan2(nicheHeight, nicheWidth);
            float cos = Mathf.Cos(rotationAngle);
            float sin = Mathf.Sin(rotationAngle);

            // by rotating the corners around the middle
            Vector3 lassoMiddle = (lassoEnd + lassoStart) / 2f;
            Vector3 v = lassoStart - lassoMiddle;
            v = new Vector3(v.x*cos-v.z*sin, v.y, v.x*sin+v.z*cos);
            lr.SetPosition(1, v + lassoMiddle);
            v = lassoEnd - lassoMiddle;
            v = new Vector3(v.x*cos-v.z*sin, v.y, v.x*sin+v.z*cos);
            lr.SetPosition(3, v + lassoMiddle);
        }
        else 
        {
            lr.enabled = false;
        }
    }
    public bool IsResourceOf(Piece consumer)
    {
        if (NichePos == null || consumer.NicheStart == null || consumer.NicheEnd == null)
            return false;

        int resX = NichePos.X;
        int resY = NichePos.Y;
        int conX1 = consumer.NicheStart.X;
        int conX2 = consumer.NicheEnd.X;
        int conY1 = consumer.NicheStart.Y;
        int conY2 = consumer.NicheEnd.Y;
        if ((resX>=conX1 && resX<=conX2 || resX<=conX1 && resX>=conX2) &&
            (resY>=conY1 && resY<=conY2 || resY<=conY1 && resY>=conY2))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool Dragging { get; private set; }
    public void OnDrag(PointerEventData ped)
    {
    }
    public void OnBeginDrag(PointerEventData ped)
    {
        anim.SetTrigger("Drag"); // TODO: this is a bit of a mess atm, prob want to move things into statecontrollers
        Dragging = true;
        DragStartedEvent.Invoke();
    }
    public void OnEndDrag(PointerEventData ped)
    {
        anim.SetTrigger("Inspect");
        Dragging = false;
    }

    public void OnPointerClick(PointerEventData ped)
    {
        anim.SetTrigger("Inspect");
        if (ped.clickCount == 1 && !ped.dragging)
            ClickedEvent.Invoke();
    }

}