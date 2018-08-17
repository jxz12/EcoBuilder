using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(LineRenderer))]
public class Piece : MonoBehaviour, IDragHandler, IBeginDragHandler, IPointerClickHandler
{
    MeshRenderer mr;
    LineRenderer lr;

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
        lr = GetComponent<LineRenderer>();
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
    public Square NicheStart {get; set; }
    public Square NicheEnd { get; set; }
    public void DrawLasso() {
        if (NicheStart != null && NicheEnd != null)
        {
            lr.enabled = true;
            Vector3 a = NicheStart.transform.position, b = NicheEnd.transform.position;
            lr.SetPosition(0, new Vector3(a.x, 0, a.z));
            lr.SetPosition(1, new Vector3(a.x, 0, b.z));
            lr.SetPosition(2, new Vector3(b.x, 0, b.z));
            lr.SetPosition(3, new Vector3(b.x, 0, a.z));
        }
        else 
        {
            lr.enabled = false;
        }
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