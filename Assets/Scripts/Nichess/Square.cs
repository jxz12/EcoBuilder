using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EcoBuilder.Nichess
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(BoxCollider))]
    public class Square : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        IDragHandler, IBeginDragHandler, IEndDragHandler, IDropHandler
    {
        // [SerializeField] float defaultAlpha=.2f, hoverAlpha=1f;
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
        }
        public void Select()
        {
            // transform.localPosition += .01f*Vector3.up;
        }
        public void Deselect()
        {
            // transform.localPosition -= .01f*Vector3.up;
        }


        /////////////////////////////////////////////////////////
        // TODO: make this deal with two touches

        public event Action OnEnter;
        public event Action OnExit;
        public event Action OnClicked;
        public event Action OnHeld;
        public event Action OnDragStarted;
        public event Action OnDraggedInto;
        public event Action OnDragEnded;
        public event Action OnDroppedOn;
        public void OnPointerEnter(PointerEventData ped)
        {
            OnEnter();

            if (ped.dragging)
                OnDraggedInto();
        }
        public void OnPointerExit(PointerEventData ped)
        {
            OnExit();
            potentialHold = false; // prevent hold & click if left and reentered
        }
        bool potentialHold = false;
        public void OnPointerDown(PointerEventData ped)
        {
            potentialHold = true;
            StartCoroutine(WaitForHold(1, ped));
        }
        IEnumerator WaitForHold(float seconds, PointerEventData ped)
        {
            float endTime = Time.time + seconds;
            while (Time.time < endTime)
            {
                if (potentialHold == false)
                    yield break;
                else
                    yield return null;
            }
            // potentialHold = false;
            OnHeld();
        }
        public void OnPointerUp(PointerEventData ped)
        {
            if (potentialHold)
            {
                OnClicked();
            }
            potentialHold = false;
        }
        public void OnBeginDrag(PointerEventData ped)
        {
            potentialHold = false;
            OnDragStarted();
        }
        public void OnEndDrag(PointerEventData ped)
        {
            OnDragEnded();
            // // so that you can drop on yourself
            // if (ped.pointerEnter == this.gameObject)
            //     OnDroppedOn();
            // else
            //     OnDragEnded();
        }
        public void OnDrag(PointerEventData ped)
        {
        }
        public void OnDrop(PointerEventData ped)
        {
            OnDroppedOn();
        }




        /////////////////////////////////////
        // markers

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

        public void AddMarker(Piece con)
        {
            if (con == this)
                throw new Exception("cannot eat itself");

            var newMarker = GetMarker(markersParent);
            newMarker.AttachPiece(con);

            int i = 1;
            var head = markersLL.First;
            bool added = false;

            // traverse the list and add the new marker at the right place
            while (head != null)
            {
                if (newMarker.ConIdx < head.Value.ConIdx && added==false)
                {
                    newMarker.Size = i;
                    newMarker.RenderOrder = i;
                    markersLL.AddBefore(head, newMarker);
                    i += 1;
                    added = true;
                }
                head.Value.Size = i;
                head.Value.RenderOrder = i;
                head = head.Next;
                i += 1;
            }
            if (added == false)
            {
                newMarker.Size = i;
                newMarker.RenderOrder = i;
                markersLL.AddLast(newMarker);
            }
        }
        public void RemoveMarker(Piece con)
        {
            if (con == this)
                throw new Exception("cannot eat itself");

            int i = 1;
            var head = markersLL.First;
            bool removed = false;

            // traverse the list and remove the old marker from its place
            while (head != null)
            {
                if (head.Value.ConIdx == con.Idx)
                {
                    if (removed == true)
                        throw new Exception("two of the same marker removed");

                    var nodeToRemove = head;
                    head = head.Next;

                    var oldMarker = nodeToRemove.Value;
                    oldMarker.DetachPiece(con);
                    ReturnMarker(oldMarker);
                    markersLL.Remove(nodeToRemove);
                    removed = true;
                }
                else
                {
                    head.Value.Size = i;
                    head.Value.RenderOrder = i;
                    i += 1;
                    head = head.Next;
                }
            }
            if (removed == false)
            {
                throw new Exception("no marker matches " + con.Idx);
            }
        }
        public void ResizeMarkers(float size)
        {
            markersParent.transform.localScale = new Vector3(size, size, size);
        }
    }
}