using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace EcoBuilder.Nichess
{
    public class Nichess : MonoBehaviour
    {
        // nichess handles individual edges, board handles squares and overlap
        public event Action<int, int, int> OnPiecePlaced; // idx, x, y
        public event Action<int, int, int, int, int> OnPieceNiched; // idx, b, l, t, r
        public event Action<int, Color> OnPieceColoured;
        public event Action<int> OnPieceSelected;
        public event Action<int, int> OnEdgeAdded;
        public event Action<int, int> OnEdgeRemoved;

        [SerializeField] Board board;
        void Start()
        {
            board.OnPosChanged +=   (p)=> UpdateConsumers(p);
            board.OnNicheChanged += (p)=> UpdateResources(p);

            board.OnPosChanged +=
                (p)=> OnPiecePlaced.Invoke(p.Idx, p.NichePos.X, p.NichePos.Y);
            board.OnNicheChanged +=
                (p)=> OnPieceNiched.Invoke(p.Idx, p.NicheMin.X, p.NicheMin.Y, p.NicheMax.X, p.NicheMax.Y);
        }

        [SerializeField] Piece piecePrefab;
        [SerializeField] Mesh squareMesh, circleMesh;

        Dictionary<int, Piece> pieces = new Dictionary<int, Piece>();
        public void InspectPiece(int idx)
        {
            board.InspectPiece(pieces[idx]);
            InitResCon(pieces[idx]);
        }
        public void AddPiece(int idx)
        {
            Piece newPiece = Instantiate(piecePrefab);
            newPiece.Init(idx);
            newPiece.OnColoured += (c)=> OnPieceColoured(idx, c);
            newPiece.OnSelected +=  ()=> OnPieceSelected.Invoke(newPiece.Idx);

            board.AddPiece(newPiece);
            pieces[idx] = newPiece;
        }
        public void RemovePiece(int idx)
        {
            board.RemovePiece(pieces[idx]);
            pieces.Remove(idx);
        }
        public void FixPieceRange(int idx) {
            pieces[idx].StaticRange = true;
        }
        public void ShapePieceIntoSquare(int idx)
        {
            pieces[idx].Shape = squareMesh;
        }
        public void ShapePieceIntoCircle(int idx)
        {
            pieces[idx].Shape = circleMesh;
        }
        public void ColourPiece2D(int idx, float x, float y)
        {
            if (x < 0 || x > 1 || y < 0 || y > 1)
                throw new Exception("colour out of bounds");

            pieces[idx].Colour2D(x, y);
        }
        public void PlacePiece(int idx, int x, int y)
        {
            if (x < 0 || y < 0)
                throw new Exception("negative coordinates");

            // TODO: catch errors here
            board.PlacePiece(pieces[idx], x, y);
        }
        public void NichePiece(int idx, int l, int b, int r, int t)
        {
            if (l < 0 || b < 0 || r < 0 || t < 0)
                throw new Exception("negative coordinates");
            // TODO: it's possible for a redo piece to not have a niche

            // TODO: catch errors here too
            board.NichePiece(pieces[idx], l, b, r, t);
        }



        ///////////////////////////////
        // edge add/remove stuff

        HashSet<Piece> inspectedConsumers = new HashSet<Piece>();
        HashSet<Piece> inspectedResources = new HashSet<Piece>();
        private void InitResCon(Piece inspected)
        {
            inspectedConsumers.Clear();
            inspectedResources.Clear();

            foreach (Piece p in pieces.Values)
            {
                if (p.IsResourceOf(inspected))
                    inspectedResources.Add(p);
                if (inspected.IsResourceOf(p))
                    inspectedConsumers.Add(p);
            }
        }
        private void UpdateResources(Piece inspected)
        {
            foreach (Piece res in pieces.Values)
            {
                if (res.IsResourceOf(inspected))
                {
                    if (!inspectedResources.Contains(res))
                    {
                        inspectedResources.Add(res);
                        OnEdgeAdded.Invoke(res.Idx, inspected.Idx);
                    }
                } 
                else
                {
                    if (inspectedResources.Contains(res))
                    {
                        inspectedResources.Remove(res);
                        OnEdgeRemoved.Invoke(res.Idx, inspected.Idx);
                    }
                }
            }
        }
        private void UpdateConsumers(Piece inspected)
        {
            foreach (Piece con in pieces.Values)
            {
                if (inspected.IsResourceOf(con))
                {
                    if (!inspectedConsumers.Contains(con))
                    {
                        inspectedConsumers.Add(con);
                        OnEdgeAdded.Invoke(inspected.Idx, con.Idx);
                    }
                } 
                else
                {
                    if (inspectedConsumers.Contains(con))
                    {
                        inspectedConsumers.Remove(con);
                        OnEdgeRemoved.Invoke(inspected.Idx, con.Idx);
                    }
                }
            }
        }
    }
}