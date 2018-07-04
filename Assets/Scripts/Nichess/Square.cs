using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(MeshRenderer))]
public class Square : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler//, IPointerDownHandler
{
    Board parentBoard;
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
                //dragged.transform.SetParent(transform, false);
                dragged.Parent(transform);
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
    //public void OnPointerDown(PointerEventData ped)
    //{
    //    //if (ped.pointerId == -1)
    //    //    print("hello");
    //    //if (ped.pointerId == -2)
    //    //    print("hello2");
    //}
}