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