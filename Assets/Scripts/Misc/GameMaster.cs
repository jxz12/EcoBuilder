using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EcoBuilder
{
    public class GameMaster : MonoBehaviour
    {
        [SerializeField] Nichess.Nichess nichess;
        [SerializeField] Inspector.Inspector inspector;
        [SerializeField] NodeLink.NodeLink nodelink;
        [SerializeField] Model.Model model;

        class Species
        {
            public bool producer { get; private set; }
            public double bodySize { get; private set; }
            public double greediness { get; private set; }
            public int nichePos { get; private set; }
            public int nicheStart { get; private set; }
            public int nicheEnd { get; private set; }
            // TODO:
            // public int randomSeed;
            // DO THISSS
            public Species(bool prod, double size, double greed, int pos, int start, int end)
            {
                producer = prod;
                bodySize = size;
                greediness = greed;
                nichePos = pos;
                nicheStart = start;
                nicheEnd = end;
            }
        }
        Stack<Species> moves = new Stack<Species>();

        void Awake()
        {
            
        }
        int idxCounter = 0;
        void Start()
        {
            nichess.OnPiecePlaced +=        ()=> PlacePiece();
            nichess.OnPieceSelected +=     (i)=> print(i);
            nichess.OnEdgeAdded +=       (i,j)=> nodelink.AddLink(i,j);
            nichess.OnEdgeRemoved +=     (i,j)=> nodelink.RemoveLink(i,j);
            nodelink.OnFocus +=            (i)=> print(i);
            nodelink.OnUnfocus +=           ()=> print("hi");
            nodelink.LaplacianUnsolvable += ()=> print("unsolvable");
            nodelink.LaplacianSolvable +=   ()=> print("solvable");
            inspector.OnSpawned +=          ()=> Do();
            inspector.OnSizeChosen +=      (x)=> ChooseSize(x);
            inspector.OnGreedChosen +=     (x)=> ChooseGreed(x);

            PrepareNewMove();
        }
        void PrepareNewMove()
        {
            nichess.AddNewPiece(idxCounter);
            nodelink.AddNode(idxCounter);
            if (GameManager.Instance.Level[idxCounter] == true) // if producer
            {
                nichess.FixPieceRange(idxCounter);
                nichess.ShapePieceIntoSquare(idxCounter);
            }
            else
            {
                nichess.ShapePieceIntoCircle(idxCounter);
            }
        }
        void ChooseSize(float size)
        {
            nichess.ColourPieceUV(size, .5f);
        }
        void ChooseGreed(float greed)
        {
            nichess.ColourPieceUV(.5f, greed);
        }

        Stack<Species> undoStack = new Stack<Species>();
        public void Do()
        {
            moves.Push(new Species(false, 0, 0, 0, 0, 0));
            undoStack.Clear();

            idxCounter += 1;
            if (idxCounter < GameManager.Instance.Level.Length)
            {
                PrepareNewMove();
            }
        }
        public void Undo()
        {
            undoStack.Push(moves.Pop());
            // TODO: revert to top of stack species
        }
        public void Redo()
        {
            if (undoStack.Count > 0)
            {
                // TODO: make move again
                moves.Push(undoStack.Pop());
            }
        }

        public void PlacePiece()
        {
            return; // TODOOO
        }



        
        public void Quit()
        {
        }
        public void Save()
        {
            // set gamemanager file here
        }
        public void Load()
        {
            // grab gamemanager file here
        }
    }
}
