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
            inspector.OnUnincubated +=        ()=> nodelink.MoveMiddle();
            inspector.OnSpawned +=         (i,g)=> model.AddSpecies(i);
            inspector.OnSpawned +=         (i,g)=> nodelink.AddNode(i,g);
            inspector.OnUnspawned +=         (i)=> model.RemoveSpecies(i);
            inspector.OnUnspawned +=         (i)=> nodelink.RemoveNode(i);
            inspector.OnGameFinished +=       ()=> FinishGame();
            inspector.OnMainMenu +=           ()=> BackToMenu();

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

            // TODO: add May's (or Tang's) complexity criteria here, directly
            model.OnCalculated +=             ()=> status.FillStars(model.Feasible, model.Stable, model.Nonreactive);
            // model.OnCalculated +=             ()=> nodelink.ResizeNodes(i=> model.GetAbundance(i));
            // model.OnCalculated +=             ()=> nodelink.ReflowLinks((i,j)=> model.GetFlux(i,j));
            model.OnEndangered +=            (i)=> nodelink.FlashNode(i);
            model.OnRescued +=               (i)=> nodelink.IdleNode(i);

            status.OnMenu +=                  ()=> GameManager.Instance.ShowLevelCard();
            status.OnUndo +=                  ()=> inspector.UnspawnLast();

            inspector.ConstrainTypes(GameManager.Instance.NumProducers, GameManager.Instance.NumConsumers);
        }

        void TryAddNewSpecies()
        {
            if (inspector.Dragging)
            {
                inspector.Spawn();
            }
        }
        void FinishGame()
        {
            bool passed = true;
            if (!model.Feasible)
            {
                passed = false;
                status.ShowErrorMessage("Not every species can coexist");
            }
            if (nodelink.LongestLoop() < GameManager.Instance.MinLoop)
            {
                passed = false;
                status.ShowErrorMessage("No loop longer than " + GameManager.Instance.MinLoop + " exists");
            }
            if (nodelink.MaxChainLength() < GameManager.Instance.MinChain)
            {
                passed = false;
                status.ShowErrorMessage("No chain taller than " + GameManager.Instance.MinChain + " exists");
            }
            if (passed)
            {
                status.Finish();
                nodelink.Finish();
                inspector.Finish();
            }
        }
        void BackToMenu()
        {
            int score = 0;
            if (model.Feasible)
                score += 1;
            if (model.Stable)
                score += 1;
            if (model.Nonreactive)
                score += 1;

            GameManager.Instance.ReturnToMenu(score);
        }
    }
}
