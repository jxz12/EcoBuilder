using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Nichess : MonoBehaviour
{
    [Serializable] public class EdgeEvent : UnityEvent<int, int> { }
    [SerializeField] EdgeEvent edgeAddEvent;
    [SerializeField] EdgeEvent edgeRemoveEvent;

    [Serializable] public class IntEvent : UnityEvent<int> { }
    [SerializeField] IntEvent InspectEvent;

    [SerializeField] Board board;
    [SerializeField] Piece piecePrefab;
    [SerializeField] Piece basalPiecePrefab;
    [SerializeField] Lasso lasso;
    Dictionary<int, Piece> pieces = new Dictionary<int, Piece>();

    private void Start()
    {
        //board.Init(GameManager.Instance.boardSize, (x,y)=>ColorHelper.HSLSquare(x,y));
        board.Init(GameManager.Instance.BoardSize, (x,y)=>ColorHelper.HSVSquare(x,y,.8f));
    }

    public void AddPiece(int idx)
    {
        Piece newPiece;
        if (SpeciesManager.Instance.GetIsBasal(idx))
            newPiece = Instantiate(basalPiecePrefab);
        else
            newPiece = Instantiate(piecePrefab);

        //bool staticPos = SpeciesManager.Instance.GetIsStaticPos(idx);
        //bool staticRange = SpeciesManager.Instance.GetIsStaticRange(idx);
        
        newPiece.Init(idx, false, SpeciesManager.Instance.GetIsBasal(idx), InspectEvent);

        board.PlaceNewPiece(newPiece);
        pieces[idx] = newPiece;
        InspectEvent.Invoke(idx);
    }
    public void RemovePiece(int idx)
    {
        Destroy(pieces[idx].gameObject);
        pieces.Remove(idx);
    }
    public void MovePiece(int idx, int xPos, int yPos)
    {

    }
    public void MovePiece(int idx, int xPos, int yPos, int xRangeStart, int yRangeStart, int xRangeEnd, int yRangeEnd)
    {

    }

    public void ColorPiece(int idx, Color c)
    {
        pieces[idx].Col = c;
    }

    private Piece inspected;
    public void InspectPiece(int idx)
    {
        inspected = pieces[idx]; // TODO: highlight other things here
        if (!inspected.StaticRange)
        {
            lasso.Activate(pieces[idx]);
        }
        else
        {
            lasso.Deactivate();
        }
    }
    public void Uninspect()
    {
        lasso.Deactivate();
    }

    //public void CalculateNewEdges()
    //{
    //    inspected.
    //}

}
