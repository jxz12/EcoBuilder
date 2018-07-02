using System.Collections;
using UnityEngine;
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

    public Square NicheStart { get; set; }
    public Square NicheEnd { get; set; }

    public void Init(int idx)
    {
        Idx = idx;
        name = idx.ToString();
    }

    public bool InitialMode { get; private set; }
    private void Awake()
    {
        mr = GetComponent<MeshRenderer>();
        InitialMode = true;
    }

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
    void Update()
    {
    }

    public void OnDrag(PointerEventData ped)
    {
    }
    public void OnBeginDrag(PointerEventData ped)
    {
        SpeciesManager.Instance.InspectSpecies(Idx);
    }

    public void OnPointerClick(PointerEventData ped)
    {
        if (ped.clickCount == 1 && !ped.dragging)
            SpeciesManager.Instance.InspectSpecies(Idx);
        else if (ped.clickCount == 2)
            SpeciesManager.Instance.ExtinctSpecies(Idx);
    }

}