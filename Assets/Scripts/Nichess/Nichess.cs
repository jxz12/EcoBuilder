using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Nichess : MonoBehaviour
{
    [Serializable] class IntIntEvent : UnityEvent<int, int> { }
    [SerializeField] IntIntEvent EdgeAddedEvent;
    [SerializeField] IntIntEvent EdgeRemovedEvent;

    [Serializable] class IntEvent : UnityEvent<int> { }
    [SerializeField] IntEvent InspectedEvent;

    [SerializeField] Board board;
    [SerializeField] Piece producerPrefab;
    [SerializeField] Piece consumerPrefab;
    Dictionary<int, Piece> pieces = new Dictionary<int, Piece>();

    private void Start()
    {
        //board.Init(GameManager.Instance.boardSize, (x,y)=>ColorHelper.HSLSquare(x,y));
    }

    public void AddProducer(int idx)//, bool staticPos)
    {
        Piece newPiece = Instantiate(producerPrefab);
        //newPiece.Init(idx, staticPos, true, InspectedEvent);
        newPiece.Init(idx, false, true, InspectedEvent);

        board.PlaceNewPiece(newPiece);
        pieces[idx] = newPiece;
        InspectedEvent.Invoke(idx);
    }
    public void AddConsumer(int idx)//, bool staticPos, bool staticRange)
    {
        Piece newPiece = Instantiate(consumerPrefab);
        newPiece.Init(idx, false, false, InspectedEvent);

        board.PlaceNewPiece(newPiece);
        pieces[idx] = newPiece;
        InspectedEvent.Invoke(idx); // maybe shouldn't be here
    }
    public void RemovePiece(int idx)
    {
        Destroy(pieces[idx].gameObject);
        pieces.Remove(idx);
        Uninspect();
    }
    public void MovePiece(int idx, int xPos, int yPos)
    {
        throw new NotImplementedException();
    }
    public void MovePiece(int idx, int xPos, int yPos, int xRangeStart, int yRangeStart, int xRangeEnd, int yRangeEnd)
    {
        throw new NotImplementedException();
    }
    public string GetConfigString()
    {
        throw new NotImplementedException();
    }

    //public void ColorPiece(int idx, Color c)
    //{
    //    pieces[idx].Col = c;
    //}

    private Piece inspected;
    public void InspectPiece(int idx)
    {
        inspected = pieces[idx]; // TODO: highlight other things here
    }
    public void Uninspect()
    {
        inspected = null;
    }

    //public void CalculateNewEdges()
    //{
    //    inspected.
    //}

}
