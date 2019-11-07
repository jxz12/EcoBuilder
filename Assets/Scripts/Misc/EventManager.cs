﻿using UnityEngine;
using System;

namespace EcoBuilder
{
    // this class handles communication between all the top level components
    public class EventManager : MonoBehaviour
    {
        [SerializeField] NodeLink.NodeLink nodelink;
        [SerializeField] Model.Model model;

        [SerializeField] UI.Inspector inspector;
        [SerializeField] UI.Score score;
        [SerializeField] UI.MoveRecorder recorder;

        void Start()
        {
            ///////////////////////////////////
            // hook up events between objects

            inspector.OnIncubated +=        ()=> nodelink.FullUnfocus();
            inspector.OnIncubated +=        ()=> nodelink.MoveHorizontal(-.5f); // TODO: magic number
            inspector.OnUnincubated +=      ()=> nodelink.MoveHorizontal(0);
            inspector.OnSpawned +=         (i)=> nodelink.AddNode(i);
            inspector.OnSpawned +=         (i)=> model.AddSpecies(i);
            inspector.OnDespawned +=       (i)=> nodelink.RemoveNode(i);
            inspector.OnDespawned +=       (i)=> model.RemoveSpecies(i);
            inspector.OnDespawned +=       (i)=> score.RemoveIdx(i);
            inspector.OnShaped +=        (i,g)=> nodelink.ShapeNode(i,g);
            inspector.OnIsProducerSet += (i,x)=> nodelink.SetIfNodeCanBeTarget(i,!x);
            inspector.OnIsProducerSet += (i,x)=> model.SetSpeciesIsProducer(i,x);
            inspector.OnIsProducerSet += (i,x)=> score.AddType(i,x);
            inspector.OnSizeSet +=       (i,x)=> model.SetSpeciesBodySize(i,x);
            inspector.OnGreedSet +=      (i,x)=> model.SetSpeciesInterference(i,x);
            inspector.OnUserSpawned +=     (i)=> nodelink.FocusNode(i);

            nodelink.OnNodeFocused += (i)=> inspector.InspectSpecies(i);
            nodelink.OnUnfocused +=    ()=> inspector.Uninspect();
            nodelink.OnEmptyPressed += ()=> inspector.Unincubate();
            nodelink.OnConstraints +=  ()=> score.DisplayDisjoint(nodelink.Disjoint);
            nodelink.OnConstraints +=  ()=> score.DisplayNumEdges(nodelink.NumEdges);
            nodelink.OnConstraints +=  ()=> score.DisplayMaxChain(nodelink.MaxChain);
            nodelink.OnConstraints +=  ()=> score.DisplayMaxLoop(nodelink.MaxLoop);

            model.OnEndangered += (i)=> nodelink.FlashNode(i);
            model.OnRescued +=    (i)=> nodelink.UnflashNode(i);
            // model.OnEquilibrium += ()=> nodelink.ResizeNodes(i=> model.GetScaledAbundance(i));
            model.OnEquilibrium += ()=> nodelink.RehealthBars(i=> model.GetScaledAbundance(i));
            model.OnEquilibrium += ()=> nodelink.ReflowLinks((i,j)=> model.GetScaledFlux(i,j));
            model.OnEquilibrium += ()=> score.DisplayScore(model.NormalisedComplexity);
            model.OnEquilibrium += ()=> score.DisplayFeastability(model.Feasible, model.Stable);

            score.OnProducersAvailable += (b)=> inspector.SetProducerAvailability(b);
            score.OnConsumersAvailable += (b)=> inspector.SetConsumerAvailability(b);
            score.OnLevelCompleted +=      ()=> inspector.Hide();
            score.OnLevelCompleted +=      ()=> nodelink.Freeze();
            score.OnLevelCompleted +=      ()=> recorder.Record();

            inspector.OnSpawned +=         (i)=> atEquilibrium = false;
            inspector.OnSpawned +=         (i)=> graphSolved = false;
            inspector.OnDespawned +=       (i)=> atEquilibrium = false;
            inspector.OnDespawned +=       (i)=> graphSolved = false;
            inspector.OnIsProducerSet += (i,x)=> atEquilibrium = false;
            inspector.OnSizeSet +=       (i,x)=> atEquilibrium = false;
            inspector.OnGreedSet +=      (i,x)=> atEquilibrium = false;
            nodelink.OnLinked +=            ()=> atEquilibrium = false;
            nodelink.OnLinked +=            ()=> graphSolved = false;

            inspector.OnUserSpawned +=      (i)=> recorder.SpeciesSpawn(i, inspector.Respawn, inspector.Despawn);
            inspector.OnUserDespawned +=    (i)=> recorder.SpeciesDespawn(i, inspector.Respawn, inspector.Despawn);
            inspector.OnUserSizeSet +=  (i,x,y)=> recorder.SizeSet(i, x, y, inspector.SetSize);
            inspector.OnUserGreedSet += (i,x,y)=> recorder.GreedSet(i, x, y, inspector.SetGreed);
            nodelink.OnUserLinked +=      (i,j)=> recorder.InteractionAdded(i, j, nodelink.AddLink, nodelink.RemoveLink);
            nodelink.OnUserUnlinked +=    (i,j)=> recorder.InteractionRemoved(i, j, nodelink.AddLink, nodelink.RemoveLink);

            recorder.OnSpeciesUndone +=     (i)=> nodelink.SwitchFocus(i);
            recorder.OnSpeciesMemoryLeak += (i)=> nodelink.RemoveNodeCompletely(i);
            recorder.OnSpeciesMemoryLeak += (i)=> inspector.DespawnCompletely(i);

            nodelink.AddLandscape(GameManager.Instance.RandomLandscape());
            score.AllowUpdateWhen(()=> atEquilibrium &&
                                        !model.IsCalculating &&
                                        graphSolved &&
                                        !nodelink.IsCalculating); 

            var level = GameManager.Instance.PlayedLevel;
            if (level == null)
                return; // only for testing, should never happen in the wild

            for (int i=0; i<level.Details.numSpecies; i++)
            {
                inspector.SpawnNotIncubated(i,
                    level.Details.plants[i],
                    level.Details.sizes[i],
                    level.Details.greeds[i],
                    level.Details.randomSeeds[i],
                    level.Details.sizeEditable,
                    level.Details.greedEditable);

                inspector.SetSpeciesRemovable(i, false);
                nodelink.SetIfNodeRemovable(i, false);
            }
            for (int ij=0; ij<level.Details.numInteractions; ij++)
            {
                int i = level.Details.resources[ij];
                int j = level.Details.consumers[ij];
                nodelink.AddLink(i, j);
                nodelink.SetIfLinkRemovable(i, j, false);
            }
            GameManager.Instance.LoadTutorialIfNeeded(); // UGLY
        }

        bool atEquilibrium = true, graphSolved = true;
        void LateUpdate()
        {
            if (!graphSolved && !nodelink.IsCalculating)
            {
                graphSolved = true;
                #if UNITY_WEBGL
                    nodelink.ConstraintsSync();
                #else
                    nodelink.ConstraintsAsync();
                #endif
            }
            if (!atEquilibrium && !model.IsCalculating)
            {
                atEquilibrium = true;
                #if UNITY_WEBGL
                    model.EquilibriumSync(nodelink.GetTargets);
                #else
                    model.EquilibriumAsync(nodelink.GetTargets);
                #endif
            }
        }
    }
}
