using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Nichess : MonoBehaviour
{
    [Serializable] class IntIntEvent : UnityEvent<int, int> { }
    [Serializable] class IntEvent : UnityEvent<int> { }
    [Serializable] class IntColorEvent : UnityEvent<int, Color> { }
    [SerializeField] IntIntEvent EdgeAddedEvent;
    [SerializeField] IntIntEvent EdgeRemovedEvent;
    [SerializeField] IntEvent PieceInspectedEvent;
    [SerializeField] IntColorEvent PieceColoredEvent;

    [SerializeField] Piece producerPrefab;
    [SerializeField] Piece consumerPrefab;
    Dictionary<int, Piece> pieces = new Dictionary<int, Piece>();

    [SerializeField] Board board;
    [SerializeField] SpawnPlatform spawnPlatform;

    private void Start()
    {
        board.SquareDragStartedEvent += s => print("start " + s.name);
        board.SquareDraggedEvent += s => print("drag " + s.name);
    }

    public void AddProducer(int idx)//, bool staticPos)
    {
        Piece newPiece = Instantiate(producerPrefab);
        newPiece.Init(idx, false, true);
        SetupNewPiece(newPiece);
    }
    public void AddConsumer(int idx)//, bool staticPos, bool staticRange)
    {
        Piece newPiece = Instantiate(consumerPrefab);
        newPiece.Init(idx, false, false);
        SetupNewPiece(newPiece);
    }
    void SetupNewPiece(Piece newPiece)
    {
        newPiece.InspectedEvent += () => PieceInspectedEvent.Invoke(newPiece.Idx);
        newPiece.ColoredEvent += () => PieceColoredEvent.Invoke(newPiece.Idx, newPiece.Col);
        pieces[newPiece.Idx] = newPiece;
        spawnPlatform.Spawn(newPiece);
        PieceInspectedEvent.Invoke(newPiece.Idx); // maybe shouldn't go here
    }

    public void RemovePiece(int idx)
    {
        Destroy(pieces[idx].gameObject);
        pieces.Remove(idx);
        Uninspect();
    }
    public void ConfigFromString(string config)
    {
        throw new NotImplementedException();
    }
    public string GetConfigString()
    {
        throw new NotImplementedException();
    }

    private Piece inspected;
    public void InspectPiece(int idx)
    {
        inspected = pieces[idx]; // TODO: highlight other things here
    }
    public void Uninspect()
    {
        inspected = null;
    }

}
