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
        [SerializeField] UI.StatusBar status;
        [SerializeField] UI.Tutorial tutorial;
        [SerializeField] UI.MoveRecorder recorder;

        void Start()
        {
            ///////////////////////////////////
            // hook up events between objects

            inspector.OnIncubated +=        ()=> nodelink.FullUnfocus();
            inspector.OnUnincubated +=      ()=> print("perhaps move left and right again?");
            inspector.OnSpawned +=         (i)=> nodelink.AddNode(i);
            inspector.OnSpawned +=         (i)=> model.AddSpecies(i);
            inspector.OnDespawned +=       (i)=> nodelink.RemoveNode(i);
            inspector.OnDespawned +=       (i)=> model.RemoveSpecies(i);
            inspector.OnDespawned +=       (i)=> status.RemoveIdx(i);
            inspector.OnShaped +=        (i,g)=> nodelink.ShapeNode(i,g);
            inspector.OnIsProducerSet += (i,x)=> nodelink.SetIfNodeCanBeTarget(i,!x);
            inspector.OnIsProducerSet += (i,x)=> model.SetSpeciesIsProducer(i,x);
            inspector.OnIsProducerSet += (i,x)=> status.AddType(i,x);
            inspector.OnSizeSet +=       (i,x)=> model.SetSpeciesBodySize(i,x);
            inspector.OnGreedSet +=      (i,x)=> model.SetSpeciesInterference(i,x);
            inspector.OnUserSpawned +=     (i)=> nodelink.FocusNode(i);

            nodelink.OnNodeFocused += (i)=> inspector.InspectSpecies(i);
            nodelink.OnUnfocused +=    ()=> inspector.Uninspect();
            nodelink.OnEmptyPressed += ()=> inspector.Unincubate();
            // nodelink.OnEmptyPressed += ()=> status.ShowHelp(false);
            nodelink.OnConstraints +=  ()=> status.DisplayDisjoint(nodelink.Disjoint);
            nodelink.OnConstraints +=  ()=> status.DisplayNumEdges(nodelink.NumEdges);
            nodelink.OnConstraints +=  ()=> status.DisplayMaxChain(nodelink.MaxChain);
            nodelink.OnConstraints +=  ()=> status.DisplayMaxLoop(nodelink.MaxLoop);

            model.OnEndangered += (i)=> nodelink.FlashNode(i);
            model.OnRescued +=    (i)=> nodelink.UnflashNode(i);
            model.OnEquilibrium += ()=> nodelink.ResizeNodes(i=> model.GetScaledAbundance(i));
            model.OnEquilibrium += ()=> nodelink.ReflowLinks((i,j)=> model.GetScaledFlux(i,j));
            // model.OnEquilibrium += ()=> status.DisplayScore(model.TotalFlux);
            model.OnEquilibrium += ()=> status.DisplayScore(model.Complexity);
            model.OnEquilibrium += ()=> status.DisplayFeastability(model.Feasible, model.Stable);

            status.OnProducersAvailable += (b)=> inspector.SetProducerAvailability(b);
            status.OnConsumersAvailable += (b)=> inspector.SetConsumerAvailability(b);
            status.OnLevelCompleted     +=  ()=> CompleteLevel();

            inspector.OnSpawned +=         (i)=> atEquilibrium = false;
            inspector.OnSpawned +=         (i)=> graphSolved = false;
            inspector.OnDespawned +=       (i)=> atEquilibrium = false;
            inspector.OnDespawned +=       (i)=> graphSolved = false;
            nodelink.OnLinked +=            ()=> atEquilibrium = false;
            nodelink.OnLinked +=            ()=> graphSolved = false;
            inspector.OnIsProducerSet += (i,x)=> atEquilibrium = false;
            inspector.OnSizeSet +=       (i,x)=> atEquilibrium = false;
            inspector.OnGreedSet +=      (i,x)=> atEquilibrium = false;

            inspector.OnUserSpawned +=           (i)=> recorder.SpeciesSpawn(i, inspector.Respawn, inspector.Despawn);
            inspector.OnUserDespawned +=         (i)=> recorder.SpeciesDespawn(i, inspector.Respawn, inspector.Despawn);
            nodelink.OnUserLinked +=           (i,j)=> recorder.InteractionAdded(i, j, nodelink.AddLink, nodelink.RemoveLink);
            nodelink.OnUserUnlinked +=         (i,j)=> recorder.InteractionRemoved(i, j, nodelink.AddLink, nodelink.RemoveLink);
            inspector.OnUserIsProducerSet += (i,x,y)=> recorder.TypeSet(i, x, y, inspector.SetIsProducer);
            inspector.OnUserSizeSet +=       (i,x,y)=> recorder.SizeSet(i, x, y, inspector.SetSize);
            inspector.OnUserGreedSet +=      (i,x,y)=> recorder.GreedSet(i, x, y, inspector.SetGreed);

            recorder.OnSpeciesUndone +=          (i)=> nodelink.SwitchFocus(i);



            ///////////////////
            // set up level

            var level = GameManager.Instance.PlayedLevel;
            if (level == null)
            {
                level = GameManager.Instance.GetNewLevel();
                // tutorial.gameObject.SetActive(true);
            }
            status.ConstrainFromLevel(level);

            status.AllowUpdateWhen(()=> atEquilibrium &&
                                        !model.IsCalculating &&
                                        graphSolved &&
                                        !nodelink.IsCalculating &&
                                        !tutorial.Teaching);

            for (int i=0; i<level.Details.numSpecies; i++)
            {
                int newIdx = inspector.SpawnNotIncubated(
                    level.Details.plants[i],
                    level.Details.sizes[i],
                    level.Details.greeds[i],
                    level.Details.randomSeeds[i],
                    level.Details.sizeEditable,
                    level.Details.greedEditable);

                inspector.SetSpeciesRemovable(i, false);
                if (newIdx != i)
                    throw new Exception("inspector not adding indices contiguously");
            }
            for (int ij=0; ij<level.Details.numInteractions; ij++)
            {
                int i = level.Details.resources[ij];
                int j = level.Details.consumers[ij];
                nodelink.AddLink(i, j);
                nodelink.SetIfLinkRemovable(i, j, false);
            }
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

        void CompleteLevel()
        {
            inspector.Hide();
            nodelink.Freeze();
            status.Confetti();
            recorder.Record();
            GameManager.Instance.SavePlayedLevel(status.NumStars, model.TotalFlux);
        }
    }
}