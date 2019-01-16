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

        Square[,] squares;
        public event Action<Square> SquareSelectedEvent;
        public event Action<Piece> PieceSelectedEvent;
        public event Action<Piece> PieceThrownAwayEvent;
        public event Action<Piece> PieceNichePosChangedEvent;
        public event Action<Piece> PieceNicheRangeChangedEvent;
        // move these events into piece, along with animation

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
        private Square selectedSquare = null;
        private Square draggedSquare = null;
        private enum BoardState {
            Idle, SquareSelected, PieceSelected,
            /*EmptyDragging,*/ PieceDragging, NicheDragging
        };
        private BoardState myState = BoardState.Idle;
        private BoardState State {
            get { return myState; }
            set { print(value); myState = value; }
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
                // PieceSelectedEvent(pce);
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
                }
                selectedSquare = sqr;
            }
        }
        public void Deselect()
        {
            ClickSquare(null);
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
                    UpdateNiche(selectedSquare.Occupant, draggedSquare, sqr);
                    draggedSquare = sqr;
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
                print("niche range finish");
                State = BoardState.PieceSelected;
            }
            else
            {
                ClickSquare(sqr);
            }
            draggedSquare = null;
        }
        // always happens after drop
        // also cannot drop on itself
        private void DragEndFromSquare(Square sqr)
        {
            if (draggedSquare != null)
            {
                if (State == BoardState.Idle || State == BoardState.SquareSelected)
                {
                    if (sqr == draggedSquare)
                    {
                        ClickSquare(sqr);
                    }
                }
                else if (State == BoardState.PieceDragging)
                {
                    if (draggedSquare == selectedSquare)
                    {
                        draggedSquare.Occupant.Drop();
                        State = BoardState.PieceSelected;
                    }
                    else
                    {
                        // if drop happens off grid
                        print("DELETE PIECE " + draggedSquare.Occupant.name);
                        PieceThrownAwayEvent(draggedSquare.Occupant);
                        draggedSquare.Occupant = null;
                        selectedSquare.Deselect();
                        State = BoardState.Idle;
                    }
                }
                else if (State == BoardState.NicheDragging) // if the niche is 1 square
                {
                    print("niche range finished 2");
                    State = BoardState.PieceSelected;
                }
                else
                {
                    ClickSquare(null);
                }
                draggedSquare = null;
            }

        }
        private void UpdateNiche(Piece toUpdate, Square from, Square to)
        {
            if (toUpdate.NicheStart == null || toUpdate.NicheEnd == null)
            {
                toUpdate.NicheStart = toUpdate.NicheEnd = from;
                from.AddConsumer(toUpdate);
                PieceNicheRangeChangedEvent(toUpdate);
            }
            else
            {
                int x0=toUpdate.NicheStart.X, x=toUpdate.NichePos.X, x1=toUpdate.NicheEnd.X;
                int y0=toUpdate.NicheStart.Y, y=toUpdate.NichePos.Y, y1=toUpdate.NicheEnd.Y;

                if (from == to) // if it's the start of the drag
                {
                    if ( !(((x0<=x && x<=x1) && (y==y0 || y==y1)) ||
                           ((y0<=y && y<=y1) && (x==x0 || x==x1)))
                    ) {
                        print(x0 + " " + x1);
                        for (int i=x0; i<=x1; i++)
                        {
                            for (int j=y0; j<=y1; j++)
                            {
                                print("hi2");
                                squares[i,j].RemoveConsumer(toUpdate);
                            }
                        }
                        toUpdate.NicheStart = toUpdate.NicheEnd = from;
                        from.AddConsumer(toUpdate);
                        PieceNicheRangeChangedEvent(toUpdate);
                    }
                }
                else
                {
                    // if dragged square is on corner, move corner
                    // if dragged square was on side, move corner connected to that side
                    // check whether it gets bigger or smaller
                    if (toUpdate.NicheStart == from)
                    {
                        print("start");
                        toUpdate.NicheStart = to;
                        to.AddConsumer(toUpdate);
                        PieceNicheRangeChangedEvent(toUpdate);
                    }
                    else if (toUpdate.NicheEnd == from)
                    {
                        print("end");
                        toUpdate.NicheStart = to;
                        to.AddConsumer(toUpdate);
                        PieceNicheRangeChangedEvent(toUpdate);
                    }
                }
                // if (x1 > x0 || 
            }
        }
    }
}