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
        public event Action<Piece> PieceDeletedEvent;
        public event Action<Piece> PieceNichePosChangedEvent;
        public event Action<Piece> PieceNicheRangeChangedEvent;
        // move these events into piece, along with animation

        private void Awake()
        {
            int size = GameManager.Instance.BoardSize;
            // Func<float,float,Color> ColorMap = (x,y)=>ColorHelper.HSLSquare(x,y,.5f);
            Func<float,float,Color> ColorMap = (x,y)=>ColorHelper.YUVtoRGBtruncated(defaultYColor,YUVRange*x,YUVRange*y);

            Vector3 bottomLeft = new Vector3(-.5f, 0, -.5f);
            float gap = 1f / size;
            Vector3 offset = new Vector3(gap / 2, 0, gap / 2);
            float squareWidth = (1f / size) * (1f-squareBorder);
            Vector3 scale = new Vector3(squareWidth, squareWidth, squareWidth);

            squares = new Square[size, size];

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    var s = Instantiate(squarePrefab, transform);

                    s.transform.localPosition = bottomLeft + new Vector3(i * gap, 0, j * gap) + offset;
                    s.transform.localScale = scale;
                    // Color c = ColorMap((float)i/(size-1), (float)j/(size-1));
                    Color c = ColorMap((float)i/(size-1)-.5f, (float)j/(size-1)-.5f);
                    c = ColorHelper.ApplyGamma(c);
                    s.Init(i, j, c);

                    s.ClickedEvent += ()=> ClickSquare(s);
                    s.DragStartedEvent += ()=> DragStartFromSquare(s);
                    s.DragEndedEvent += ()=> DragEndFromSquare(s);
                    s.DraggedIntoEvent += ()=> DragIntoSquare(s);
                    s.DroppedOnEvent += ()=> DropOnSquare(s);
                    // s.HoveredEvent += ()=> HoveredSquare = s;
                    // s.UnhoveredEvent += ()=> { if (HoveredSquare==s) HoveredSquare=null; };

                    squares[i, j] = s;
                }
            }
        }
        private Square selectedSquare = null;
        private Square SelectedSquare {
            get {
                return selectedSquare;
            }
            set {
                if (selectedSquare != null)
                    selectedSquare.Deselect();
                selectedSquare = value;
                if (value != null)
                    selectedSquare.Select();
            }
        }

        public void PlaceNewPiece(Piece p)
        {
            if (selectedSquare == null)
                throw new Exception("square not selected first!");
            else
            {
                p.NichePos = selectedSquare;
                selectedSquare.Occupant = p;
            }
        }
        public void Deselect()
        {
            SelectedSquare = null;
        }
        private void ClickSquare(Square s)
        {
            if (s.Occupant != null)
            {
                s.Occupant.Select();
                PieceSelectedEvent(s.Occupant); // move this into piece
            }
            else
            {
                SquareSelectedEvent(s);
            }
            selectedSquare = s;
        }

        private Square draggedSquare = null;
        private void DragStartFromSquare(Square s)
        {
            if (selectedSquare == null || selectedSquare.Occupant == null)
            {
                // empty drag, do nothing until drop
            }
            else
            {
                s.Occupant.Select();
                PieceSelectedEvent(s.Occupant); // move this into piece
                selectedSquare = s;
            }
            draggedSquare = s;
        }
        private void DragIntoSquare(Square s)
        {
            // change niche, but don't actually drop the piece
            if (draggedSquare != null && draggedSquare.Occupant != null)
            {
                s.Occupant = draggedSquare.Occupant;
                s.Occupant.NichePos = s;
                draggedSquare.Occupant = null;
                PieceNichePosChangedEvent(s.Occupant);

                draggedSquare = selectedSquare = s; // this is a little confusing
            }
        }
        private void DropOnSquare(Square s)
        {
            print("drop");
            // treat an empty drag as a click on the dropped square
            if (draggedSquare == null || selectedSquare == null
                || selectedSquare.Occupant == null)
            {
                ClickSquare(s);
            }
            else // otherwise a piece needs to be affected
            {
                if (draggedSquare == selectedSquare)
                {
                    draggedSquare.Occupant.Deselect();
                }
            }
            draggedSquare = null;
        }
        // always happens after drop
        private void DragEndFromSquare(Square s)
        {
            if (draggedSquare != null) // if not dropped on square
            {
                if (selectedSquare != null && selectedSquare.Occupant != null)
                {
                    // print("DELETE PIECE");
                    PieceDeletedEvent(selectedSquare.Occupant);
                }
                draggedSquare = null;
            }
        }
    }        
}