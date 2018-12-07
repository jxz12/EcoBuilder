using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace EcoBuilder.Nichess
{
    public class Nichess : MonoBehaviour
    {
        [Serializable] class IntIntEvent : UnityEvent<int, int> { }
        [Serializable] class IntEvent : UnityEvent<int> { }
        [Serializable] class IntColorEvent : UnityEvent<int, Color> { }
        [SerializeField] IntIntEvent EdgeAddedEvent;
        [SerializeField] IntIntEvent EdgeRemovedEvent;
        [SerializeField] IntEvent PieceClickedEvent;
        [SerializeField] IntEvent PieceRemovedEvent;
        [SerializeField] IntColorEvent PieceColoredEvent;

        [SerializeField] Piece piecePrefab;
        [SerializeField] Mesh squareMesh, squareOutlineMesh, circleMesh, circleOutlineMesh;
        Dictionary<int, Piece> pieces = new Dictionary<int, Piece>();

        [SerializeField] Board board;
        [SerializeField] SpawnPlatform spawnPlatform;

        public void AddPiece(int idx)
        {
            Piece newPiece = Instantiate(piecePrefab);
            newPiece.Init(idx, 0);

            newPiece.ClickedEvent += () => PieceClickedEvent.Invoke(newPiece.Idx);
            // newPiece.DragStartedEvent += () => PieceClickedEvent.Invoke(newPiece.Idx);
            newPiece.ColoredEvent += () => PieceColoredEvent.Invoke(newPiece.Idx, newPiece.Col);
            pieces[newPiece.Idx] = newPiece;
            spawnPlatform.Spawn(newPiece);
        }
        public void ShapePieceIntoSquare(int idx)
        {
            pieces[idx].SetShape(squareMesh, squareOutlineMesh);
        }
        public void ShapePieceIntoCircle(int idx)
        {
            pieces[idx].SetShape(circleMesh, circleOutlineMesh);
        }
        public void SetDarkness(int idx, float darkness)
        {
            float pieceLightness = .2f + .7f*(1-darkness); // make sure that the color is not too light or dark
            pieces[idx].Lightness = pieceLightness;
        }

        public void RemovePiece(int idx)
        {
            if (inspected != null && inspected.Idx == idx)
                Uninspect();

            Destroy(pieces[idx].gameObject);
            pieces.Remove(idx);
        }

        public void FixPiecePos(int idx) { pieces[idx].StaticPos = true; }
        public void FixPieceRange(int idx) { pieces[idx].StaticRange = true; }
        
        private void Start()
        {
            // PieceInspectedEvent.AddListener(InspectPiece);
            board.SquareDraggedEvent += MoveInspectedPos;
            board.SquarePinched1Event += MoveInspectedNicheStart;
            board.SquarePinched2Event += MoveInspectedNicheEnd;
            // TODO: instead of a lasso action, drag the nearest edge like before
            //       this lets us move the entire lasso at once too

        }
        private void Update()
        {
            if (Input.GetButtonDown("Jump") && inspected != null)
                PieceRemovedEvent.Invoke(inspected.Idx);
        }

        private Piece inspected;
        private HashSet<Piece> inspectedConsumers=new HashSet<Piece>();
        private HashSet<Piece> inspectedResources=new HashSet<Piece>();
        public void InspectPiece(int idx)
        {
            if (inspected == pieces[idx])
                return;
            if (inspected != null)
                inspected.Uninspect();

            inspected = pieces[idx];
            inspected.Inspect();

            inspectedConsumers.Clear();
            inspectedResources.Clear();
            foreach (Piece p in pieces.Values)
            {
                if (p != inspected) // cannot eat itself
                {
                    if (p.IsResourceOf(inspected))
                        inspectedResources.Add(p);

                    if (inspected.IsResourceOf(p))
                        inspectedConsumers.Add(p);
                }
            }
        }
        public void Uninspect()
        {
            if (inspected != null)
            {
                inspected.Uninspect();
                inspected = null;
            }
        }
        void UpdateInspectedConsumers()
        {
            foreach (Piece p in pieces.Values)
            {
                // if (p != inspected) // cannot eat itself
                // {
                    if (inspected.IsResourceOf(p))
                    {
                        if (!inspectedConsumers.Contains(p))
                        {
                            inspectedConsumers.Add(p);
                            EdgeAddedEvent.Invoke(inspected.Idx, p.Idx);
                        }
                    } 
                    else
                    {
                        if (inspectedConsumers.Contains(p))
                        {
                            inspectedConsumers.Remove(p);
                            EdgeRemovedEvent.Invoke(inspected.Idx, p.Idx);
                        }
                    }
                // }
            }
        }
        void UpdateInspectedResources()
        {
            foreach (Piece p in pieces.Values)
            {
                // if (p != inspected) // cannot eat itself
                // {
                    if (p.IsResourceOf(inspected))
                    {
                        if (!inspectedResources.Contains(p))
                        {
                            inspectedResources.Add(p);
                            EdgeAddedEvent.Invoke(p.Idx, inspected.Idx);
                        }
                    } 
                    else
                    {
                        if (inspectedResources.Contains(p))
                        {
                            inspectedResources.Remove(p);
                            EdgeRemovedEvent.Invoke(p.Idx, inspected.Idx);
                        }
                    }
                // }
            }
        }
        void MoveInspectedPos(Square newSquare)
        {
            if (inspected != null && inspected.Dragging && !inspected.StaticPos)
            {
                if (newSquare.transform.childCount == 0)
                {
                    Square oldSquare = inspected.NichePos;
                    inspected.ParentToSquare(newSquare);
                    if (inspected.IsResourceOf(inspected)) // cannot eat itself
                    {
                        inspected.ParentToSquare(oldSquare);
                    }
                    else
                    {
                        UpdateInspectedConsumers();
                        if (spawnPlatform.Active)
                            spawnPlatform.Despawn();
                    }
                }
            }
        }
        // TODO: THIS DOESN'T WORK WITH RIGHT CLICK STARTING THE LASSO
        void MoveInspectedNicheStart(Square newStart)
        {
            if (inspected != null && !inspected.StaticRange)
            {
                Square oldStart = inspected.NicheStart;
                inspected.NicheStart = newStart;
                if (inspected.IsResourceOf(inspected))
                {
                    inspected.NicheStart = oldStart; // cannot eat itself
                }
                else
                    UpdateInspectedResources();
            }
        }
        void MoveInspectedNicheEnd(Square newEnd)
        {
            if (inspected != null && !inspected.StaticRange)
            {
                Square oldEnd = inspected.NicheEnd;
                inspected.NicheEnd = newEnd;
                if (inspected.IsResourceOf(inspected))
                {
                    inspected.NicheEnd = oldEnd; // cannot eat itself
                }
                else
                    UpdateInspectedResources();
            }
        }

        public void ConfigFromString(string config)
        {
            throw new NotImplementedException();
        }
        public string GetConfigString()
        {
            throw new NotImplementedException();
        }



    }
}