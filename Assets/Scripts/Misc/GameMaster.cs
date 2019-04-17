using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EcoBuilder
{
    public class GameMaster : MonoBehaviour
    {
        [SerializeField] Inspector.Inspector inspector;
        [SerializeField] NodeLink.NodeLink nodelink;
        [SerializeField] Model.Model model;
        [SerializeField] StatusBar status;

        class Species
        {
            public int Idx { get; private set; }
            public bool IsProducer { get; private set; }
            public float BodySize { get; set; } = -1;
            public float Greediness { get; set; } = -1;
            public HashSet<int> resources = new HashSet<int>();

            public Species(int idx, bool isProducer)
            {
                Idx = idx;
                IsProducer = isProducer;
            }
            public Color GetColor()
            {
                float y = .8f - .5f*BodySize;
                float u = IsProducer? -.4f : .4f;
                float v = -.4f + .8f*Greediness;
                return ColorHelper.YUVtoRGBtruncated(y, u, v);
            }

            readonly float minKg = .001f, maxKg = 1000; // 1 gram to 1 tonne
            public float GetKg()
            {
                // float min = Mathf.Log10(minKg);
                // float max = Mathf.Log10(maxKg);
                // float mid = min + input*(max-min);
                // return Mathf.Pow(10, mid);

                // same as above commented
                return Mathf.Pow(minKg, 1-BodySize) * Mathf.Pow(maxKg, BodySize);
            }

            // TODO: with Archie?
            // public int randomSeed;
            // public Mesh GetMesh()
            // {

            // }
        }
        // Stack<Species> moves = new Stack<Species>();
        Dictionary<int, Species> allSpecies = new Dictionary<int, Species>();
        Species toAdd = null;
        int idxCounter = 0;

        void Start()
        {
            inspector.OnSizeChosen +=      (x)=> ChangeSize(x);
            inspector.OnGreedChosen +=     (x)=> ChangeGreed(x);

            nodelink.OnNodeHeld +=         (i)=> Inspect(i);
            nodelink.OnNodeClicked +=      (i)=> MakeInteraction(i);
            nodelink.OnEmptyClicked +=      ()=> Uninspect();
            nodelink.LaplacianUnsolvable += ()=> print("unsolvable");
            nodelink.LaplacianSolvable +=   ()=> print("solvable");
            nodelink.OnDroppedOn +=         ()=> AddNewSpecies();

            model.OnCalculated +=           ()=> CalculateScore();
            // model.OnCalculated +=           ()=> ResizeNodes();
            // model.OnEndangered +=          (i)=> nodelink.FlashNode(i);
            // model.OnRescued +=             (i)=> nodelink.IdleNode(i);
            model.OnEndangered +=          (i)=> print("neg: " + i);
            model.OnRescued +=             (i)=> print("pos: " + i);

            // status.OnMenu +=                ()=> print("MENU");
            // status.OnUndo +=                ()=> UndoMove();
            // status.OnRedo +=                ()=> RedoMove();

            // AddNewSpecies(idxCounter++);
            toAdd = new Species(idxCounter, GameManager.Instance.SelectedLevel.IsProducers[0]);
        }

        void AddNewSpecies()
        {
            if (allSpecies.Count >= GameManager.Instance.SelectedLevel.IsProducers.Count)
            {
                bool constraintSatisfied = GameManager.Instance.SelectedLevel.GraphConstraints(nodelink);
                if (constraintSatisfied)
                {
                    print("game over!");
                }
                else
                {
                    print(GameManager.Instance.SelectedLevel.ConstraintNotMetMessage);
                }
                return;
            }
            else if (toAdd.BodySize >= 0 && toAdd.Greediness >= 0)
            {
                nodelink.AddNode(toAdd.Idx);
                model.AddSpecies(toAdd.Idx);
                if (toAdd.IsProducer)
                {
                    nodelink.ShapeNodeIntoCube(toAdd.Idx);
                    model.SetSpeciesAsProducer(toAdd.Idx);
                }
                else
                {
                    nodelink.ShapeNodeIntoSphere(toAdd.Idx);
                    model.SetSpeciesAsConsumer(toAdd.Idx);
                }
                nodelink.ColourNode(toAdd.Idx, toAdd.GetColor());
                model.SetSpeciesBodySize(toAdd.Idx, toAdd.GetKg());
                model.SetSpeciesGreediness(toAdd.Idx, toAdd.Greediness);

                allSpecies[toAdd.Idx] = toAdd;
                Inspect(toAdd.Idx);

                idxCounter += 1;
                if (idxCounter < GameManager.Instance.SelectedLevel.IsProducers.Count)
                {
                    toAdd = new Species(idxCounter, GameManager.Instance.SelectedLevel.IsProducers[idxCounter]);
                }
                else
                {
                    toAdd = null;
                }
            }
        }
        void ChangeSize(float size)
        {
            print("hello");
            if (inspected == null)
            {
                if (toAdd != null) // TODO: change this to disabling the sliders completely
                    toAdd.BodySize = size;
            }
            else
            {
                inspected.BodySize = size;
                nodelink.ColourNode(inspected.Idx, inspected.GetColor());
                model.SetSpeciesBodySize(inspected.Idx, inspected.GetKg());
            }
        }
        void ChangeGreed(float greed)
        {
            if (inspected == null)
            {
                if (toAdd != null) // TODO: change this to disabling the sliders completely
                    toAdd.Greediness = greed;
            }
            else
            {
                inspected.Greediness = greed;
                nodelink.ColourNode(inspected.Idx, inspected.GetColor());
                model.SetSpeciesGreediness(inspected.Idx, greed);
            }
        }

        Species inspected;
        void Inspect(int idx)
        {
            if (inspected != allSpecies[idx])
            {
                inspected = allSpecies[idx];
                inspector.SetSize(inspected.BodySize);
                inspector.SetGreed(inspected.Greediness);

                nodelink.FocusNode(idx);
            }
        }
        void Uninspect()
        {
            if (inspected != null)
            {
                inspected = null;
                inspector.SetSize(toAdd.BodySize);
                inspector.SetGreed(toAdd.Greediness);

                nodelink.Unfocus();
            }
        }
        void MakeInteraction(int res)
        {
            if (inspected == null)
            {
                // TODO: maybe inspect in this case
                return;
            }
            if (inspected.IsProducer)
            {
                return;
            }
            int con = inspected.Idx;
            if (res != con)
            {
                if (!inspected.resources.Contains(res))
                {
                    inspected.resources.Add(res);
                    nodelink.AddLink(res, con);
                    model.AddInteraction(res, con);
                }
                else
                {
                    inspected.resources.Remove(res);
                    nodelink.RemoveLink(res, con);
                    model.RemoveInteraction(res, con);
                }
            }
        }

        // Stack<Species> redoStack = new Stack<Species>();
        // void DoMove()
        // {
        //     redoStack.Clear();
        //     idxCounter += 1;
        //     if (idxCounter < GameManager.Instance.Level.Length)
        //     {
        //         PrepareNewMove();
        //     }
        //     else
        //     {
        //         EndGame();
        //     }
        // }
        // void UndoMove()
        // {
        //     if (moves.Count > 1)
        //     {
        //         nodelink.RemoveNode(idxCounter);
        //         model.RemoveSpecies(idxCounter);

        //         Species toUndo = moves.Pop();
        //         redoStack.Push(toUndo);

        //         idxCounter -= 1;
        //     }
        // }
        // void RedoMove()
        // {
        //     if (redoStack.Count > 0)
        //     {
        //         Species toRedo = redoStack.Pop();
        //         idxCounter += 1;
        //         nodelink.AddNode(idxCounter);
        //         model.AddSpecies(idxCounter);

        //         moves.Push(toRedo);

        //         if (toRedo.isProducer)
        //         {
        //             nodelink.ShapeNodeIntoCube(idxCounter);
        //             model.SetSpeciesAsProducer(idxCounter);
        //         }
        //         else
        //         {
        //             nodelink.ShapeNodeIntoSphere(idxCounter);
        //             model.SetSpeciesAsConsumer(idxCounter);
        //         }
        //         model.SetSpeciesBodySize(idxCounter, toRedo.bodySize);
        //         model.SetSpeciesGreediness(idxCounter, toRedo.greediness);
        //     }
        // }


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
