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
        [SerializeField] SpawnArea spawnArea;
        [SerializeField] StatusBar status;

        class Species
        {
            public int Idx { get; private set; }
            public bool IsProducer { get; set; }
            public Species(int idx, bool isProducer)
            {
                Idx = idx;
                IsProducer = isProducer;
                RandomSeed = UnityEngine.Random.Range(0, int.MaxValue);
            }
            public HashSet<int> resources { get; private set; } = new HashSet<int>();
            public float BodySize { get; set; } = .5f;
            public float Greediness { get; set; } = .5f;
            public int RandomSeed { get; set; } = 0;
        }
        Dictionary<int, Species> allSpecies = new Dictionary<int, Species>();
        int idxCounter = 0;

        void Start()
        {
            inspector.OnSizeChanged +=      (x)=> ChangeSize(x);
            inspector.OnGreedChanged +=     (x)=> ChangeGreed(x);
            inspector.OnProducerPressed +=  ()=> InitNewSpecies(true);
            inspector.OnConsumerPressed +=  ()=> InitNewSpecies(false);

            nodelink.OnNodeHeld +=         (i)=> Inspect(i);
            nodelink.OnNodeClicked +=      (j)=> MakeInteraction(j);
            nodelink.OnEmptyClicked +=      ()=> Uninspect();
            nodelink.LaplacianUnsolvable += ()=> print("unsolvable");
            nodelink.LaplacianSolvable +=   ()=> print("solvable");
            nodelink.OnDroppedOn +=         ()=> TryAddNewSpecies();

            model.OnCalculated +=           ()=> CalculateScore();
            // model.OnCalculated +=           ()=> ResizeNodes();
            // model.OnEndangered +=          (i)=> nodelink.FlashNode(i);
            // model.OnRescued +=             (i)=> nodelink.IdleNode(i);
            model.OnEndangered +=          (i)=> print("neg: " + i);
            model.OnRescued +=             (i)=> print("pos: " + i);

            // status.OnMenu +=                ()=> print("MENU");
            // status.OnUndo +=                ()=> UndoMove();
            // status.OnRedo +=                ()=> RedoMove();

        }

        bool shown = false;
        Species toAdd = null;
        GameObject newObject = null;
        void InitNewSpecies(bool isProducer)
        {
            Uninspect();

            // TODO: temporary - use animation instead
            if (!shown)
            {
                shown = true;
                spawnArea.Show();
                nodelink.transform.localScale *= .5f;
            }

            toAdd = new Species(idxCounter, isProducer);
            inspector.SetSize(toAdd.BodySize);
            inspector.SetGreed(toAdd.Greediness);
            SetNewSpeciesObject();
        }
        void SetNewSpeciesObject()
        {
            if (newObject != null)
                Destroy(newObject);
            newObject = GenerateSpeciesObject(toAdd);
            spawnArea.PrepareSpawn(newObject);
        }
        static GameObject GenerateSpeciesObject(Species s)
        {
            GameObject newObject = GameManager.Instance.GetSpeciesObject(s.IsProducer, s.BodySize, s.Greediness, s.RandomSeed);
            return newObject;
        }

        void TryAddNewSpecies()
        {
            if (shown)
            {
                nodelink.AddNode(toAdd.Idx, newObject);
                model.AddSpecies(toAdd.Idx);
                if (toAdd.IsProducer)
                {
                    model.SetSpeciesAsProducer(toAdd.Idx);
                }
                else
                {
                    model.SetSpeciesAsConsumer(toAdd.Idx);
                }
                // model.SetSpeciesBodySize(toAdd.Idx, toAdd.GetKg());
                model.SetSpeciesBodySize(toAdd.Idx, toAdd.BodySize);
                model.SetSpeciesGreediness(toAdd.Idx, toAdd.Greediness);

                allSpecies[toAdd.Idx] = toAdd;
                Inspect(toAdd.Idx);
                idxCounter += 1;

                // TODO: end game if enough species are added

                toAdd = null;
                newObject = null;
            }

        }
        Species inspected;
        void Inspect(int idx)
        {
            if (inspected != allSpecies[idx])
            {
                inspected = allSpecies[idx];
                // inspector.SetSize(inspected.BodySize);
                // inspector.SetGreed(inspected.Greediness);

                nodelink.FocusNode(idx);

                if (shown)
                {
                    spawnArea.Hide();
                    nodelink.transform.localScale *= 2;
                    shown = false;
                }
            }
        }
        void Uninspect()
        {
            if (shown)
            {
                spawnArea.Hide();
                nodelink.transform.localScale *= 2;
                shown = false;
            }
            else if (inspected != null)
            {
                inspected = null;
                // inspector.SetSize(toAdd.BodySize);
                // inspector.SetGreed(toAdd.Greediness);

                nodelink.Unfocus();
            }
        }
        void ChangeSize(float size)
        {
            if (inspected == null)
            {
                if (shown)
                {
                    toAdd.BodySize = size;
                    SetNewSpeciesObject();
                }
            }
            else
            {
                inspected.BodySize = size;
                // nodelink.ColourNode(inspected.Idx, inspected.GetColor());
                nodelink.ReshapeNode(inspected.Idx, GenerateSpeciesObject(inspected));
                // model.SetSpeciesBodySize(inspected.Idx, inspected.GetKg());
                model.SetSpeciesBodySize(inspected.Idx, inspected.BodySize);
            }
        }
        void ChangeGreed(float greed)
        {
            if (inspected == null)
            {
                if (shown)
                {
                    toAdd.Greediness = greed;
                    SetNewSpeciesObject();
                }
            }
            else
            {
                inspected.Greediness = greed;
                // nodelink.ColourNode(inspected.Idx, inspected.GetColor());
                nodelink.ReshapeNode(inspected.Idx, GenerateSpeciesObject(inspected));
                model.SetSpeciesGreediness(inspected.Idx, greed);
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
        // void EndGame()
        // {
        //     // TODO: stop the game here, and look for height, loops, omnivory
        //     print("game over!");
        // }

        
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
