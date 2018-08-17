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
        PieceInspectedEvent.AddListener(InspectPiece);
        board.SquareDragStartedEvent += MoveInspectedNicheStart;
        board.SquareDraggedEvent += MoveInspectedNicheEnd;
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
        // PieceInspectedEvent.Invoke(newPiece.Idx);
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
    private HashSet<Piece> inspectedConsumers, inspectedResources;
    public void InspectPiece(int idx)
    {
        if (inspected != null) {
            inspected.transform.localScale = Vector3.one;
        }
        inspected = pieces[idx]; // TODO: highlight other things here, use hashsets to calculate edge changes
        inspected.transform.localScale = 1.5f * Vector3.one;
    }
    public void Uninspect()
    {
        if (inspected != null)
        {
            inspected.transform.localScale = Vector3.one;
            inspected = null;
        }
    }
    public void MoveInspectedNicheStart(Square newStart)
    {
        if (inspected != null)
        {
            inspected.NicheStart = newStart;
            inspected.DrawLasso();
        }
    }
    public void MoveInspectedNicheEnd(Square newEnd)
    {
        if (inspected != null)
        {
            inspected.NicheEnd = newEnd;
            inspected.DrawLasso();
        }
    }

}
