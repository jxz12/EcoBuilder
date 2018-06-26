﻿using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(MeshRenderer))]
public class Square : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    Board parentBoard;
    MeshRenderer mr;
    float defaultAlpha;

    public Color Col {
        get { return mr.material.color; }
        set { mr.material.color = value; }
    }

    void Awake()
    {
        mr = GetComponent<MeshRenderer>();
        defaultAlpha = mr.material.color.a;
    }
    void Start()
    {
        var c = Col;
        c.a = defaultAlpha;
        Col = c;
        parentBoard = transform.parent.GetComponent<Board>();
        if (parentBoard == null)
            throw new Exception("square parent is not have a Board component");
    }

    public void OnPointerEnter(PointerEventData ped)
    {
        // make any dragged piece enter this square
        if (ped.pointerDrag != null)
        {
            Piece dragged = ped.pointerDrag.GetComponent<Piece>();
            if (dragged != null && transform.childCount == 0)
            {
                dragged.transform.SetParent(transform, false);
                parentBoard.PieceAdopted(this, dragged);
            }
        }
        var c = Col;
        c.a = 1;
        Col = c;
    }
    public void OnPointerExit(PointerEventData ped)
    {
        var c = Col;
        c.a = defaultAlpha;
        Col = c;
    }
}