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
        [SerializeField] UnityEvent PlacementReadyEvent;
        [SerializeField] IntEvent PieceSelectedEvent;
        [SerializeField] IntEvent PieceRemovedEvent;
        [SerializeField] IntIntEvent EdgeAddedEvent;
        [SerializeField] IntIntEvent EdgeRemovedEvent;
        [SerializeField] IntColorEvent PieceColoredEvent;

        [SerializeField] Piece piecePrefab;
        [SerializeField] Mesh squareMesh, circleMesh;

        [SerializeField] Board board;

        Dictionary<int, Piece> pieces = new Dictionary<int, Piece>();
        
        private void Start()
        {
            board.SquareSelectedEvent += s=> {
                selectedPiece = null;
                PlacementReadyEvent.Invoke();
            };
            board.PieceSelectedEvent += p=> {
                SelectPiece(p);
                PieceSelectedEvent.Invoke(p.Idx);
            };

            board.PieceNichePosChangedEvent += p=> {
                UpdateInspectedConsumers();
                PieceColoredEvent.Invoke(p.Idx, p.Col);
            };
            board.PieceNicheRangeChangedEvent += p=> {
                UpdateInspectedResources();
            };
            board.PieceThrownAwayEvent += p=> {
                RemovePiece(p.Idx);
                PieceRemovedEvent.Invoke(p.Idx);
            };
        }

        public void AddPiece(int idx)
        {
            Piece newPiece = Instantiate(piecePrefab);
            newPiece.Init(idx, 0);

            board.PlaceNewPiece(newPiece);
            pieces[newPiece.Idx] = newPiece;
            SelectPiece(newPiece);
        }
        public void RemovePiece(int idx)
        {
            Destroy(pieces[idx].gameObject);
            pieces.Remove(idx);
        }
        public void ShapePieceIntoSquare(int idx)
        {
            pieces[idx].SetShape(squareMesh);
        }
        public void ShapePieceIntoCircle(int idx)
        {
            pieces[idx].SetShape(circleMesh);
        }
        public void SetPieceLightness(int idx, float lightness)
        {
            float pieceLightness = .2f + .7f*lightness; // make sure that the color is not too light or dark
            pieces[idx].Lightness = pieceLightness;
        }

        // public void FixPiecePos(int idx) { pieces[idx].StaticPos = true; }
        // public void FixPieceRange(int idx) { pieces[idx].StaticRange = true; }

        //////////////////////////////////////////////////////////////

        private Piece selectedPiece;
        private HashSet<Piece> selectedConsumers=new HashSet<Piece>();
        private HashSet<Piece> selectedResources=new HashSet<Piece>();
        private void SelectPiece(Piece toSelect)
        {
            if (selectedPiece == toSelect)
                throw new Exception("piece already inspected");

            selectedPiece = toSelect;

            selectedConsumers.Clear();
            selectedResources.Clear();
            foreach (Piece p in pieces.Values)
            {
                if (p.IsResourceOf(selectedPiece))
                    selectedResources.Add(p);
                if (selectedPiece.IsResourceOf(p))
                    selectedConsumers.Add(p);
            }
        }
        public void SelectPiece(int idx) // for use from outside to affect board
        {
            board.SelectPiece(pieces[idx]);
            SelectPiece(pieces[idx]);
        }
        public void DeselectAll()
        {
            selectedPiece = null;
            board.DeselectAll();
        }
        private void UpdateInspectedConsumers()
        {
            foreach (Piece con in pieces.Values)
            {
                if (selectedPiece.IsResourceOf(con))
                {
                    if (!selectedConsumers.Contains(con))
                    {
                        selectedConsumers.Add(con);
                        EdgeAddedEvent.Invoke(selectedPiece.Idx, con.Idx);
                    }
                } 
                else
                {
                    if (selectedConsumers.Contains(con))
                    {
                        selectedConsumers.Remove(con);
                        EdgeRemovedEvent.Invoke(selectedPiece.Idx, con.Idx);
                    }
                }
            }
        }
        private void UpdateInspectedResources()
        {
            foreach (Piece res in pieces.Values)
            {
                if (res.IsResourceOf(selectedPiece))
                {
                    if (!selectedResources.Contains(res))
                    {
                        selectedResources.Add(res);
                        EdgeAddedEvent.Invoke(res.Idx, selectedPiece.Idx);
                    }
                } 
                else
                {
                    if (selectedResources.Contains(res))
                    {
                        selectedResources.Remove(res);
                        EdgeRemovedEvent.Invoke(res.Idx, selectedPiece.Idx);
                    }
                }
            }
        }
    }
}