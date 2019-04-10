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
        [SerializeField] StatusBar status;

        class Species
        {
            public bool isProducer = false;
            public float bodySize = 0;
            public float greediness = 0;
            public int nichePosX = -1;
            public int nichePosY = -1;
            public int nicheStartX = -1;
            public int nicheStartY = -1;
            public int nicheEndX = -1;
            public int nicheEndY = -1;
            // TODO:
            // public int randomSeed;
            // DO THISSS (with archie?)
        }
        Stack<Species> moves = new Stack<Species>();

        int idxCounter = 0;
        void Start()
        {
            inspector.OnSpawned +=          ()=> DoMove();
            inspector.OnSizeChosen +=     (kg)=> ChooseSize(kg);
            inspector.OnGreedChosen +=  (a_ii)=> ChooseGreed(a_ii);

            nichess.OnPieceSelected +=       (i)=> print(i);
            nichess.OnPiecePlaced +=     (i,x,y)=> UpdateSpeciesPos(i, x, y);
            nichess.OnPieceNiched += (i,l,b,r,t)=> UpdateSpeciesNiche(i, l, b, r, t);
            nichess.OnEdgeAdded +=         (i,j)=> nodelink.AddLink(i, j);
            nichess.OnEdgeRemoved +=       (i,j)=> nodelink.RemoveLink(i, j);
            nichess.OnEdgeAdded +=         (i,j)=> model.AddInteraction(i, j);
            nichess.OnEdgeRemoved +=       (i,j)=> model.RemoveInteraction(i, j);
            nichess.OnPieceColoured +=     (i,c)=> nodelink.ColorNode(i, c);

            nodelink.OnFocus +=            (i)=> print(i);
            nodelink.OnUnfocus +=           ()=> print("hi");
            nodelink.LaplacianUnsolvable += ()=> print("unsolvable");
            nodelink.LaplacianSolvable +=   ()=> print("solvable");

            model.OnCalculated +=           ()=> CalculateScore();
            // model.OnCalculated +=           ()=> ResizeNodes();
            model.OnEndangered +=          (i)=> nodelink.FlashNode(i);
            model.OnRescued +=             (i)=> nodelink.IdleNode(i);

            status.OnMenu +=                ()=> print("MENU");
            status.OnUndo +=                ()=> UndoMove();
            status.OnRedo +=                ()=> RedoMove();

            PrepareNewMove();
        }
        void PrepareNewMove()
        {
            nichess.AddPiece(idxCounter);
            nodelink.AddNode(idxCounter);
            model.AddSpecies(idxCounter);
            nichess.InspectPiece(idxCounter);

            var newSpecies = new Species();
            if (GameManager.Instance.Level[idxCounter] == true) // if producer, change later
            {
                nichess.FixPieceRange(idxCounter);
                nichess.ShapePieceIntoSquare(idxCounter);
                nodelink.ShapeNodeIntoCube(idxCounter);
                model.SetSpeciesAsProducer(idxCounter);
                newSpecies.isProducer = true;
            }
            else
            {
                nichess.ShapePieceIntoCircle(idxCounter);
                nodelink.ShapeNodeIntoSphere(idxCounter);
                model.SetSpeciesAsConsumer(idxCounter);
                newSpecies.isProducer = false;
            }
            moves.Push(newSpecies);
        }
        void ChooseSize(float size)
        {
            moves.Peek().bodySize = size;
            nichess.ColourPiece2D(idxCounter, size, moves.Peek().greediness);
            model.SetSpeciesBodySize(idxCounter, size);
        }
        void ChooseGreed(float greed)
        {
            moves.Peek().greediness = greed;
            nichess.ColourPiece2D(idxCounter, moves.Peek().bodySize, greed);
            model.SetSpeciesGreediness(idxCounter, greed);
        }
        void UpdateSpeciesPos(int i, int x, int y)
        {
            if (i != idxCounter)
                throw new Exception("NOT POSSIBLE PLACEMENT");

            moves.Peek().nichePosX = x;
            moves.Peek().nichePosY = y;
        }
        void UpdateSpeciesNiche(int i, int l, int b, int r, int t)
        {
            if (i != idxCounter)
                throw new Exception("NOT POSSIBLE NICHE");

            moves.Peek().nicheStartX = l;
            moves.Peek().nicheStartY = b;
            moves.Peek().nicheEndX = r;
            moves.Peek().nicheEndY = t;
        }

        Stack<Species> redoStack = new Stack<Species>();
        void DoMove()
        {
            redoStack.Clear();
            idxCounter += 1;
            if (idxCounter < GameManager.Instance.Level.Length)
            {
                PrepareNewMove();
            }
            else
            {
                EndGame();
            }
        }
        void UndoMove()
        {
            if (moves.Count > 1)
            {
                nichess.RemovePiece(idxCounter);
                nodelink.RemoveNode(idxCounter);
                model.RemoveSpecies(idxCounter);

                Species toUndo = moves.Pop();
                redoStack.Push(toUndo);

                idxCounter -= 1;
                nichess.InspectPiece(idxCounter);
            }
        }
        void RedoMove()
        {
            if (redoStack.Count > 0)
            {
                Species toRedo = redoStack.Pop();
                idxCounter += 1;
                nichess.AddPiece(idxCounter);
                nodelink.AddNode(idxCounter);
                model.AddSpecies(idxCounter);

                nichess.InspectPiece(idxCounter);
                moves.Push(toRedo);

                if (toRedo.isProducer)
                {
                    nichess.FixPieceRange(idxCounter);
                    nichess.ShapePieceIntoSquare(idxCounter);
                    nodelink.ShapeNodeIntoCube(idxCounter);
                    model.SetSpeciesAsProducer(idxCounter);
                }
                else
                {
                    nichess.ShapePieceIntoCircle(idxCounter);
                    nodelink.ShapeNodeIntoSphere(idxCounter);
                    model.SetSpeciesAsConsumer(idxCounter);
                }
                nichess.PlacePiece(idxCounter, toRedo.nichePosX, toRedo.nichePosY);
                nichess.NichePiece(idxCounter, toRedo.nicheStartX, toRedo.nicheStartY, toRedo.nicheEndX, toRedo.nicheEndY);
                nichess.ColourPiece2D(idxCounter, toRedo.bodySize, toRedo.greediness);
                model.SetSpeciesBodySize(idxCounter, toRedo.bodySize);
                model.SetSpeciesGreediness(idxCounter, toRedo.greediness);
            }
        }


        // void ResizeNodes()
        // {
        //     for (int i=0; i<moves.Count; i++)
        //     {
        //         float abundance = model.GetAbundance(i);
        //         if (abundance > 0)
        //         {
        //             nodelink.ResizeNode(i, abundance);
        //         }
        //         else
        //         {
        //             nodelink.ResizeNode(i, 0);
        //             nodelink.ResizeNodeOutline(i, -abundance);
        //         }
        //     }
        // }
        void CalculateScore()
        {
            if (model.Feasible)
                status.FillStar1();
            else
                status.EmptyStar1();
            if (model.Stable)
                status.FillStar2();
            else
                status.EmptyStar2();
            if (model.Nonreactive)
                status.FillStar3();
            else
                status.EmptyStar3();

            if (model.Feasible)
                print("flux: " + model.Flux);
        }
        void EndGame()
        {
            // TODO: stop the game here, and look for height, loops, omnivory
            print("game over!");
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
