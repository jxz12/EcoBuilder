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
        public event Action OnPiecePlaced;
        public event Action<int> OnPieceSelected;
        public event Action<int, int> OnEdgeAdded;
        public event Action<int, int> OnEdgeRemoved;

        [SerializeField] Board board;
        void Start()
        {
            board.OnNicheChanged += ()=> UpdateResources();
        }

        [SerializeField] Piece piecePrefab;
        [SerializeField] Mesh squareMesh, circleMesh;

        Dictionary<int, Piece> pieces = new Dictionary<int, Piece>();
        Piece inspected;
        HashSet<Piece> inspectedConsumers = new HashSet<Piece>();
        HashSet<Piece> inspectedResources = new HashSet<Piece>();
        public void AddNewPiece(int idx)
        {
            Piece newPiece = Instantiate(piecePrefab);
            newPiece.Init(idx);
            newPiece.OnPosChanged += ()=> UpdateConsumers();
            newPiece.OnSelected +=   ()=> OnPieceSelected(newPiece.Idx);

            // newPiece.Col = new Color(UnityEngine.Random.Range(0,1f), UnityEngine.Random.Range(0,1f), UnityEngine.Random.Range(0,1f));

            board.AddNewPiece(newPiece);
            pieces[idx] = newPiece;
            inspected = newPiece;
            InitResCon();
        }
        public void RemovePiece(int idx)
        {
            // TODOOO
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
        public void ColourPieceUV(float u, float v)
        {
            // TODO: colour according to YUV
            inspected.Col = new Color(u, v, .1f);
        }

        private void InitResCon()
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
        private void UpdateResources()
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
        private void UpdateConsumers()
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

        // public Tuple<int, int> GetCurrentNichePos()
        // {
        //     return 
        // }



































    }
}