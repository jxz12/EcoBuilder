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
        newPiece.ClickedEvent += () => PieceInspectedEvent.Invoke(newPiece.Idx);
        newPiece.DragStartedEvent += () => PieceInspectedEvent.Invoke(newPiece.Idx);
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
    
    private void Start()
    {
        PieceInspectedEvent.AddListener(InspectPiece);
        board.SquareDraggedEvent += MoveInspectedPos;
        board.SquarePinched1Event += MoveInspectedNicheStart;
        board.SquarePinched2Event += MoveInspectedNicheEnd;
    }

    private Piece inspected;
    private HashSet<Piece> inspectedConsumers=new HashSet<Piece>(), inspectedResources=new HashSet<Piece>();
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
            spawnPlatform.Despawn();
        }
    }
    void UpdateInspectedConsumers()
    {
        foreach (Piece p in pieces.Values)
        {
            if (p != inspected) // cannot eat itself
            {
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
            }
        }
    }
    void UpdateInspectedResources()
    {
        foreach (Piece p in pieces.Values)
        {
            if (p != inspected) // cannot eat itself
            {
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
            }
        }
    }
    void MoveInspectedPos(Square newPos)
    {
        if (inspected != null && inspected.Dragging)
        {
            if (newPos.transform.childCount == 0)
            {
                inspected.ParentToSquare(newPos);
                UpdateInspectedConsumers();
            }
            if (spawnPlatform.Active)
                spawnPlatform.Despawn();
        }
    }
    void MoveInspectedNicheStart(Square newStart)
    {
        if (inspected != null)
        {
            inspected.NicheStart = newStart;
            UpdateInspectedResources();
        }
    }
    void MoveInspectedNicheEnd(Square newEnd)
    {
        if (inspected != null)
        {
            inspected.NicheEnd = newEnd;
            UpdateInspectedResources();
        }
    }

}
