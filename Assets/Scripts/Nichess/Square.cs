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
        [SerializeField] Marker markerPrefab;
        [SerializeField] Transform markersParent;
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
            markerPoolPrefab = markerPrefab; // this is ugly but whatever
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

        // pool markers because they will get added and removed often
        private static Marker markerPoolPrefab;
        private static Stack<Marker> markerPool = new Stack<Marker>();
        private static int poolNum = 10;
        private static Transform pooledParent;
        private static void PoolMarkers(int numMarkers)
        {
            if (pooledParent == null)
            {
                pooledParent = new GameObject().transform;
                pooledParent.name = "Marker Pool";
            }

            for (int i=0; i<numMarkers; i++)
            {
                var newMarker = Instantiate(markerPoolPrefab);
                newMarker.gameObject.SetActive(false);
                newMarker.transform.SetParent(pooledParent);
                markerPool.Push(newMarker);
            }
        }
        private static Marker GetMarker(Transform newParent)
        {
            if (markerPool.Count == 0)
                PoolMarkers(poolNum);

            var newMarker = markerPool.Pop();
            newMarker.gameObject.SetActive(true);
            newMarker.transform.parent = newParent;
            newMarker.transform.localPosition = Vector3.zero;
            newMarker.transform.localRotation = Quaternion.identity;
            return newMarker;
        }
        private static void ReturnMarker(Marker oldMarker)
        {
            oldMarker.gameObject.SetActive(false);
            oldMarker.transform.SetParent(pooledParent);
            markerPool.Push(oldMarker);
        }

        // private SortedDictionary<int, Marker> markers = new SortedDictionary<int, Marker>();
        private LinkedList<Marker> markersLL = new LinkedList<Marker>();
        // need to go through once to resize anyway, so use a LL
        public int NumMarkers { get { return markersLL.Count; } }

        public void AddConsumer(Piece con)
        {
            var newMarker = GetMarker(markersParent);
            con.OnPosChanged += ()=> newMarker.Col = con.Col;
            con.OnThrownAway += ()=> RemoveConsumer(con);
            newMarker.Col = con.Col;
            newMarker.ConIdx = con.Idx;

            int i = 1, oldCount = markersLL.Count;
            var head = markersLL.First;

            // traverse the list and add the new marker at the right place
            while (head != null)
            {
                if (newMarker.ConIdx < head.Value.ConIdx)
                {
                    newMarker.Size = newMarker.RenderOrder = i;
                    markersLL.AddBefore(head, newMarker);
                    i += 1;
                }
                else {
                    head.Value.Size = head.Value.RenderOrder = i;
                    i += 1;
                    head = head.Next;
                }
            }
            if (markersLL.Count == oldCount)
            {
                newMarker.Size = newMarker.RenderOrder = i;
                markersLL.AddLast(newMarker);
            }

        }
        public void RemoveConsumer(Piece con)
        {
            int i = 1, oldCount = markersLL.Count;
            var head = markersLL.First;

            // traverse the list and remove the old marker from its place
            while (head != null)
            {
                if (head.Value.ConIdx == con.Idx)
                {
                    var oldMarker = head.Value;
                    con.OnPosChanged -= ()=> oldMarker.Col = con.Col;
                    con.OnThrownAway -= ()=> RemoveConsumer(con);
                    ReturnMarker(oldMarker);

                    var nodeToRemove = head;
                    head = head.Next;
                    markersLL.Remove(nodeToRemove);
                }
                else
                {
                    head.Value.Size = head.Value.RenderOrder = i;
                    i += 1;
                    head = head.Next;
                }
            }
            if (markersLL.Count == oldCount)
            {
                throw new Exception("no marker matches " + con.Idx);
            }
        }
        public void ResizeMarkers(float size)
        {
            markersParent.transform.localScale = new Vector3(size, size, size);
        }

        /////////////////////////////////////////////////////////
        // TODO: make this deal with two touches

        public event Action OnClicked;
        public event Action OnDragStarted;
        public event Action OnDraggedInto;
        public event Action OnDragEnded;
        public event Action OnDroppedOn;
        public void OnPointerEnter(PointerEventData ped)
        {
            var c = Col;
            c.a = hoverAlpha;
            Col = c;

            if (ped.dragging)
                OnDraggedInto();
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
                OnClicked();
            }
        }
        public void OnBeginDrag(PointerEventData ped)
        {
            OnDragStarted();
        }
        public void OnEndDrag(PointerEventData ped)
        {
            // so that you can drop on yourself
            if (ped.pointerEnter == this.gameObject)
                OnDroppedOn();
            else
                OnDragEnded();
        }
        public void OnDrag(PointerEventData ped)
        {
        }
        public void OnDrop(PointerEventData ped)
        {
            OnDroppedOn();
        }
    }
}