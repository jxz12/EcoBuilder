using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(MeshRenderer))]
public class Piece : MonoBehaviour, IDragHandler, IBeginDragHandler, IPointerClickHandler
{
    MeshRenderer mr;

    public Color Col {
        get { return mr.material.color; }
        set { mr.material.color = value; }
    }
    public int Idx { get; private set; }
    public bool StaticPos { get; private set; }
    public bool StaticRange { get; private set; }

    private void Awake()
    {
        mr = GetComponent<MeshRenderer>();
    }

    public void Init(int idx, bool staticPos, bool staticRange)
    {
        Idx = idx;
        name = idx.ToString();
        StaticPos = staticPos;
        StaticRange = staticRange;
    }

    public event Action InspectedEvent;
    public event Action ColoredEvent;

    public void ParentToSquare(Square newParent)
    {
        Vector3 oldScale = transform.localScale;
        Quaternion oldRotation = transform.localRotation;
        transform.parent = newParent.transform;
        transform.localPosition = Vector3.zero;
        transform.localScale = oldScale;
        transform.localRotation = oldRotation;

        Col = newParent.Col;
        ColoredEvent.Invoke();
    }

    public void OnDrag(PointerEventData ped)
    {
    }
    public void OnBeginDrag(PointerEventData ped)
    {
        InspectedEvent.Invoke();
    }

    public void OnPointerClick(PointerEventData ped)
    {
        if (ped.clickCount == 1 && !ped.dragging)
            InspectedEvent.Invoke();
    }

}