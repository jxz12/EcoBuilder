using UnityEngine;
using System;

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
            ////////////////////////
            // hook up events

            // inspector.OnIncubated +=          ()=> nodelink.MoveLeft();
            // inspector.OnUnincubated +=        ()=> nodelink.MoveMiddle();
            inspector.OnSpawned +=         (i,g)=> model.AddSpecies(i);
            inspector.OnSpawned +=         (i,g)=> nodelink.AddNode(i,g);
            inspector.OnIsProducerSet +=   (i,b)=> model.SetSpeciesIsProducer(i,b);
            inspector.OnIsProducerSet +=   (i,b)=> nodelink.SetIfNodeCanBeTarget(i,!b);
            inspector.OnSizeSet +=         (i,x)=> model.SetSpeciesBodySize(i,x);
            inspector.OnGreedSet +=        (i,x)=> model.SetSpeciesGreediness(i,x);

            nodelink.OnNodeFocused +=        (i)=> inspector.InspectSpecies(i);
            nodelink.OnUnfocused +=           ()=> inspector.Uninspect();
            nodelink.OnLinkAdded +=        (i,j)=> model.AddInteraction(i,j);
            nodelink.OnLinkRemoved +=      (i,j)=> model.RemoveInteraction(i,j);
            nodelink.OnDroppedOn +=           ()=> inspector.TrySpawnIncubated();
            ///////////////////////////
            nodelink.OnConstraints +=         ()=> status.DisplayNumEdges(nodelink.NumEdges);
            nodelink.OnConstraints +=         ()=> status.DisplayMaxChain(nodelink.MaxChain);
            nodelink.OnConstraints +=         ()=> status.DisplayMaxLoop(nodelink.MaxLoop);


            model.OnEndangered +=            (i)=> nodelink.FlashNode(i);
            model.OnRescued +=               (i)=> nodelink.IdleNode(i);
            ///////////////////////////
            // model.OnEquilibrium +=            ()=> status.FillStars(model.Feasible, model.Stable, model.Nonreactive);
            model.OnEquilibrium +=            ()=> nodelink.ResizeNodes(i=> model.GetAbundance(i));
            model.OnEquilibrium +=            ()=> nodelink.ReflowLinks((i,j)=> model.GetFlux(i,j));
            ///////////////////////////
            print("TODO: move into inspector, with activation on strong-focus");
            nodelink.OnNodeRemoved +=        (i)=> inspector.UnspawnSpecies(i);
            nodelink.OnNodeRemoved +=        (i)=> model.RemoveSpecies(i);

            status.OnLevelFinish +=           ()=> FinishLevel();
            status.OnLevelReplay +=           ()=> print("TODO:");
            status.OnLevelNext +=             ()=> print("TODO:");
            status.OnBackToMenu +=            ()=> print("TODO:");



            ///////////////////
            // set up level

            var level = GameManager.Instance.DefaultLevel; // only use for dev

            status.SlotInLevel(level);
            status.ConstrainNumEdges(level.Details.minEdges);
            status.ConstrainMaxChain(level.Details.minChain);
            status.ConstrainMaxLoop(level.Details.minLoop);

            for (int i=0; i<level.Details.numSpecies; i++)
            {
                int newIdx = inspector.SpawnNotIncubated(level.Details.plants[i],
                                                         level.Details.sizes[i],
                                                         level.Details.greeds[i],
                                                         level.Details.randomSeeds[i]);
                if (newIdx != i)
                    throw new Exception("inspector not adding indices contiguously");

                nodelink.SetIfNodeRemovable(i, false);
            }
            for (int ij=0; ij<level.Details.numInteractions; ij++)
            {
                int i = level.Details.resources[ij];
                int j = level.Details.consumers[ij];
                nodelink.AddLink(i, j);
                nodelink.SetIfLinkRemovable(i, j, false);
            }

            inspector.ConstrainTypes(level.Details.numProducers, level.Details.numConsumers);
            
            // TODO: this is a little hacky?
            nodelink.Unfocus();
        }

        void FinishLevel()
        {
            // GameManager.Instance.SavePlayedLevel(numStars);
            print("TODO: make animations do things and confetti");
        }
    }
}
