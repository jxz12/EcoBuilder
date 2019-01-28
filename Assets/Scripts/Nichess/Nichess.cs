using System;
using System.Collections;
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
        [SerializeField] UnityEvent OnPlacementReady;
        [SerializeField] IntEvent OnPieceSelected;
        [SerializeField] IntEvent OnPieceRemoved;
        [SerializeField] IntIntEvent OnEdgeAdded;
        [SerializeField] IntIntEvent OnEdgeRemoved;
        [SerializeField] IntColorEvent OnPieceColored;

        [SerializeField] Piece piecePrefab;
        [SerializeField] Mesh squareMesh, circleMesh;

        [SerializeField] Board board;

        Dictionary<int, Piece> pieces = new Dictionary<int, Piece>();
        
        private void Start()
        {
            board.OnSquareSelected += s=> {
                selectedPiece = null;
                OnPlacementReady.Invoke();
            };
            board.OnPieceSelected += p=> {
                SelectPiece(p);
                InitSelectedResCon();
                OnPieceSelected.Invoke(p.Idx);
            };

            board.OnPieceNicheRangeChanged += p=> {
                UpdateSelectedResources();
            };
        }

        public void AddPiece(int idx)
        {
            Piece newPiece = Instantiate(piecePrefab);
            newPiece.Init(idx);
            pieces[newPiece.Idx] = newPiece;

            board.PlaceNewPiece(newPiece);
            SelectPiece(newPiece);
            FixPiecePos(idx);

            StartCoroutine(WaitThenInitPiece(newPiece));
        }
        // necessary because event system means that other vertices may not be initialised
        IEnumerator WaitThenInitPiece(Piece toInit)
        {
            yield return null;
            UpdateSelectedConsumers();
            OnPieceColored.Invoke(toInit.Idx, toInit.Col);
            toInit.OnPosChanged += ()=> UpdateSelectedConsumers();
            toInit.OnPosChanged += ()=> OnPieceColored.Invoke(toInit.Idx, toInit.Col);
            toInit.OnThrownAway += ()=> RemovePiece(toInit.Idx);
            toInit.OnThrownAway += ()=> OnPieceRemoved.Invoke(toInit.Idx);

            OnPieceSelected.Invoke(toInit.Idx);
        }
        public void RemovePiece(int idx)
        {
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

        public void FixPiecePos(int idx) { pieces[idx].StaticPos = true; }
        public void FixPieceRange(int idx) { pieces[idx].StaticRange = true; }
        // public void SetPieceNichePos(int idx, int x, int y);
        // public void SetPieceNicheRange(int idx, int x, int y);

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
        private void InitSelectedResCon()
        {
            foreach (Piece p in pieces.Values)
            {
                if (p.IsResourceOf(selectedPiece))
                    selectedResources.Add(p);
                if (selectedPiece.IsResourceOf(p))
                    selectedConsumers.Add(p);
            }
        }
        private void UpdateSelectedResources()
        {
            foreach (Piece res in pieces.Values)
            {
                if (res.IsResourceOf(selectedPiece))
                {
                    if (!selectedResources.Contains(res))
                    {
                        selectedResources.Add(res);
                        OnEdgeAdded.Invoke(res.Idx, selectedPiece.Idx);
                    }
                } 
                else
                {
                    if (selectedResources.Contains(res))
                    {
                        selectedResources.Remove(res);
                        OnEdgeRemoved.Invoke(res.Idx, selectedPiece.Idx);
                    }
                }
            }
        }
        private void UpdateSelectedConsumers()
        {
            foreach (Piece con in pieces.Values)
            {
                if (selectedPiece.IsResourceOf(con))
                {
                    if (!selectedConsumers.Contains(con))
                    {
                        selectedConsumers.Add(con);
                        OnEdgeAdded.Invoke(selectedPiece.Idx, con.Idx);
                    }
                } 
                else
                {
                    if (selectedConsumers.Contains(con))
                    {
                        selectedConsumers.Remove(con);
                        OnEdgeRemoved.Invoke(selectedPiece.Idx, con.Idx);
                    }
                }
            }
        }
    }
}