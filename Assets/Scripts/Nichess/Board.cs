using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EcoBuilder.Nichess
{
    public class Board : MonoBehaviour
    {
        // board handles square events, piece handles piece events

        [SerializeField] Square squarePrefab;
        [SerializeField] Color lightSq, darkSq;
        [SerializeField] float squareBorder=.05f;
        // [SerializeField] MeshRenderer glow;

        Square[,] squares;
        SortedDictionary<int, int> markerCounts;

        private void Awake()
        {
            int size = GameManager.Instance.BoardSize;

            squares = new Square[size, size];

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    var s = Instantiate(squarePrefab, transform);

                    Color c = (i+j)%2 == 0? lightSq : darkSq;
                    s.Init(i, j, c, size, squareBorder);

                    s.OnClicked +=     ()=> ClickSquare(s);
                    s.OnHeld +=        ()=> HoldSquare(s);
                    s.OnEnter +=       ()=> EnterSquare(s);
                    s.OnExit +=        ()=> ExitSquare(s);
                    s.OnDragStarted += ()=> DragStartFromSquare(s);
                    s.OnDragEnded +=   ()=> DragEndFromSquare(s);
                    s.OnDraggedInto += ()=> DragIntoSquare(s);
                    s.OnDroppedOn +=   ()=> DropOnSquare(s);

                    squares[i, j] = s;
                }
            }
            markerCounts = new SortedDictionary<int,int>();
            markerCounts[0] = size*size; // keep track of markers on each square
        }


        Piece inspected = null;
        public void AddPiece(Piece p)
        {
            state = BoardState.Idle;
        }
        public void InspectPiece(Piece p)
        {
            if (inspected != null)
                inspected.Uninspect();

            p.Inspect();
            inspected = p;
        }
        public void PlacePiece(Piece p, int x, int y)
        {
            int size = squares.GetLength(0);
            if (x >= size || y >= size)
                throw new Exception("out of bounds of board");

            Square s = squares[x, y];
            if (s.Occupant != null)
                throw new Exception("square is occupied");

            p.PlaceOnSquare(s);
            OnPosChanged.Invoke(inspected);
        }
        public void NichePiece(Piece p, int l, int b, int r, int t)
        {
            int size = squares.GetLength(0);
            if (l >= size || b >= size || r >= size || t >= size)
                throw new Exception("out of bounds of board");

            if (p.NichePos != null &&
                l<=p.NichePos.X && p.NichePos.X<=r &&
                b<=p.NichePos.Y && p.NichePos.Y<=t
            ) {
                throw new Exception("cannot eat itself");
            }

            UpdateNicheMarkers(p, squares[l, b], squares[r, t]);
            // guaranteed to be updated
            OnNicheChanged.Invoke(inspected);
        }
        public void RemovePiece(Piece p)
        {
            if (p.NicheMin != null && p.NicheMax != null)
            {
                int l = p.NicheMin.X, r = p.NicheMax.X;
                int b = p.NicheMin.Y, t = p.NicheMax.Y;

                for (int i=l; i<=r; i++)
                    for (int j=b; j<=t; j++)
                        RemoveMarkerFromSquare(squares[i,j], p);
            }
            // OnNicheChanged.Invoke(p); // not needed because should be removed anyway

            if (p == inspected)
            {
                inspected = null;
            }
            p.Remove();
        }


        //////////////////////
        // events and state

        public event Action<Piece> OnPosChanged;
        public event Action<Piece> OnNicheChanged;

        enum BoardState { Idle, PieceDragging, NicheDragging };
        BoardState state = BoardState.Idle;

        // HashSet<Square> enteredSquares = new HashSet<Square>();
        private void EnterSquare(Square s)
        {
            // if (enteredSquares.Count == 1)
            // {
            //     state = BoardState.DragTwoFingers;
            //     bool updated = UpdateNicheMarkers(inspected, enteredSquares.First(), s);
            //     if (updated)
            //     {
            //         OnNicheChanged.Invoke(inspected);
            //     }
            // }
            // enteredSquares.Add(s);
        }
        private void ExitSquare(Square s)
        {
            // if (enteredSquares.Count == 2)
            // {
            //     nicheStartSquare = s;
            //     state = BoardState.DragOneFinger;
            // }
            // enteredSquares.Remove(s);
        }
        private void ClickSquare(Square s)
        {
            if (state == BoardState.Idle)
            {
                bool updated = UpdateNicheMarkers(inspected, s, s);
                if (updated)
                {
                    OnNicheChanged.Invoke(inspected);
                }
                else
                {
                    // TODO: do a jiggle animation here
                }
            }
        }

        public void HoldSquare(Square s)
        {
            // do nothing
        }

        Square nicheStartSquare = null;
        public void DragStartFromSquare(Square s)
        {
            if (state == BoardState.Idle)
            {
                if (s == inspected.NichePos || inspected.NichePos == null)
                {
                    state = BoardState.PieceDragging;
                    inspected.Lift();
                }
                else
                {
                    bool updated = UpdateNicheMarkers(inspected, s, s);
                    if (updated)
                    {
                        nicheStartSquare = s;
                        state = BoardState.NicheDragging;
                        OnNicheChanged.Invoke(inspected);
                    }
                }
            }
        }
        public void DragEndFromSquare(Square s)
        {
            state = BoardState.Idle;
        }
        public void DragIntoSquare(Square s)
        {
            if (state == BoardState.Idle)
            {
            }
            else if (state == BoardState.PieceDragging)
            {
                if (s.Occupant == null && !inspected.SquareInNiche(s))
                {
                    inspected.PlaceOnSquare(s);
                    s.Occupant = inspected;
                    OnPosChanged.Invoke(inspected);
                }
            }
            else if (state == BoardState.NicheDragging)
            {
                bool updated = UpdateNicheMarkers(inspected, nicheStartSquare, s);
                if (updated)
                {
                    OnNicheChanged.Invoke(inspected);
                }
            }

        }
        public void DropOnSquare(Square s)
        {
            if (state == BoardState.Idle)
            {
                // ClickSquare(s);
            }
            else if (state == BoardState.PieceDragging)
            {
                inspected.Drop();
            }
            else if (state == BoardState.NicheDragging)
            {

            }
        }

        //////////////////////////////
        // niche checks

        // returns if the niche has been updated
        private bool UpdateNicheMarkers(Piece toUpdate, Square min, Square max)
        {
            if (toUpdate.StaticRange)
            {
                return false;
            }
            else
            {
                // 1 is new niche range
                int l1 = min.X, r1 = max.X;
                int b1 = min.Y, t1 = max.Y;

                // flip niche start or end back around to make calculation simpler
                if (l1 > r1) { int temp = l1; l1 = r1; r1 = temp; }
                if (b1 > t1) { int temp = b1; b1 = t1; t1 = temp; }

                if (toUpdate.NichePos != null &&
                    l1<=toUpdate.NichePos.X && toUpdate.NichePos.X<=r1 &&
                    b1<=toUpdate.NichePos.Y && toUpdate.NichePos.Y<=t1
                ) {
                    // cannot eat itself
                    return false;
                }

                if (toUpdate.NicheMin == null || toUpdate.NicheMax == null) // new piece
                {
                    // simply fill new squares
                    for (int i=l1; i<=r1; i++)
                    {
                        for (int j=b1; j<=t1; j++)
                        {
                            AddMarkerToSquare(squares[i,j], toUpdate);
                        }
                    }
                }
                else
                {
                    // 0 is old niche range, l=left, r=right, b=bottom, t=top
                    int l0=toUpdate.NicheMin.X, r0=toUpdate.NicheMax.X;
                    int b0=toUpdate.NicheMin.Y, t0=toUpdate.NicheMax.Y;

                    // make the following better? complexity isn't great, but in practice should be okay
                        
                    // 2 is intersection of rectangles
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
                                RemoveMarkerFromSquare(squares[i,j], toUpdate);
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
                                AddMarkerToSquare(squares[i,j], toUpdate);
                            }
                        }
                    }
                }
                toUpdate.NicheMin = squares[l1, b1];
                toUpdate.NicheMax = squares[r1, t1];

                RescaleMarkersIfNeeded();
                return true;
            }
        }

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
        private void AddMarkerToSquare(Square sqr, Piece con)
        {
            RemoveMarkerCount(sqr.NumMarkers);
            sqr.AddMarker(con);
            AddMarkerCount(sqr.NumMarkers);
        }
        private void RemoveMarkerFromSquare(Square sqr, Piece con)
        {
            RemoveMarkerCount(sqr.NumMarkers);
            sqr.RemoveMarker(con);
            AddMarkerCount(sqr.NumMarkers);
        }
        [SerializeField] float maxMarkerSize = 1f;
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























        /*
        private Square selectedSquare = null;
        private Square draggedSquare = null;
        private enum BoardState {
            Idle, SquareSelected, PieceSelected,
            PieceDragging, NicheDragging
        };
        private BoardState myState = BoardState.Idle;
        private BoardState State {
            get { return myState; }
            set {
                myState = value;
                if (myState == BoardState.Idle)
                {
                    EraseLasso();
                    EraseGlow();
                }
                else if (myState == BoardState.SquareSelected)
                {
                    EraseLasso();
                    DrawGlow(selectedSquare);
                }
                else if (myState == BoardState.PieceSelected)
                {
                    DrawLasso(selectedSquare.Occupant);
                    EraseGlow();
                }
                else if (myState == BoardState.NicheDragging)
                {
                    DrawLasso(selectedSquare.Occupant);
                }
                else if (myState == BoardState.PieceDragging)
                {
                    DrawLasso(selectedSquare.Occupant);
                    EraseGlow();
                }
            }
        }

        public void PlaceNewPieceOnSelectedSquare(Piece newPiece)
        {
            if (selectedSquare == null)
                throw new Exception("square not selected first!");
            else
            {
                newPiece.NichePos = selectedSquare;
                selectedSquare.Occupant = newPiece;

                State = BoardState.PieceSelected;
            }
        }

        public void SelectPieceExternal(Piece pce)
        {
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

            State = BoardState.PieceSelected;
        }
        public void RemovePieceExternal(Piece toRemove)
        {
            if (State == BoardState.Idle || State == BoardState.SquareSelected)
            {
                // nothing extra needed
            }
            else if (State == BoardState.PieceSelected)
            {
                if (selectedSquare.Occupant == toRemove)
                {
                    selectedSquare.Deselect();
                    selectedSquare = null;
                    State = BoardState.Idle;
                }
            }
            else if (State == BoardState.PieceDragging)
            {
                if (draggedSquare.Occupant == toRemove)
                {
                    selectedSquare.Deselect();
                    selectedSquare = null;
                    State = BoardState.Idle;
                }
            }
            else if (State == BoardState.NicheDragging)
            {
                if (selectedSquare.Occupant == toRemove)
                {
                    selectedSquare.Deselect();
                    selectedSquare = null;
                    State = BoardState.Idle;
                }
            }
            toRemove.NichePos.Occupant = null;
            RemoveNiche(toRemove);
            toRemove.ThrowAwayExternal();
        }

        public void DeselectAll()
        {
            ClickSquare(null);
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
                selectedSquare = sqr;
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
                        OnSquareSelected(sqr);
                        State = BoardState.SquareSelected;
                    }
                }
                else
                {
                    State = BoardState.Idle;
                }
            }
            else
            {
                if (State == BoardState.PieceDragging)
                {
                    selectedSquare.Occupant.Drop();
                }
            }
        }
        private void HoldSquare(Square heldSquare)
        {
            if (heldSquare.Occupant != null)
            {
                if (heldSquare.Occupant.StaticPos)
                {
                    print("cannot move! TODO: make this show a message instead");
                }
                else
                {
                    if (State == BoardState.Idle)
                    {
                        heldSquare.Select();
                        heldSquare.Occupant.Select();
                        heldSquare.Occupant.Lift();

                        selectedSquare = heldSquare;
                        State = BoardState.PieceDragging;
                    }
                    else if (State == BoardState.SquareSelected)
                    {
                        if (selectedSquare == heldSquare)
                        {
                            throw new Exception("impossibly occupied square");
                        }
                        selectedSquare.Deselect();

                        heldSquare.Select();
                        heldSquare.Occupant.Select();
                        heldSquare.Occupant.Lift();

                        selectedSquare = heldSquare;
                        State = BoardState.PieceDragging;
                    }
                    else if (State == BoardState.PieceSelected)
                    {
                        if (selectedSquare != heldSquare)
                        {
                            selectedSquare.Deselect();
                            selectedSquare.Occupant.Deselect();

                            heldSquare.Select();
                            heldSquare.Occupant.Select();
                            selectedSquare = heldSquare;
                        }
                        heldSquare.Occupant.Lift();

                        State = BoardState.PieceDragging;
                    }   
                    else
                    {
                        throw new Exception("impossible state");
                    }
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
                    RemoveNiche(draggedSquare.Occupant);
                    draggedSquare.Occupant.ThrowAway();
                    draggedSquare.Occupant = null;
                    selectedSquare.Deselect();
                    selectedSquare = null;

                    State = BoardState.Idle;
                    ///////////////

                    // return to original position
                    ///////////////
                    // selectedSquare.Occupant = draggedSquare.Occupant;
                    // selectedSquare.Occupant.NichePos = selectedSquare;
                    // selectedSquare.Occupant.Drop();
                    // DrawLasso(selectedSquare.Occupant);
                    // draggedSquare.Occupant = null;

                    // State = BoardState.PieceSelected;
                    ///////////////
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

        private void DrawLasso(Piece toDraw)
        {
            if (toDraw.NicheMin == null || toDraw.NicheMax == null)
            {
                EraseLasso();
                toDraw.LookNowhere();
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

                toDraw.LookAt((squares[l,b].transform.position + squares[r,t].transform.position) / 2);
            }
        }
        private void EraseLasso()
        {
            lasso.enabled = false;
        }
        
        private void DrawGlow(Square toDraw)
        {
            glow.transform.parent = toDraw.transform;
            glow.transform.localPosition = Vector3.zero;
            glow.GetComponent<Animator>().SetBool("Pulse", true);
        }
        private void EraseGlow()
        {
            glow.GetComponent<Animator>().SetBool("Pulse", false);
        }
        */
    }
}