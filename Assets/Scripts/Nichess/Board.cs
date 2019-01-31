using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EcoBuilder.Nichess
{
    public class Board : MonoBehaviour
    {
        [SerializeField] Square squarePrefab;
        [SerializeField] float defaultYColor=.5f;
        [SerializeField] float YUVRange=.8f;
        [SerializeField] float squareBorder=.05f;
        [SerializeField] LineRenderer lasso;

        [SerializeField] PieceCamera cam;

        Square[,] squares;
        public event Action<Square> OnSquareSelected;
        // public event Action<Piece> OnPieceSelected;
        public event Action<Piece> OnPieceNicheRangeChanged;

        private void Awake()
        {
            int size = GameManager.Instance.BoardSize;
            // Func<float,float,Color> ColorMap = (x,y)=>ColorHelper.HSLSquare(x,y,.5f);
            Func<float,float,Color> ColorMap = (x,y)=>ColorHelper.YUVtoRGBtruncated(defaultYColor,YUVRange*x,YUVRange*y);

            squares = new Square[size, size];

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    var s = Instantiate(squarePrefab, transform);

                    Color c = ColorMap((float)i/(size-1)-.5f, (float)j/(size-1)-.5f);
                    // c = ColorHelper.ApplyGamma(c);
                    s.Init(i, j, c, size, squareBorder);

                    s.OnClicked += ()=> ClickSquare(s);
                    s.OnHeld += ()=> HoldSquare(s);
                    s.OnDragStarted += ()=> DragStartFromSquare(s);
                    s.OnDragEnded += ()=> DragEndFromSquare(s);
                    s.OnDraggedInto += ()=> DragIntoSquare(s);
                    s.OnDroppedOn += ()=> DropOnSquare(s);

                    squares[i, j] = s;
                }
            }
            markerCounts[0] = size*size; // keep track of markers on each square
        }
        private void Start()
        {
            OnSquareSelected += s=> cam.ViewBoard(this);
            OnSquareSelected += s=> EraseLasso();
        }

        private Square selectedSquare = null;
        private Square draggedSquare = null;
        private enum BoardState {
            Idle, SquareSelected, PieceSelected,
            /*EmptyDragging,*/ PieceDragging, NicheDragging
        };
        private BoardState myState = BoardState.Idle;
        private BoardState State {
            get { return myState; }
            set { /*print(value);*/ myState = value; }
        }

        public void PlaceNewPieceOnSelectedSquare(Piece pce)
        {
            if (selectedSquare == null)
                throw new Exception("square not selected first!");
            else
            {
                pce.NichePos = selectedSquare;
                selectedSquare.Occupant = pce;

                pce.OnSelected += ()=> cam.ViewPiece(pce);
                pce.OnSelected += ()=> DrawLasso(pce);
                pce.Select();

                State = BoardState.PieceSelected;
            }
        }

        public void SelectPieceExternal(Piece pce)
        {
            // ClickSquare(p.NichePos); // cannot do this because it will wrongly invoke events
            if (State == BoardState.SquareSelected)
            {
                selectedSquare.Deselect();
            }
            else if (State == BoardState.PieceSelected)
            {
                selectedSquare.Deselect();
                selectedSquare.Occupant.Deselect();
            }
            pce.NichePos.Select();
            pce.Select();
            selectedSquare = pce.NichePos;

            cam.ViewPiece(pce);
            DrawLasso(selectedSquare.Occupant);

            State = BoardState.PieceSelected;
        }

        public void DeselectAll()
        {
            ClickSquare(null);
            cam.ViewBoard(this);
            EraseLasso();
        }

        private void ClickSquare(Square sqr)
        {
            if (sqr != selectedSquare)
            {
                // undo previous stuff first 

                if (State == BoardState.Idle)
                {} // do nothing
                else if (State == BoardState.SquareSelected)
                {
                    selectedSquare.Deselect();
                }
                else if (State == BoardState.PieceSelected)
                {
                    selectedSquare.Deselect();
                    selectedSquare.Occupant.Deselect();
                }

                // do current stuff
                if (sqr != null)
                {
                    sqr.Select();
                    if (sqr.Occupant != null)
                    {
                        sqr.Occupant.Select();
                        State = BoardState.PieceSelected;
                    }
                    else
                    {
                        State = BoardState.SquareSelected;
                        OnSquareSelected(sqr);
                    }
                }
                else
                {
                    State = BoardState.Idle;
                    cam.ViewBoard(this);
                }
                selectedSquare = sqr;
            }
        }
        private void HoldSquare(Square sqr)
        {
            if (sqr.Occupant != null)
            {
                if (sqr.Occupant.StaticPos)
                {
                    print("cannot move! TODO: make this show a message instead");
                }
                else
                {
                    ClickSquare(sqr);
                    sqr.Occupant.Lift();
                    draggedSquare = sqr;
                    State = BoardState.PieceDragging;
                }
            }
        }

        private void DragStartFromSquare(Square sqr)
        {
            if (State == BoardState.Idle || State == BoardState.SquareSelected)
            {
                // do nothing
            }
            else if (State == BoardState.PieceDragging)
            {
                // do nothing, as should already be dragged from the hold
            }
            else if (State == BoardState.PieceSelected)
            {
                // change niche range
                bool updated = UpdateNiche(selectedSquare.Occupant, sqr, sqr);
                if (updated)
                {
                    RescaleMarkersIfNeeded();
                    OnPieceNicheRangeChanged(selectedSquare.Occupant);
                    DrawLasso(selectedSquare.Occupant);
                    State = BoardState.NicheDragging;
                }
            }
            else
            {
                throw new Exception("impossible state " + State);
            }
            draggedSquare = sqr;
        }
        private void DragIntoSquare(Square sqr)
        {
            if (sqr != draggedSquare) // in case pointer leaves board entirely and enters same square
            {
                if (State == BoardState.PieceDragging)
                {
                    if (draggedSquare.Occupant.IsPotentialNewSquare(sqr))
                    {
                        draggedSquare.Occupant.NichePos = sqr;
                        sqr.Occupant = draggedSquare.Occupant;
                        draggedSquare.Occupant = null;
                        draggedSquare = sqr;
                        DrawLasso(draggedSquare.Occupant); // only to change colour
                    }
                    else {} // otherwise keep the previous draggedSquare
                }
                else if (State == BoardState.NicheDragging)
                {
                    bool updated = UpdateNiche(selectedSquare.Occupant, draggedSquare, sqr);
                    if (updated)
                    {
                        RescaleMarkersIfNeeded();
                        OnPieceNicheRangeChanged(selectedSquare.Occupant);
                        DrawLasso(selectedSquare.Occupant);
                        draggedSquare = sqr;
                    }
                    else {} // again, keep previous draggedSquare to make it seem as if it was invisibly being moved
                }
            }
        }
        private void DropOnSquare(Square sqr)
        {
            if (State == BoardState.PieceDragging)
            {
                draggedSquare.Occupant.Drop();
                // we no not want to reselect the piece, so do not call Select()
                selectedSquare.Deselect();
                sqr.Select();
                selectedSquare = sqr;
                State = BoardState.PieceSelected;
            }
            else if (State == BoardState.NicheDragging)
            {
                State = BoardState.PieceSelected;
            }
            else
            {
                ClickSquare(sqr);
            }
            draggedSquare = null;
        }
        // always happens after drop
        private void DragEndFromSquare(Square sqr)
        {
            if (draggedSquare != null) // if not dropped on a square
            {
                if (State == BoardState.PieceDragging)
                {
                    // the following is for deletion
                    ///////////////
                    // cam.ViewBoard(this);
                    // RemoveNiche(draggedSquare.Occupant);
                    // draggedSquare.Occupant.ThrowAway();
                    // draggedSquare.Occupant = null;
                    // selectedSquare.Deselect();
                    // selectedSquare = null;

                    // EraseLasso();
                    // State = BoardState.Idle;
                    ///////////////

                    // return to original position
                    selectedSquare.Occupant = draggedSquare.Occupant;
                    selectedSquare.Occupant.NichePos = selectedSquare;
                    selectedSquare.Occupant.Drop();
                    DrawLasso(selectedSquare.Occupant);
                    draggedSquare.Occupant = null;

                    State = BoardState.PieceSelected;
                }
                else if (State == BoardState.NicheDragging)
                {
                    State = BoardState.PieceSelected; // make sure state is reset
                }
                else
                {
                    ClickSquare(null);
                }
                draggedSquare = null;
            }

        }

        ///////////////////////////////////////
        // Niche stuff below

        private SortedDictionary<int, int> markerCounts = new SortedDictionary<int,int>();
        private void AddMarkerCount(int n)
        {
            if (markerCounts.ContainsKey(-n))
                markerCounts[-n] += 1;
            else
                markerCounts[-n] = 1;

        }
        private void RemoveMarkerCount(int n)
        {
            if (markerCounts.ContainsKey(-n))
            {
                markerCounts[-n] -= 1;
                if (markerCounts[-n] == 0)
                    markerCounts.Remove(-n);
            }
            else
            {
                throw new Exception("does not have key " + n);
            }
        }
        private void AddConsumerToSquare(Square sqr, Piece con)
        {
            RemoveMarkerCount(sqr.NumMarkers);
            sqr.AddConsumer(con);
            AddMarkerCount(sqr.NumMarkers);
        }
        private void RemoveConsumerFromSquare(Square sqr, Piece con)
        {
            RemoveMarkerCount(sqr.NumMarkers);
            sqr.RemoveConsumer(con);
            AddMarkerCount(sqr.NumMarkers);
        }
        [SerializeField] float maxMarkerSize = .9f;
        private int prevMaxNumMarkers=0;
        private void RescaleMarkersIfNeeded()
        {
            int newMaxNumMarkers = markerCounts.Keys.First();//OrDefault();
            if (newMaxNumMarkers != prevMaxNumMarkers && newMaxNumMarkers != 0)
            {
                // minus because we are storing negative in order to access largest first
                float newSize = maxMarkerSize / -newMaxNumMarkers;
                foreach (Square s in squares)
                {
                    s.ResizeMarkers(newSize);
                }
                prevMaxNumMarkers = newMaxNumMarkers;
            }
        }

        private bool UpdateNiche(Piece toUpdate, Square from, Square to)
        {
            if (toUpdate.StaticRange)
            {
                return false;
            }
            else if (toUpdate.NicheMin == null || toUpdate.NicheMax == null) // new piece
            {
                if (from != to)
                    throw new Exception("impossible niche update");

                if (from == toUpdate.NichePos)
                {
                    return false;
                }
                else
                {
                    toUpdate.NicheMin = toUpdate.NicheMax = from;
                    AddConsumerToSquare(from, toUpdate);
                    return true;
                }
            }
            else
            {
                // 0 is old Niche Range, l=left, r=right, b=bottom, t=top
                int l0=toUpdate.NicheMin.X, r0=toUpdate.NicheMax.X;
                int b0=toUpdate.NicheMin.Y, t0=toUpdate.NicheMax.Y;

                if (from == to) // if it's the start of the drag
                {
                    if (from == toUpdate.NichePos)
                    {
                        return false;
                    }
                    else if (
                        ((l0<=from.X && from.X<=r0) && (from.Y==b0 || from.Y==t0)) ||
                        ((b0<=from.Y && from.Y<=t0) && (from.X==l0 || from.X==r0))
                    ) {
                        return true; // return dragged, but don't do anything else
                    }
                    else
                    {
                        for (int i=l0; i<=r0; i++)
                        {
                            for (int j=b0; j<=t0; j++)
                            {
                                RemoveConsumerFromSquare(squares[i,j], toUpdate);
                            }
                        }
                        toUpdate.NicheMin = toUpdate.NicheMax = from;
                        AddConsumerToSquare(from, toUpdate);
                        return true;
                    }
                }
                else
                {
                    int xFrom = from.X, yFrom = from.Y;
                    int xTo = to.X, yTo = to.Y;

                    // 1 is new Niche Range
                    int l1 = l0, r1 = r0;
                    int b1 = b0, t1 = t0;
                    if (xFrom==l0 && yFrom==b0) // bottom left
                    {
                        l1 = xTo;
                        b1 = yTo;
                    }
                    else if (xFrom==l0 && yFrom==t0) // top left
                    {
                        l1 = xTo;
                        t1 = yTo;
                    }
                    else if (xFrom==r0 && yFrom==t0) // top right
                    {
                        r1 = xTo;
                        t1 = yTo;
                    }
                    else if (xFrom==r0 && yFrom==b0) // bottom right
                    {
                        r1 = xTo;
                        b1 = yTo;
                    }
                    else if (xFrom==l0 && (b0<yFrom && yFrom<t0)) // left
                    {
                        l1 = xTo;
                    } 
                    else if ((l0<xFrom && xFrom<r0) && yFrom==t0) // top
                    {
                        t1 = yTo;
                    }
                    else if (xFrom==r0 && (t0>yFrom && yFrom>b0)) // right
                    {
                        r1 = xTo;
                    }
                    else if ((l0<xFrom && xFrom<r0) && yFrom==b0) // bottom
                    {
                        b1 = yTo;
                    }
                    else
                    {
                        throw new Exception("impossible drag");
                    }

                    // flip niche start or end back around to make calculation simpler
                    if (l1 > r1) { int temp = l1; l1 = r1; r1 = temp; }
                    if (b1 > t1) { int temp = b1; b1 = t1; t1 = temp; }

                    if (l1<=toUpdate.NichePos.X && toUpdate.NichePos.X<=r1 &&
                        b1<=toUpdate.NichePos.Y && toUpdate.NichePos.Y<=t1
                    ) {
                        // cannot eat itself
                        return false;
                    }

                    toUpdate.NicheMin = squares[l1, b1];
                    toUpdate.NicheMax = squares[r1, t1];

                    //////////////////////////////////////////////////////////////
                    // make the following better? complexity isn't great, but in practice should be okay
                     
                    // 4 and 5 are the intersect
                    int l2 = Math.Max(l0, l1);
                    int r2 = Math.Min(r0, r1);
                    int b2 = Math.Max(b0, b1);
                    int t2 = Math.Min(t0, t1);

                    // clear vacated squares
                    for (int i=l0; i<=r0; i++)
                    {
                        for (int j=b0; j<=t0; j++)
                        {
                            if (!(l2<=i && i<=r2 && b2<=j && j<=t2)) // if not in intersect
                            {
                                RemoveConsumerFromSquare(squares[i,j], toUpdate);
                            }
                        }
                    }                  
                    // fill new squares
                    for (int i=l1; i<=r1; i++)
                    {
                        for (int j=b1; j<=t1; j++)
                        {
                            if (!(l2<=i && i<=r2 && b2<=j && j<=t2)) // if not in intersect
                            {
                                AddConsumerToSquare(squares[i,j], toUpdate);
                            }
                        }
                    }
                    return true;
                }
            }
        }
        private void RemoveNiche(Piece toRemove)
        {
            if (toRemove.NicheMin == null || toRemove.NicheMax == null)
            {
                return;
            }
            else
            {
                int l = toRemove.NicheMin.X, r = toRemove.NicheMax.X;
                int b = toRemove.NicheMin.Y, t = toRemove.NicheMax.Y;

                for (int i=l; i<=r; i++)
                {
                    for (int j=b; j<=t; j++)
                    {
                        RemoveConsumerFromSquare(squares[i,j], toRemove);
                    }
                }
            }
        }
        private void DrawLasso(Piece toDraw)
        {
            if (toDraw.NicheMin == null || toDraw.NicheMax == null)
            {
                EraseLasso();
            }
            else
            {
                lasso.enabled = true;
                lasso.material.color = toDraw.Col + new Color(.1f,.1f,.1f);
                int l = toDraw.NicheMin.X, r = toDraw.NicheMax.X;
                int b = toDraw.NicheMin.Y, t = toDraw.NicheMax.Y;

                lasso.SetPosition(0, squares[l,b].transform.TransformPoint(new Vector3(-.5f, 0, -.5f)));
                lasso.SetPosition(1, squares[l,t].transform.TransformPoint(new Vector3(-.5f, 0, .5f)));
                lasso.SetPosition(2, squares[r,t].transform.TransformPoint(new Vector3(.5f, 0, .5f)));
                lasso.SetPosition(3, squares[r,b].transform.TransformPoint(new Vector3(.5f, 0, -.5f)));
            }
        }
        private void EraseLasso()
        {
            lasso.enabled = false;
        }
    }
}