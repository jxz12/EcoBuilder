using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace EcoBuilder
{
    public class GameMaster : MonoBehaviour
    {
        [SerializeField] UI.Inspector inspector;
        [SerializeField] UI.Spawner spawner;
        [SerializeField] UI.StatusBar status;
        [SerializeField] NodeLink.NodeLink nodelink;
        [SerializeField] Model.Model model;

        class Species
        {
            public int Idx { get; private set; }
            public bool IsProducer { get; private set; }
            public Species(int idx, bool isProducer)
            {
                Idx = idx;
                IsProducer = isProducer;
                RandomSeed = UnityEngine.Random.Range(0, int.MaxValue);
            }
            public string Name { get; set; }
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

            nodelink.OnNodeHeld +=         (i)=> Inspect(i);
            nodelink.OnNodeClicked +=      (j)=> MakeInteraction(j);
            nodelink.OnEmptyClicked +=      ()=> Uninspect();
            nodelink.LaplacianUnsolvable += ()=> print("unsolvable");
            nodelink.LaplacianSolvable +=   ()=> print("solvable");
            nodelink.OnDroppedOn +=         ()=> TrySpawnNewSpecies();

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

        Species toSpawn = null;
        public void IncubateNewSpecies(bool isProducer)
        {
            if (toSpawn == null)
            {
                toSpawn = new Species(idxCounter, isProducer);
                inspector.SetSize(toSpawn.BodySize);
                inspector.SetGreed(toSpawn.Greediness);

                Incubate();
                inspected = null;
            }
            else
            {
                RefreshIncubated();
            }
        }
        public void RefreshIncubated()
        {
            Species old = toSpawn;
            toSpawn = new Species(old.Idx, old.IsProducer);
            toSpawn.BodySize = old.BodySize;
            toSpawn.Greediness = old.Greediness;

            Incubate();
            inspected = null;
        }
        public void Incubate()
        {
            GameObject toIncubate = spawner.GenerateObject(toSpawn.IsProducer, toSpawn.BodySize, toSpawn.Greediness, toSpawn.RandomSeed);
            toSpawn.Name = toIncubate.name;
            inspector.SetName(toSpawn.Name);
            spawner.IncubateSpecies(toIncubate);
        }

        // TODO: make this actually sense a drag from Spawner
        public void TrySpawnNewSpecies()
        {
            if (toSpawn == null)
                return;

            var newObject = spawner.TakeIncubated();
            nodelink.AddNode(toSpawn.Idx, newObject);
            model.AddSpecies(toSpawn.Idx);
            if (toSpawn.IsProducer)
            {
                model.SetSpeciesAsProducer(toSpawn.Idx);
            }
            else
            {
                model.SetSpeciesAsConsumer(toSpawn.Idx);
            }
            // model.SetSpeciesBodySize(toSpawn.Idx, toSpawn.GetKg());
            model.SetSpeciesBodySize(toSpawn.Idx, toSpawn.BodySize);
            model.SetSpeciesGreediness(toSpawn.Idx, toSpawn.Greediness);


            allSpecies[toSpawn.Idx] = toSpawn;
            Inspect(toSpawn.Idx);
            // GetComponent<Animator>().SetTrigger("Spawn");

            // TODO: end game if enough species are added
            toSpawn = null;
            idxCounter += 1;
        }
        Species inspected;
        public void Inspect(int idx)
        {
            if (inspected != allSpecies[idx])
            {
                if (toSpawn != null)
                {
                    toSpawn = null;
                    Destroy(spawner.TakeIncubated());
                }
                inspected = allSpecies[idx];
                inspector.SetSize(inspected.BodySize);
                inspector.SetGreed(inspected.Greediness);
                inspector.SetName(inspected.Name);

                nodelink.FocusNode(idx);

                toSpawn = null;
                GetComponent<Animator>().SetTrigger("Inspect");
            }
        }
        void Uninspect()
        {
            if (toSpawn != null)
            {
                toSpawn = null;
                Destroy(spawner.TakeIncubated());
                GetComponent<Animator>().SetTrigger("Uninspect");
            }
            else if (inspected != null)
            {
                inspected = null;
                nodelink.Unfocus();
                GetComponent<Animator>().SetTrigger("Uninspect");
            }
        }
        void ChangeSize(float size)
        {
            if (toSpawn != null)
            {
                toSpawn.BodySize = size;
                Incubate();
            }
            else if (inspected != null)
            {
                inspected.BodySize = size;
                // nodelink.ColourNode(inspected.Idx, inspected.GetColor());
                nodelink.ReshapeNode(inspected.Idx, spawner.GenerateObject(inspected.IsProducer, inspected.BodySize, inspected.Greediness, inspected.RandomSeed));
                // model.SetSpeciesBodySize(inspected.Idx, inspected.GetKg());
                model.SetSpeciesBodySize(inspected.Idx, inspected.BodySize);
            }
        }
        void ChangeGreed(float greed)
        {
            if (toSpawn != null)
            {
                toSpawn.Greediness = greed;
                Incubate();
            }
            else if (inspected != null)
            {
                inspected.Greediness = greed;
                // nodelink.ColourNode(inspected.Idx, inspected.GetColor());
                nodelink.ReshapeNode(inspected.Idx, spawner.GenerateObject(inspected.IsProducer, inspected.BodySize, inspected.Greediness, inspected.RandomSeed));
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
