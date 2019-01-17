using System;
using System.Collections.Generic;
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

        [SerializeField] PieceCamera cam;

        Square[,] squares;
        public event Action<Square> SquareSelectedEvent;
        public event Action<Piece> PieceSelectedEvent;
        public event Action<Piece> PieceThrownAwayEvent;
        public event Action<Piece> PieceNichePosChangedEvent;
        public event Action<Piece> PieceNicheRangeChangedEvent;

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
                    c = ColorHelper.ApplyGamma(c);
                    s.Init(i, j, c, size, squareBorder);

                    s.ClickedEvent += ()=> ClickSquare(s);
                    s.DragStartedEvent += ()=> DragStartFromSquare(s);
                    s.DragEndedEvent += ()=> DragEndFromSquare(s);
                    s.DraggedIntoEvent += ()=> DragIntoSquare(s);
                    s.DroppedOnEvent += ()=> DropOnSquare(s);

                    squares[i, j] = s;
                }
            }
        }
        private void Start()
        {
            SquareSelectedEvent += s=> cam.ViewBoard(this);
            PieceSelectedEvent += p=> cam.ViewPiece(p);
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

        public void PlaceNewPiece(Piece pce)
        {
            if (selectedSquare == null)
                throw new Exception("square not selected first!");
            else
            {
                pce.NichePos = selectedSquare;
                selectedSquare.Occupant = pce;
                pce.Select();
                cam.ViewPiece(pce);
                State = BoardState.PieceSelected;
            }
        }

        private void ClickSquare(Square sqr)
        {
            if (sqr != selectedSquare)
            {
                // undo previous stuff
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
                        PieceSelectedEvent(sqr.Occupant);
                    }
                    else
                    {
                        State = BoardState.SquareSelected;
                        SquareSelectedEvent(sqr);
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
        public void SelectPiece(Piece pce)
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
            pce.Select();
            pce.NichePos.Select();
            cam.ViewPiece(pce);
            State = BoardState.PieceSelected;
            selectedSquare = pce.NichePos;
        }
        public void DeselectAll()
        {
            ClickSquare(null);
            cam.ViewBoard(this);
        }

        private void DragStartFromSquare(Square sqr)
        {
            if (State == BoardState.Idle || State == BoardState.SquareSelected)
            {
                // if there is something to drag then click it first
                if (sqr.Occupant != null)
                {
                    ClickSquare(sqr);
                    sqr.Occupant.Lift();
                    State = BoardState.PieceDragging;
                }
            }
            else if (State == BoardState.PieceSelected)
            {
                // if we are dragging the same square
                if (sqr == selectedSquare)
                {
                    sqr.Occupant.Lift();
                    State = BoardState.PieceDragging;
                }
                else // otherwise, change niche range
                {
                    UpdateNiche(selectedSquare.Occupant, sqr, sqr);
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
            if (sqr != draggedSquare)
            {
                if (State == BoardState.PieceDragging)
                {
                    // change nichePos, but don't actually drop the piece
                    sqr.Occupant = draggedSquare.Occupant;
                    draggedSquare.Occupant = null;
                    sqr.Occupant.NichePos = sqr;
                    draggedSquare = sqr;
                    PieceNichePosChangedEvent(sqr.Occupant);
                }
                else if (State == BoardState.NicheDragging)
                {
                    bool updated = UpdateNiche(selectedSquare.Occupant, draggedSquare, sqr);
                    if (updated)
                    {
                        draggedSquare = sqr;
                    }
                }
            }
        }
        private void DropOnSquare(Square sqr)
        {
            if (State == BoardState.PieceDragging)
            {
                draggedSquare.Occupant.Drop();
                // we no not want to reselect the piece, so do not call 
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
            if (draggedSquare != null)
            {
                if (State == BoardState.PieceDragging)
                {
                    cam.ViewBoard(this);
                    PieceThrownAwayEvent(draggedSquare.Occupant);
                    draggedSquare.Occupant = null;
                    selectedSquare.Deselect();
                    State = BoardState.Idle;
                }
                else if (State == BoardState.NicheDragging) // if the niche is 1 square
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
        private bool UpdateNiche(Piece toUpdate, Square from, Square to)
        {
            // new piece
            if (toUpdate.NicheMin == null || toUpdate.NicheMax == null)
            {
                toUpdate.NicheMin = toUpdate.NicheMax = from;
                from.AddConsumer(toUpdate);
                PieceNicheRangeChangedEvent(toUpdate);
                return true;
            }
            else
            {
                // 0 is old Niche Range, l=left, r=right, b=bottom, t=top
                int l0=toUpdate.NicheMin.X, r0=toUpdate.NicheMax.X;
                int b0=toUpdate.NicheMin.Y, t0=toUpdate.NicheMax.Y;

                if (from == to) // if it's the start of the drag
                {
                    if ( !(((l0<=from.X && from.X<=r0) && (from.Y==b0 || from.Y==t0)) ||
                           ((b0<=from.Y && from.Y<=t0) && (from.X==l0 || from.X==r0)))
                    ) {
                        for (int i=l0; i<=r0; i++)
                        {
                            for (int j=b0; j<=t0; j++)
                            {
                                squares[i,j].RemoveConsumer(toUpdate);
                            }
                        }
                        toUpdate.NicheMin = toUpdate.NicheMax = from;
                        from.AddConsumer(toUpdate);
                        PieceNicheRangeChangedEvent(toUpdate);
                        return true;
                    }
                    else
                    {
                        return false; // otherwise we have grabbed a side or a corner
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
                        // return false;
                    }

                    // flip niche start or end back around to make calculation simpler
                    if (l1 > r1) { int temp = l1; l1 = r1; r1 = temp; }
                    if (b1 > t1) { int temp = b1; b1 = t1; t1 = temp; }

                    if (l1<=toUpdate.NichePos.X && toUpdate.NichePos.X<=r1 &&
                        b1<=toUpdate.NichePos.Y && toUpdate.NichePos.Y<=t1
                    ) {
                        // cannot eat itself
                        // State = BoardState.PieceSelected;
                        return false;
                    }

                    toUpdate.NicheMin = squares[l1, b1];
                    toUpdate.NicheMax = squares[r1, t1];
                    

                    ////////////////////////////////////
                    // MAKE THE FOLLOWING BETTER OMG
                     
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
                                squares[i,j].RemoveConsumer(toUpdate);
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
                                squares[i,j].AddConsumer(toUpdate);
                            }
                        }
                    }
                    PieceNicheRangeChangedEvent(toUpdate);
                    return true;
                }
            }
        }
    }
}