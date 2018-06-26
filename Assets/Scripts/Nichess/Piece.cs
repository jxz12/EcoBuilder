using System;
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

    public void Init(int idx)
    {
        Idx = idx;
        name = idx.ToString();
    }

    private void Awake()
    {
        mr = GetComponent<MeshRenderer>();
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


    //Square habitat;
    //public Square Habitat {
    //    get { return habitat; }
    //    set { habitat = value; }
    //}
    public Square Habitat;
    public Tuple<Square, Square> Niche { get; set; }

}