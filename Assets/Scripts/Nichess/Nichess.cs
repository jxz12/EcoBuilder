using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Nichess : MonoBehaviour
{
    [Serializable] public class EdgeEvent : UnityEvent<int, int> { }
    [SerializeField] EdgeEvent edgeAddEvent;
    [SerializeField] EdgeEvent edgeRemoveEvent;

    [SerializeField] Board board;
    [SerializeField] Piece piecePrefab;
    [SerializeField] Lasso lasso;
    Dictionary<int, Piece> pieces = new Dictionary<int, Piece>();

    private void Start()
    {
        //board.Init(GameManager.Instance.boardSize, (x,y)=>ColorHelper.HSLSquare(x,y));
        board.Init(GameManager.Instance.BoardSize, (x,y)=>ColorHelper.HSVSquare(x,y,.8f));
    }

    public void AddPiece(int idx)
    {
        Piece newPiece = Instantiate(piecePrefab);
        newPiece.Init(idx);
        board.PlaceNewPiece(newPiece);
        pieces[idx] = newPiece;
    }
    public void RemovePiece(int idx)
    {
        Destroy(pieces[idx].gameObject);
        pieces.Remove(idx);
    }

    public void ColorPiece(int idx, Color c)
    {
        pieces[idx].Col = c;
    }

    private Piece inspected;
    public void InspectPiece(int idx)
    {
        inspected = pieces[idx];
        lasso.Activate(pieces[idx]);
    }
    public void Uninspect()
    {
        lasso.Deactivate();
    }

    //public void CalculateNewEdges()
    //{
    //    inspected.
    //}

    //public void Edge()
    //{
    //    if (pieces.Count > 1)
    //    {
    //        int consumer = UnityEngine.Random.Range(1, pieces.Count);
    //        int resource = UnityEngine.Random.Range(0, consumer);
    //        edgeAddEvent.Invoke(resource, consumer);
    //    }
    //}
}
