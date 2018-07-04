using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(MeshRenderer))]
public class Piece : MonoBehaviour, IDragHandler, IBeginDragHandler, IPointerClickHandler
{
    MeshRenderer mr;
    //[SerializeField] TrailRenderer tr;

    public Color Col {
        get { return mr.material.color; }
        set { mr.material.color = value; }
    }
    public int Idx { get; private set; }
    public bool InitialMode { get; private set; }
    public bool StaticPos { get; private set; }
    public bool StaticRange { get; private set; }

    public Square NicheStart { get; set; }
    public Square NicheEnd { get; set; }

    UnityEvent<int> InspectEvent;
    public void Init(int idx, bool staticPos, bool staticRange, UnityEvent<int> InspectEvent)
    {
        Idx = idx;
        name = idx.ToString();
        StaticPos = staticPos;
        StaticRange = staticRange;
        this.InspectEvent = InspectEvent;
    }

    private void Awake()
    {
        mr = GetComponent<MeshRenderer>();
        InitialMode = true;
    }

    //float angle = 0;
    //void Update()
    //{
    //    Vector3 cornerBL = NicheStart.transform.TransformPoint(-.5f, -.5f, 0);
    //    Vector3 cornerTR = NicheEnd.transform.TransformPoint(.5f, .5f, 0);

    //    //float xMin = -1, xMax = 1, yMin = -5, yMax = 5;
    //    //float xMin = min.x, xMax = max.x, yMin = min.y, yMax = max.y;

    //    if (angle < 1)
    //    {
    //        tr.transform.position = Vector3.Lerp(cornerBL, cornerTR, angle);
    //    }
    //    else if (angle < 2)
    //    {
    //        tr.transform.position = Vector3.Lerp(cornerTR, cornerBL, angle - 1);
    //    }
    //    //else if (angle < 3)
    //    //{
    //    //    tr.transform.position = new Vector3((xMin-xMax)*(angle-2) + xMax, 0, yMin);
    //    //}
    //    //else if (angle < 4)
    //    //{
    //    //    tr.transform.position = new Vector3(xMin, 0, (yMax-yMin)*(angle-3) + yMin);
    //    //}

    //    angle += .05f;
    //    if (angle >= 2)
    //        angle = 0;
    //}

    public void Parent(Transform newParent)
    {
        Vector3 oldScale = transform.localScale;
        Quaternion oldRotation = transform.localRotation;
        transform.parent = newParent;
        transform.localPosition = Vector3.zero;
        transform.localScale = oldScale;
        transform.localRotation = oldRotation;

        if (InitialMode == true)
        {
            Square placement = transform.parent.GetComponent<Square>();
            if (placement != null)
                NicheStart = NicheEnd = placement;
        }
    }
    public void SetNiche(Square start, Square end)
    {
        NicheStart = start;
        NicheEnd = end;
        InitialMode = false;
    }

    public void OnDrag(PointerEventData ped)
    {
    }
    public void OnBeginDrag(PointerEventData ped)
    {
        InspectEvent.Invoke(Idx);
    }

    public void OnPointerClick(PointerEventData ped)
    {
        if (ped.clickCount == 1 && !ped.dragging)
            InspectEvent.Invoke(Idx);
        else if (ped.clickCount == 3)
            SpeciesManager.Instance.ExtinctSpecies(Idx); // change this later
    }

}