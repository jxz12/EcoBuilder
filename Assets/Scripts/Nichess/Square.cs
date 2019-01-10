using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EcoBuilder.Nichess
{
    [RequireComponent(typeof(MeshRenderer))]
    public class Square : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
        IDragHandler, IBeginDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField] float defaultAlpha=.2f, hoverAlpha=1f;
        MeshRenderer mr;

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
            // defaultAlpha = mr.material.color.a;
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
        public void Select()
        {
            var pos = transform.localPosition;
            pos.x += .1f;
            transform.localPosition = pos;
        }
        public void Deselect()
        {
            var pos = transform.localPosition;
            pos.x -= .1f;
            transform.localPosition = pos;
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
            // UnhoveredEvent();
        }
        public void OnPointerClick(PointerEventData ped)
        {
            ClickedEvent();
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