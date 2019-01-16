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
        [SerializeField] IntEvent PieceInspectedEvent;
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
            board.SquareSelectedEvent +=
                s=> PlacementReadyEvent.Invoke();
            board.PieceSelectedEvent +=
                p=> PieceInspectedEvent.Invoke(p.Idx);
            board.PieceNichePosChangedEvent +=
                p=> PieceColoredEvent.Invoke(p.Idx, p.Col);
            board.PieceNicheRangeChangedEvent +=
                p=> print(p.Idx + " ranged");
            board.PieceThrownAwayEvent +=
                p=> RemovePiece(p.Idx);
            board.PieceThrownAwayEvent +=
                p=> PieceRemovedEvent.Invoke(p.Idx);
        }

        public void AddPiece(int idx)
        {
            Piece newPiece = Instantiate(piecePrefab);
            newPiece.Init(idx, 0);

            board.PlaceNewPiece(newPiece);
            pieces[newPiece.Idx] = newPiece;
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

        // private HashSet<Piece> inspectedConsumers=new HashSet<Piece>();
        // private HashSet<Piece> inspectedResources=new HashSet<Piece>();
        // public void InspectPiece(int idx)
        // {
        //     if (inspectedPiece != null)
        //     {
        //         if (inspectedPiece.Idx == idx)
        //             return;
        //         inspectedPiece.Uninspect(); // RESET EVERYTHING HERE
        //     }
        //     inspectedPiece = pieces[idx];
        //     inspectedPiece.Inspect();

        //     // inspectedConsumers.Clear();
        //     // inspectedResources.Clear();
        //     // foreach (Piece p in pieces.Values)
        //     // {
        //     //     if (p != inspected) // cannot eat itself
        //     //     {
        //     //         if (p.IsResourceOf(inspected))
        //     //             inspectedResources.Add(p);

        //     //         if (inspected.IsResourceOf(p))
        //     //             inspectedConsumers.Add(p);
        //     //     }
        //     // }
        // }
        public void Uninspect()
        {
            board.Deselect();
        }
        // void UpdateInspectedConsumers()
        // {
        //     foreach (Piece p in pieces.Values)
        //     {
        //         if (inspectedPiece.IsResourceOf(p))
        //         {
        //             if (!inspectedConsumers.Contains(p))
        //             {
        //                 inspectedConsumers.Add(p);
        //                 EdgeAddedEvent.Invoke(inspectedPiece.Idx, p.Idx);
        //             }
        //         } 
        //         else
        //         {
        //             if (inspectedConsumers.Contains(p))
        //             {
        //                 inspectedConsumers.Remove(p);
        //                 EdgeRemovedEvent.Invoke(inspectedPiece.Idx, p.Idx);
        //             }
        //         }
        //     }
        // }
        // void UpdateInspectedResources()
        // {
        //     foreach (Piece p in pieces.Values)
        //     {
        //         if (p.IsResourceOf(inspectedPiece))
        //         {
        //             if (!inspectedResources.Contains(p))
        //             {
        //                 inspectedResources.Add(p);
        //                 EdgeAddedEvent.Invoke(p.Idx, inspectedPiece.Idx);
        //             }
        //         } 
        //         else
        //         {
        //             if (inspectedResources.Contains(p))
        //             {
        //                 inspectedResources.Remove(p);
        //                 EdgeRemovedEvent.Invoke(p.Idx, inspectedPiece.Idx);
        //             }
        //         }
        //     }
        // }
        // void MoveInspectedPos(Square newSquare)
        // {
        //     if (inspectedPiece != null && !inspectedPiece.StaticPos)
        //     {
        //         if (newSquare.transform.childCount == 0)
        //         {
        //             inspectedPiece.NichePos = newSquare;
        //             // UpdateInspectedConsumers();
        //         }
        //     }
        // }
        // void MoveInspectedNicheStart(Square newStart)
        // {
        //     if (inspectedPiece != null && !inspectedPiece.StaticRange)
        //     {
        //         inspectedPiece.NicheStart = newStart;
        //         // UpdateInspectedResources();
        //     }
        // }
        // void MoveInspectedNicheEnd(Square newEnd)
        // {
        //     if (inspectedPiece != null && !inspectedPiece.StaticRange)
        //     {
        //         inspectedPiece.NicheEnd = newEnd;
        //         // UpdateInspectedResources();
        //     }
        // }
    }
}