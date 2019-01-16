using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EcoBuilder.Nichess
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(BoxCollider))]
    public class Square : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
        IDragHandler, IBeginDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField] float defaultAlpha=.2f, hoverAlpha=1f;
        MeshRenderer mr;
        BoxCollider bc;

        public Color Col {
            get { return mr.material.color; }
            private set { mr.material.color = value; }
        }
        public int X { get; private set; }
        public int Y { get; private set; }
        public Piece Occupant { get; set; } = null;

        void Awake()
        {
            mr = GetComponent<MeshRenderer>();
            bc = GetComponent<BoxCollider>();
        }

        public void Init(int x, int y, Color c, int size, float borderWidth)
        {
            float gap = 1f / size;
            Vector3 offset = new Vector3(-.5f + gap/2, 0, -.5f + gap/2);
            transform.localPosition = new Vector3(x*gap, 0, y*gap) + offset;

            float squareWidth = (1f / size) * (1f-borderWidth);
            transform.localScale = new Vector3(squareWidth, squareWidth, squareWidth);
            bc.size = new Vector3(1f/(1-borderWidth), 0, 1f/(1-borderWidth));

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
        public void Select()
        {
            transform.localPosition += .01f*Vector3.up;
        }
        public void Deselect()
        {
            transform.localPosition -= .01f*Vector3.up;
        }
        private HashSet<Piece> consumers = new HashSet<Piece>();
        public void AddConsumer(Piece p)
        {
            // keep track of how many pieces are eating it, and draw concentric circles 
            if (consumers.Contains(p))
            {
                throw new Exception("already eaten by " + p.name);
            }
            else
            {
                consumers.Add(p);
                mr.transform.localScale *= .8f;
            }
        }
        public void RemoveConsumer(Piece p)
        {
            if (consumers.Contains(p))
            {
                print(name);
                consumers.Remove(p);
                mr.transform.localScale *= 1.25f;
            }
            else
            {
                throw new Exception("not eaten by " + p.name);
            }
        }

        /////////////////////////////////////////////////////////

        public event Action ClickedEvent;
        public event Action DragStartedEvent;
        public event Action DraggedIntoEvent;
        public event Action DragEndedEvent;
        public event Action DroppedOnEvent;
        public void OnPointerEnter(PointerEventData ped)
        {
            var c = Col;
            c.a = hoverAlpha;
            Col = c;

            if (ped.dragging)
                DraggedIntoEvent();
        }
        public void OnPointerExit(PointerEventData ped)
        {
            var c = Col;
            c.a = defaultAlpha;
            Col = c;
        }
        public void OnPointerClick(PointerEventData ped)
        {
            // don't throw a click if this is already being dragged, because EndDrag will be
            if (!ped.dragging || ped.pointerDrag != this.gameObject)
            {
                ClickedEvent();
            }
        }
        public void OnBeginDrag(PointerEventData ped)
        {
            DragStartedEvent();
        }
        public void OnEndDrag(PointerEventData ped)
        {
            DragEndedEvent();
        }
        public void OnDrag(PointerEventData ped)
        {
        }
        public void OnDrop(PointerEventData ped)
        {
            DroppedOnEvent();
        }
    }
}