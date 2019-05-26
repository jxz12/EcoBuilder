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
        [SerializeField] UI.StatusBar status;
        [SerializeField] NodeLink.NodeLink nodelink;
        [SerializeField] Model.Model model;

        void Start()
        {
            inspector.OnSpawned +=         (i,g)=> model.AddSpecies(i);
            inspector.OnSpawned +=         (i,g)=> nodelink.AddNode(i,g);
            inspector.OnProducerSet +=     (i,b)=> model.SetSpeciesProduction(i,b);
            inspector.OnProducerSet +=     (i,b)=> nodelink.SetNodeAsSourceOnly(i,b);
            inspector.OnSizeSet +=         (i,x)=> model.SetSpeciesBodySize(i,x);
            inspector.OnGreedSet +=        (i,x)=> model.SetSpeciesGreediness(i,x);
            inspector.OnIncubated +=          ()=> nodelink.MoveLeft();
            inspector.OnUnincubated +=        ()=> nodelink.MoveRight();

            nodelink.OnNodeFocused +=        (i)=> inspector.InspectSpecies(i);
            nodelink.OnUnfocused +=           ()=> inspector.Uninspect();
            nodelink.OnLinkAdded +=        (i,j)=> model.AddInteraction(i,j);
            nodelink.OnLinkRemoved +=      (i,j)=> model.RemoveInteraction(i,j);
            nodelink.OnDroppedOn +=           ()=> TryAddNewSpecies();
            // nodelink.OnLaplacianUnsolvable += ()=> print("unsolvable");
            // nodelink.OnLaplacianSolvable +=   ()=> print("solvable");

            model.OnCalculated +=             ()=> CalculateScore();
            // model.OnCalculated +=             ()=> ResizeNodes();
            // model.OnEndangered +=            (i)=> nodelink.FlashNode(i);
            // model.OnRescued +=               (i)=> nodelink.IdleNode(i);
            model.OnEndangered +=            (i)=> print("neg: " + i);
            model.OnRescued +=               (i)=> print("pos: " + i);

            // status.OnMenu +=                  ()=> print("MENU");
            // status.OnUndo +=                  ()=> UndoMove();
            // status.OnRedo +=                  ()=> RedoMove();
        }

        void TryAddNewSpecies()
        {
            if (inspector.Dragging)
            {
                inspector.Spawn();
                // nodelink.FocusNode((int)inspector.InspectedIdx);
            }
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
