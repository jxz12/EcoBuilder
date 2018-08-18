using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Board : MonoBehaviour
{
    [SerializeField] Square squarePrefab;

    Square[,] squares;
    public event Action<Square> SquareDraggedEvent;
    public event Action<Square> SquarePinched1Event;
    public event Action<Square> SquarePinched2Event;

    private void Awake()
    {
        int size = GameManager.Instance.BoardSize;
        Func<float,float,Color> ColorMap = (x,y)=>ColorHelper.HSVSquare(x,y,.8f);

        Vector3 bottomLeft = new Vector3(-.5f, 0, -.5f); float gap = 1f / size;
        Vector3 offset = new Vector3(gap / 2, 0, gap / 2);
        float squareWidth = (1f / size) * .95f;
        Vector3 scale = new Vector3(squareWidth, squareWidth, squareWidth);

        squares = new Square[size, size];

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                var s = Instantiate(squarePrefab, transform);

                s.transform.localPosition = bottomLeft + new Vector3(i * gap, 0, j * gap) + offset;
                s.transform.localScale = scale;
                s.Init(i, j, ColorMap((float)i / size, (float)j / size));

                s.HoveredEvent += () => hovered = s;
                s.UnhoveredEvent += () => { if (s == hovered) hovered = null; };

                squares[i, j] = s;
            }
        }
    }



    Square hovered=null, dragged=null;

    bool draggingLeftClick=false, draggingRightClick=false;
    private void Update()
    {
        // if left-click is pressed
        if (Input.GetMouseButtonDown(0))
        {
            dragged = hovered;
            draggingLeftClick = true;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            draggingLeftClick = false;
        }

        // if it's the frame right-click is pressed
        if (Input.GetMouseButtonDown(1))
        {
            if (hovered != null)
            {
                SquarePinched1Event.Invoke(hovered);
                SquarePinched2Event.Invoke(hovered);
                dragged = hovered;
                draggingRightClick = true;
            }
        }
        else if (Input.GetMouseButtonUp(1))
        {
            draggingRightClick = false;
        }


        if (hovered != null && hovered != dragged) // if drag has moved
        {
            if (draggingLeftClick == true)
            {
                SquareDraggedEvent.Invoke(hovered);
                dragged = hovered;
            }
            if (draggingRightClick == true)
            {
                SquarePinched2Event.Invoke(hovered);
                dragged = hovered;
            }
        }
    }


    ////public UnityEvent spin;
    //bool spinning = false;
    //public void Spin90Clockwise() //{
    //    if (spinning == false)
    //    {
    //        spinning = true;
    //        StartCoroutine(Spin(60));
    //    }
    //}
    //IEnumerator Spin(int numFrames)
    //{
    //    Quaternion initial = transform.localRotation;
    //    Quaternion target = initial * Quaternion.Euler(Vector3.up * 90);
    //    for (int i = 0; i < numFrames; i++)
    //    {
    //        transform.localRotation = Quaternion.Slerp(initial, target, (float)i / numFrames);
    //        yield return null;
    //    }
    //    transform.localRotation = target;
    //    spinning = false;
    //}
}
    