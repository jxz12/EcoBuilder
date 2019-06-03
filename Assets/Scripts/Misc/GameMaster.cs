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
            inspector.OnIncubated +=          ()=> nodelink.MoveLeft();
            inspector.OnUnincubated +=        ()=> nodelink.MoveRight();
            inspector.OnSpawned +=         (i,g)=> model.AddSpecies(i);
            inspector.OnSpawned +=         (i,g)=> nodelink.AddNode(i,g);
            inspector.OnGameEnded +=          ()=> EndGame();

            inspector.OnProducerSet +=     (i,b)=> model.SetSpeciesIsProducer(i,b);
            inspector.OnProducerSet +=     (i,b)=> nodelink.SetNodeAsSourceOnly(i,b);
            inspector.OnSizeSet +=         (i,x)=> model.SetSpeciesBodySize(i,x);
            inspector.OnGreedSet +=        (i,x)=> model.SetSpeciesGreediness(i,x);

            nodelink.OnNodeFocused +=        (i)=> inspector.InspectSpecies(i);
            nodelink.OnUnfocused +=           ()=> inspector.Uninspect();
            nodelink.OnLinkAdded +=        (i,j)=> model.AddInteraction(i,j);
            nodelink.OnLinkRemoved +=      (i,j)=> model.RemoveInteraction(i,j);
            nodelink.OnDroppedOn +=           ()=> TryAddNewSpecies();
            nodelink.OnLaplacianUnsolvable += ()=> print("unsolvable");
            nodelink.OnLaplacianSolvable +=   ()=> print("solvable");

            model.OnCalculated +=             ()=> CalculateScore();
            model.OnEndangered +=            (i)=> nodelink.FlashNode(i);
            model.OnRescued +=               (i)=> nodelink.IdleNode(i);

            status.OnMenu +=                  ()=> GameManager.Instance.ShowLevelCard();
            status.OnUndo +=                  ()=> print(nodelink.LongestLoop());

            inspector.ConstrainTypes(GameManager.Instance.NumProducers, GameManager.Instance.NumConsumers);
        }

        void TryAddNewSpecies()
        {
            if (inspector.Dragging)
            {
                inspector.Spawn();
            }
        }
        void CalculateScore()
        {
            status.FillStars(model.Feasible, model.Stable, model.Nonreactive);

            // if (model.Feasible)
            //     print("flux: " + model.Flux);
            // TODO: add May's (or Tang's) complexity criteria here, directly
        }
        void EndGame()
        {
            // TODO: check for other constraints and set stars
            if (model.Feasible)
                GameManager.Instance.EndGame(1);
        }
    }
}
