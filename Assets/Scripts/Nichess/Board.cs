using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EcoBuilder.Nichess
{
    public class Board : MonoBehaviour
    {
        [SerializeField] Square squarePrefab;
        [SerializeField] float defaultYColor=.5f;
        [SerializeField] float squareBorder=.05f;

        Square[,] squares;
        public event Action<Square> SquareDraggedEvent;
        // public event Action<Square> SquareDroppedEvent;
        public event Action<Square> SquarePinched1Event;
        public event Action<Square> SquarePinched2Event;

        private void Awake()
        {
            int size = GameManager.Instance.BoardSize;
            // Func<float,float,Color> ColorMap = (x,y)=>ColorHelper.HSVSquare(x,y,1f);
            // Func<float,float,Color> ColorMap = (x,y)=>ColorHelper.HSLSquare(x,y,.5f);
            Func<float,float,Color> ColorMap = (x,y)=>ColorHelper.YUVtoRGBtruncated(defaultYColor,.5f*x,.5f*y);
            // Func<float,float,Color> ColorMap = (x,y)=>ColorHelper.YUVtoRGBtruncated(defaultYColor,x,y);

            Vector3 bottomLeft = new Vector3(-.5f, 0, -.5f);
            float gap = 1f / size;
            Vector3 offset = new Vector3(gap / 2, 0, gap / 2);
            float squareWidth = (1f / size) * (1f-squareBorder);
            Vector3 scale = new Vector3(squareWidth, squareWidth, squareWidth);

            squares = new Square[size, size];

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    var s = Instantiate(squarePrefab, transform);

                    s.transform.localPosition = bottomLeft + new Vector3(i * gap, 0, j * gap) + offset;
                    s.transform.localScale = scale;
                    // Color c = ColorMap((float)i/(size-1), (float)j/(size-1));
                    Color c = ColorMap((float)i/(size-1)-.5f, (float)j/(size-1)-.5f);
                    c = ColorHelper.ApplyGamma(c);
                    s.Init(i, j, c);

                    // s.HoveredEvent += x => hovered = s;
                    // s.UnhoveredEvent += x => { if (s == hovered) hovered = null; };
                    s.HoveredEvent += x => Hover(s, x);
                    s.UnhoveredEvent += x => Unhover(s, x);

                    squares[i, j] = s;
                }
            }
        }

        HashSet<int> hoveredPointerIds = new HashSet<int>();
        Square lastHovered;
        bool pinching = false;
        void Hover(Square hovered, PointerEventData ped)
        {
            hoveredPointerIds.Add(ped.pointerId);
            // print("hover " + ped.pointerId + " " + hoveredPointerIds.Count);
            if (hoveredPointerIds.Count == 1) // if one touch or click
            {
                // print(ped.pointerId);
                if (draggingLeftClick || ped.pointerId == 0)
                    SquareDraggedEvent.Invoke(hovered);
                else if (draggingRightClick)
                    SquarePinched2Event.Invoke(hovered);
            }
            else if (hoveredPointerIds.Count >= 2) // if pinch
            {
                if (pinching)
                {
                    if (ped.pointerId == 0)
                        SquarePinched1Event.Invoke(hovered);
                    else if (ped.pointerId == 1)
                        SquarePinched2Event.Invoke(hovered);
                }
                else
                {
                    SquarePinched1Event.Invoke(lastHovered);
                    SquarePinched2Event.Invoke(hovered);
                    pinching = true;
                }
            }
            lastHovered = hovered;
        }
        void Unhover(Square sq, PointerEventData ped)
        {
            hoveredPointerIds.Remove(ped.pointerId);
            if (hoveredPointerIds.Count < 2)
            {
                pinching = false;
                // print("unhover " + ped.pointerId + " " + hoveredPointerIds.Count);
            }
        }
        // Square hovered=null, dragged=null;

        bool draggingLeftClick=false, draggingRightClick=false;
        private void Update()
        {
            // if left-click is pressed
            if (Input.GetMouseButtonDown(0))
                draggingLeftClick = true;
            else if (Input.GetMouseButtonUp(0))
                draggingLeftClick = false;

            // if it's the frame right-click is pressed
            if (Input.GetMouseButtonDown(1))
            {
                if (lastHovered != null)
                {
                    draggingRightClick = true;
                    SquarePinched1Event.Invoke(lastHovered);
                    SquarePinched2Event.Invoke(lastHovered);
                }
            }
            else if (Input.GetMouseButtonUp(1))
                draggingRightClick = false;


            // if (hovered != null && hovered != dragged) // if drag has moved
            // {
            //     if (draggingLeftClick == true)
            //     {
            //         SquareDraggedEvent.Invoke(hovered);
            //         dragged = hovered;
            //     }
            //     if (draggingRightClick == true)
            //     {
            //         SquarePinched2Event.Invoke(hovered);
            //         dragged = hovered;
            //     }
            // }
        }
    }        
}