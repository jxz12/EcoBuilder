using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace EcoBuilder
{
    // this class handles communication between all the top level components
    public class GameMaster : MonoBehaviour
    {
        [SerializeField] UI.Inspector inspector;
        [SerializeField] UI.Spawner spawner;
        [SerializeField] UI.StatusBar status;
        [SerializeField] NodeLink.NodeLink nodelink;
        [SerializeField] Model.Model model;

        int idxCounter = 0;

        void Start()
        {
            inspector.OnSizeChanged +=   (i,x)=> TransferSize(x);
            inspector.OnGreedChanged +=  (i,x)=> TransferGreed(x);

            nodelink.OnNodeHeld +=         (i)=> Inspect(i);
            nodelink.OnNodeClicked +=      (j)=> MakeInteraction(j);
            nodelink.OnEmptyClicked +=      ()=> Uninspect();
            nodelink.LaplacianUnsolvable += ()=> print("unsolvable");
            nodelink.LaplacianSolvable +=   ()=> print("solvable");
            // nodelink.OnDroppedOn +=         ()=> TrySpawnNewSpecies();

            model.OnCalculated +=           ()=> CalculateScore();
            // model.OnCalculated +=           ()=> ResizeNodes();
            // model.OnEndangered +=          (i)=> nodelink.FlashNode(i);
            // model.OnRescued +=             (i)=> nodelink.IdleNode(i);
            model.OnEndangered +=          (i)=> print("neg: " + i);
            model.OnRescued +=             (i)=> print("pos: " + i);

            spawner.OnIncubated += 

            // status.OnMenu +=                ()=> print("MENU");
            // status.OnUndo +=                ()=> UndoMove();
            // status.OnRedo +=                ()=> RedoMove();
        }

        // public void IncubateNewSpecies(bool isProducer)
        // {
        //     if (toSpawn == null)
        //     {
        //         toSpawn = new Species(idxCounter, isProducer);
        //         inspector.SetSize(toSpawn.BodySize);
        //         inspector.SetGreed(toSpawn.Greediness);

        //         Incubate();
        //         inspected = null;
        //     }
        //     else
        //     {
        //         RefreshIncubated();
        //     }
        // }
        // public void RefreshIncubated()
        // {
        //     Species old = toSpawn;
        //     toSpawn = new Species(old.Idx, old.IsProducer);
        //     toSpawn.BodySize = old.BodySize;
        //     toSpawn.Greediness = old.Greediness;

        //     Incubate();
        //     inspected = null;
        // }
        // public void Incubate()
        // {
        //     GameObject toIncubate = spawner.GenerateObject(toSpawn.IsProducer, toSpawn.BodySize, toSpawn.Greediness, toSpawn.RandomSeed);
        //     toSpawn.Name = toIncubate.name;
        //     inspector.SetName(toSpawn.Name);
        //     spawner.IncubateSpecies(toIncubate);
        // }

        // // TODO: make this actually sense a drag from Spawner
        // public void TrySpawnNewSpecies()
        // {
        //     if (toSpawn == null)
        //         return;

        //     var newObject = spawner.TakeIncubated();
        //     nodelink.AddNode(toSpawn.Idx, newObject);
        //     model.AddSpecies(toSpawn.Idx);
        //     if (toSpawn.IsProducer)
        //     {
        //         model.SetSpeciesAsProducer(toSpawn.Idx);
        //     }
        //     else
        //     {
        //         model.SetSpeciesAsConsumer(toSpawn.Idx);
        //     }
        //     // model.SetSpeciesBodySize(toSpawn.Idx, toSpawn.GetKg());
        //     model.SetSpeciesBodySize(toSpawn.Idx, toSpawn.BodySize);
        //     model.SetSpeciesGreediness(toSpawn.Idx, toSpawn.Greediness);


        //     allSpecies[toSpawn.Idx] = toSpawn;
        //     Inspect(toSpawn.Idx);
        //     // GetComponent<Animator>().SetTrigger("Spawn");

        //     // TODO: end game if enough species are added
        //     toSpawn = null;
        //     idxCounter += 1;
        // }
        // Species inspected;
        // public void Inspect(int idx)
        // {
        //     if (inspected != allSpecies[idx])
        //     {
        //         if (toSpawn != null)
        //         {
        //             toSpawn = null;
        //             Destroy(spawner.TakeIncubated());
        //         }
        //         inspected = allSpecies[idx];
        //         inspector.SetSize(inspected.BodySize);
        //         inspector.SetGreed(inspected.Greediness);
        //         inspector.SetName(inspected.Name);

        //         nodelink.FocusNode(idx);

        //         toSpawn = null;
        //         GetComponent<Animator>().SetTrigger("Inspect");
        //     }
        // }
        // void Uninspect()
        // {
        //     if (toSpawn != null)
        //     {
        //         toSpawn = null;
        //         Destroy(spawner.TakeIncubated());
        //         GetComponent<Animator>().SetTrigger("Uninspect");
        //     }
        //     else if (inspected != null)
        //     {
        //         inspected = null;
        //         nodelink.Unfocus();
        //         GetComponent<Animator>().SetTrigger("Uninspect");
        //     }
        // }
        void TransferSize(int idx, float kg)
        {
            model.SetSpeciesBodySize(idx, kg);
        }
        void TransferGreed(int idx, float greed)
        {
            model.SetSpeciesGreediness(idx, greed);
        }

        void MakeInteraction(int res, int con)
        {
            // if (res != con)
            // {
            //     if (!inspected.resources.Contains(res))
            //     {
            //         inspected.resources.Add(res);
            //         nodelink.AddLink(res, con);
            //         model.AddInteraction(res, con);
            //     }
            //     else
            //     {
            //         inspected.resources.Remove(res);
            //         nodelink.RemoveLink(res, con);
            //         model.RemoveInteraction(res, con);
            //     }
            // }
        }

        
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
