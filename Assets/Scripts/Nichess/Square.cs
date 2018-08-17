﻿using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(MeshRenderer))]
public class Square : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    MeshRenderer mr;
    float defaultAlpha;

    public Color Col {
        get { return mr.material.color; }
        private set { mr.material.color = value; }
    }
    public int X { get; private set; }
    public int Y { get; private set; }

    void Awake()
    {
        mr = GetComponent<MeshRenderer>();
        defaultAlpha = mr.material.color.a;
    }

    public void Init(int x, int y, Color c)
    {
        X = x;
        Y = y;
        Col = c;
        name = x + " " + y;
    }
    void Start()
    {
        var c = Col;
        c.a = defaultAlpha;
        Col = c;
    }

    public event Action HoveredEvent;
    public event Action UnhoveredEvent;
    public void OnPointerEnter(PointerEventData ped)
    {
        // make any dragged piece enter this square
        if (ped.pointerDrag != null)
        {
            Piece dragged = ped.pointerDrag.GetComponent<Piece>();
            if (dragged != null && transform.childCount == 0)
            {
                //dragged.transform.SetParent(transform, false);
                dragged.ParentToSquare(this);
            }
        }

        var c = Col;
        c.a = 1;
        Col = c;
        HoveredEvent.Invoke();
    }
    public void OnPointerExit(PointerEventData ped)
    {
        var c = Col;
        c.a = defaultAlpha;
        Col = c;
        UnhoveredEvent.Invoke();
    }
}