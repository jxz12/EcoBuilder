using System;
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
            public bool isProducer = false;
            public float bodySize = 0;
            public float greediness = 0;
            public int nichePosX = 0;
            public int nichePosY = 0;
            public int nicheStartX = 0;
            public int nicheStartY = 0;
            public int nicheEndX = 0;
            public int nicheEndY = 0;
            // TODO:
            // public int randomSeed;
            // DO THISSS (with archie?)
        }
        Stack<Species> moves = new Stack<Species>();

        int idxCounter = 0;
        void Start()
        {
            inspector.OnSpawned +=          ()=> Do();
            inspector.OnSizeChosen +=     (kg)=> ChooseSize(kg);
            inspector.OnGreedChosen +=  (a_ii)=> ChooseGreed(a_ii);

            nichess.OnPieceSelected +=       (i)=> print(i);
            nichess.OnPiecePlaced +=     (i,x,y)=> PlacePiece(i, x, y);
            nichess.OnPieceNiched += (i,l,b,r,t)=> NichePiece(i, l, b, r, t);
            nichess.OnEdgeAdded +=         (i,j)=> nodelink.AddLink(i, j);
            nichess.OnEdgeRemoved +=       (i,j)=> nodelink.RemoveLink(i, j);
            nichess.OnPieceColoured +=     (i,c)=> nodelink.ColorNode(i, c);

            nodelink.OnFocus +=            (i)=> print(i);
            nodelink.OnUnfocus +=           ()=> print("hi");
            nodelink.LaplacianUnsolvable += ()=> print("unsolvable");
            nodelink.LaplacianSolvable +=   ()=> print("solvable");

            PrepareNewMove();
        }
        void PrepareNewMove()
        {
            nichess.AddPiece(idxCounter);
            nodelink.AddNode(idxCounter);
            nichess.InspectPiece(idxCounter);

            var newSpecies = new Species();
            if (GameManager.Instance.Level[idxCounter] == true) // if producer, change later
            {
                nichess.FixPieceRange(idxCounter);
                nichess.ShapePieceIntoSquare(idxCounter);
                nodelink.ShapeNodeIntoCube(idxCounter);
                newSpecies.isProducer = true;
            }
            else
            {
                nichess.ShapePieceIntoCircle(idxCounter);
                nodelink.ShapeNodeIntoSphere(idxCounter);
                newSpecies.isProducer = false;
            }
            moves.Push(newSpecies);
        }
        void ChooseSize(float size)
        {
            moves.Peek().bodySize = size;
            nichess.ColourPiece2D(idxCounter, size, moves.Peek().greediness);
        }
        void ChooseGreed(float greed)
        {
            moves.Peek().greediness = greed;
            nichess.ColourPiece2D(idxCounter, moves.Peek().bodySize, greed);
        }
        void PlacePiece(int i, int x, int y)
        {
            if (i != idxCounter)
                throw new Exception("NOT POSSIBLE PLACEMENT");

            moves.Peek().nichePosX = x;
            moves.Peek().nichePosY = y;
        }
        void NichePiece(int i, int l, int b, int r, int t)
        {
            if (i != idxCounter)
                throw new Exception("NOT POSSIBLE NICHE");

            moves.Peek().nicheStartX = l;
            moves.Peek().nicheStartY = b;
            moves.Peek().nicheEndX = r;
            moves.Peek().nicheEndY = t;
        }

        Stack<Species> redoStack = new Stack<Species>();
        public void Do()
        {
            redoStack.Clear();

            idxCounter += 1;
            if (idxCounter < GameManager.Instance.Level.Length)
            {
                PrepareNewMove();
            }
        }
        public void Undo()
        {
            if (moves.Count > 1)
            {
                nichess.RemovePiece(idxCounter);
                nodelink.RemoveNode(idxCounter);
                redoStack.Push(moves.Pop());

                idxCounter -= 1;
                nichess.InspectPiece(idxCounter);
            }
        }
        public void Redo()
        {
            if (redoStack.Count > 0)
            {
                Species toRedo = redoStack.Pop();
                idxCounter += 1;
                nichess.AddPiece(idxCounter);
                nodelink.AddNode(idxCounter);
                if (toRedo.isProducer)
                {
                    nichess.FixPieceRange(idxCounter);
                    nichess.ShapePieceIntoSquare(idxCounter);
                    nodelink.ShapeNodeIntoCube(idxCounter);
                }
                else
                {
                    nichess.ShapePieceIntoCircle(idxCounter);
                    nodelink.ShapeNodeIntoSphere(idxCounter);
                }
                nichess.PlacePiece(idxCounter, toRedo.nichePosX, toRedo.nichePosY);
                nichess.NichePiece(idxCounter, toRedo.nicheStartX, toRedo.nicheStartY, toRedo.nicheEndX, toRedo.nicheEndY);
                moves.Push(redoStack.Pop());
            }
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
