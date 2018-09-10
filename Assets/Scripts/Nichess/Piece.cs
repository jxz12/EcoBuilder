using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(Animator))]
public class Piece : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler
{
    [SerializeField] MeshRenderer shape;
    [SerializeField] LineRenderer lassoLoop;
    LineRenderer lassoSpoke;
    Animator anim;

    public Color Col {
        get { return shape.material.color; }
        set { shape.material.color = value; }
    }
    public int Idx { get; private set; }
    public float Lightness { get; private set; }

    public bool StaticPos { get; set; }
    public bool StaticRange { get; set; }

    private void Awake()
    {
        lassoSpoke = GetComponent<LineRenderer>();
        anim = GetComponent<Animator>();
    }

    public void Init(int idx, float lightness)
    {
        Idx = idx;
        name = idx.ToString();
        Lightness = lightness;
    }

    public void Inspect()
    {
        // TODO: sort this out
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

        Col = ColorHelper.SetLightness(newParent.Col, Lightness);
        // Col = newParent.Col;
        lassoSpoke.startColor = lassoSpoke.endColor = lassoLoop.startColor = lassoLoop.endColor = Col;
        ColoredEvent.Invoke();

        DrawLasso();
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
        if (ped.button == PointerEventData.InputButton.Left)
        {
            anim.SetTrigger("Drag"); // TODO: this is a bit of a mess atm, should be controlled from Nichess
            Dragging = true;
            DragStartedEvent.Invoke();
        }
    }
    public void OnEndDrag(PointerEventData ped)
    {
        if (ped.button == PointerEventData.InputButton.Left)
        {
            anim.SetTrigger("Inspect");
            Dragging = false;
        }
    }

    public void OnPointerClick(PointerEventData ped)
    {
        if (ped.button == PointerEventData.InputButton.Left)
        {
            anim.SetTrigger("Inspect");
            if (ped.clickCount == 1 && !ped.dragging)
                ClickedEvent.Invoke();
        }
    }

    void DrawLasso()
    {
        if (NicheStart != null && NicheEnd != null)
        {
            lassoSpoke.enabled = lassoLoop.enabled = true;
            DrawLasso(NicheStart, NicheEnd);
        }
        else 
        {
            lassoSpoke.enabled = lassoLoop.enabled = false;;
        }
    }
    void DrawLasso(Square s1, Square s2)
    {
        int x1 = s1.X, x2 = s2.X;
        int y1 = s1.Y, y2 = s2.Y;
        float lassoStartLocalX = x1<=x2? -.5f : .5f;
        float lassoStartLocalY = y1<=y2? -.5f : .5f;

        Vector3 lassoStart = s1.transform.TransformPoint(lassoStartLocalX,0,lassoStartLocalY);
        Vector3 lassoEnd = s2.transform.TransformPoint(-lassoStartLocalX,0,-lassoStartLocalY);

        // some maths to calculate the adjacent corners of the lasso
        float nicheWidth = x1<=x2? x2-x1+1 : x2-x1-1;
        float nicheHeight = y1<=y2? y2-y1+1 : y2-y1-1;
        float rotationAngle = -2 * Mathf.Atan2(nicheHeight, nicheWidth);
        float cos = Mathf.Cos(rotationAngle);
        float sin = Mathf.Sin(rotationAngle);

        // by rotating the corners around the middle
        Vector3 lassoMiddle = (lassoEnd + lassoStart) / 2f;
        Vector3 v = lassoStart - lassoMiddle;
        Vector3 lassoCorner1 = new Vector3(v.x*cos-v.z*sin, 0, v.x*sin+v.z*cos) + lassoMiddle;
        v = lassoEnd - lassoMiddle;
        Vector3 lassoCorner2 = new Vector3(v.x*cos-v.z*sin, 0, v.x*sin+v.z*cos) + lassoMiddle;

        if (x1 <= x2 && y1 <= y2 || x1 > x2 && y1 > y2)
            DrawLasso(new Vector3[]{ lassoStart, lassoCorner1, lassoEnd, lassoCorner2 });
        else
            DrawLasso(new Vector3[]{ lassoStart, lassoCorner2, lassoEnd, lassoCorner1 });
    }
    void DrawLasso(Vector3[] v)
    {
        if (v.Length != 4)
            throw new Exception("lasso loop is not quadrilateral");
        Vector3 handle = transform.position;
        Vector3 middle = (v[0] + v[2]) / 2;
        Vector3 middle2 = (v[1] + v[3]) / 2;
        if (middle != middle2)
            throw new Exception("lasso loop is not a rectangle");

        float d1 = DistanceFromLine(handle, v[0], v[2]);
        float d2 = DistanceFromLine(handle, v[3], v[1]);

        Vector3 honda;
        if (d1 < 0 && d2 <= 0)
            honda = LineIntersection(handle, middle, v[0], v[1]);
        else if (d1 < 0 && d2 > 0)
            honda = LineIntersection(handle, middle, v[1], v[2]);
        else if (d1 >= 0 && d2 > 0)
            honda = LineIntersection(handle, middle, v[2], v[3]);
        else //if (d1 >= 0 && d2 <= 0)
            honda = LineIntersection(handle, middle, v[3], v[0]);

        lassoLoop.loop = true;
        lassoLoop.positionCount = 4;
        lassoLoop.SetPosition(0, v[0]);
        lassoLoop.SetPosition(1, v[1]);
        lassoLoop.SetPosition(2, v[2]);
        lassoLoop.SetPosition(3, v[3]);

        lassoSpoke.loop = false;
        lassoSpoke.positionCount = 2;
        lassoSpoke.SetPosition(0, handle);
        lassoSpoke.SetPosition(1, honda);
    }
    float DistanceFromLine(Vector3 point, Vector3 line1, Vector3 line2)
    {
        return (point.x - line1.x) * (line2.z - line1.z) - (point.z - line1.z) * (line2.x - line1.x);
    }
    Vector3 LineIntersection(Vector3 from, Vector3 to, Vector3 line1, Vector3 line2)
    {
        float x1 = from.x, x2 = to.x, x3 = line1.x, x4 = line2.x;
        float y1 = from.z, y2 = to.z, y3 = line1.z, y4 = line2.z;
        float xP = ((x1*y2-y1*x2)*(x3-x4) - (x1-x2)*(x3*y4-y3*x4)) / ((x1-x2)*(y3-y4) - (y1-y2)*(x3-x4));
        float yP = ((x1*y2-y1*x2)*(y3-y4) - (y1-y2)*(x3*y4-y3*x4)) / ((x1-x2)*(y3-y4) - (y1-y2)*(x3-x4));
        return new Vector3(xP, 0, yP);
    }


}