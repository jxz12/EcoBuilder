﻿using System;
using UnityEngine;
using System.Collections.Generic;

namespace EcoBuilder
{
    // this class handles communication between all the top level components
    public class PlayManager : MonoBehaviour
    {
        [SerializeField] Model.Model model;
        [SerializeField] NodeLink.NodeLink nodelink;

        [SerializeField] UI.Inspector inspector;
        [SerializeField] UI.Constraints constraints;
        [SerializeField] UI.Recorder recorder;
        [SerializeField] UI.Score score;

        void Start()
        {
            ////////////////////////////////////
            // hook up events between objects //
            ////////////////////////////////////
            inspector.OnSpawned +=  (i,b,g)=> { nodelink.AddNode(i,g); nodelink.SetIfNodeCanBeTarget(i,!b); };
            inspector.OnSpawned +=  (i,b,g)=> model.AddSpecies(i,b);
            inspector.OnSpawned +=  (i,b,g)=> constraints.AddIdx(i,b);
            inspector.OnDespawned +=    (i)=> nodelink.RemoveNode(i);
            inspector.OnDespawned +=    (i)=> model.RemoveSpecies(i);
            inspector.OnDespawned +=    (i)=> constraints.RemoveIdx(i);
            inspector.OnSizeSet +=    (i,x)=> model.SetSpeciesBodySize(i,x);
            inspector.OnGreedSet +=   (i,x)=> model.SetSpeciesInterference(i,x);
            inspector.OnConflicted += (i,m)=> nodelink.OutlineNode(i, cakeslice.Outline.Colour.Red);
            inspector.OnConflicted += (i,m)=> nodelink.TooltipNode(i, m);
            inspector.OnUnconflicted += (i)=> nodelink.UnoutlineNode(i);
            inspector.OnUnconflicted += (i)=> nodelink.UntooltipNode(i);
            inspector.OnIncubated +=     ()=> nodelink.ForceUnfocus();
            inspector.OnIncubated +=     ()=> nodelink.MoveHorizontal(-.5f);
            inspector.OnUnincubated +=   ()=> nodelink.MoveHorizontal(0);

            nodelink.OnEmptyTapped += ()=> inspector.CancelIncubation();
            nodelink.OnEmptyTapped += ()=> inspector.HideStatusBars();

            ////////////////////////////
            // the above will always (but not exclusively) cause the below in the next three things
            // smelly because the second callback does something that should already be done
            inspector.OnUserSpawned += (i)=> nodelink.ForceFocus(i);
            nodelink.OnFocused +=      (i)=> inspector.InspectSpecies(i);

            inspector.OnUserDespawned += (i)=> nodelink.ForceUnfocus();
            nodelink.OnUnfocused +=      (i)=> inspector.Uninspect();
            ////////////////////////////

            // constraints
            nodelink.OnLayedOut += ()=> constraints.UpdateDisjoint(nodelink.NumComponents > 1);
            nodelink.OnLayedOut += ()=> constraints.DisplayEdge(nodelink.NumEdges);
            nodelink.OnLayedOut += ()=> constraints.DisplayChain(nodelink.MaxChain);
            nodelink.OnLayedOut += ()=> constraints.DisplayLoop(nodelink.MaxLoop);

            model.OnEquilibrium += ()=> inspector.DrawHealthBars(i=> model.GetNormalisedAbundance(i));
            model.OnEquilibrium += ()=> nodelink.ReflowLinks((i,j)=> model.GetNormalisedFlux(i,j));
            model.OnEquilibrium += ()=> constraints.UpdateFeasibility(model.Feasible);
            model.OnEquilibrium += ()=> constraints.UpdateStability(model.Stable);
            model.OnEndangered += (i)=> inspector.MakeSpeciesObjectExtinct(i);
            model.OnRescued +=    (i)=> inspector.MakeSpeciesObjectRescued(i);
            model.OnEndangered += (i)=> nodelink.FlashNode(i);
            model.OnRescued +=    (i)=> nodelink.UnflashNode(i);

            constraints.OnProducersAvailable += (b)=> inspector.SetProducerAvailability(b);
            constraints.OnConsumersAvailable += (b)=> inspector.SetConsumerAvailability(b);
            constraints.OnChainHovered +=        ()=> nodelink.OutlineChain(cakeslice.Outline.Colour.Red);
            constraints.OnLoopHovered +=         ()=> nodelink.OutlineLoop(cakeslice.Outline.Colour.Red);
            constraints.OnChainUnhovered +=      ()=> nodelink.UnoutlineChainOrLoop();
            constraints.OnLoopUnhovered +=       ()=> nodelink.UnoutlineChainOrLoop();

            inspector.OnUserSpawned +=      (i)=> recorder.SpeciesSpawn(i, inspector.Respawn, inspector.Despawn);
            inspector.OnUserDespawned +=    (i)=> recorder.SpeciesDespawn(i, inspector.Respawn, inspector.Despawn);
            inspector.OnUserSizeSet +=  (i,x,y)=> recorder.SizeSet(i, x, y, inspector.SetSize);
            inspector.OnUserGreedSet += (i,x,y)=> recorder.GreedSet(i, x, y, inspector.SetGreed);
            nodelink.OnUserLinked +=      (i,j)=> recorder.InteractionAdded(i, j, nodelink.AddLink, nodelink.RemoveLink);
            nodelink.OnUserUnlinked +=    (i,j)=> recorder.InteractionRemoved(i, j, nodelink.AddLink, nodelink.RemoveLink);

            recorder.OnSpeciesTraitsChanged += (i)=> nodelink.ForceFocus(i);
            recorder.OnSpeciesMemoryLeak +=    (i)=> nodelink.RemoveNodeCompletely(i);
            recorder.OnSpeciesMemoryLeak +=    (i)=> inspector.DespawnCompletely(i);

            // these are necessary to give the model adjacency info
            model.AttachAdjacency(nodelink.GetActiveTargets);
            inspector.OnSpawned += (i,b,g)=> model.TriggerSolve();
            inspector.OnDespawned +=   (i)=> model.TriggerSolve();
            inspector.OnSizeSet +=   (i,x)=> model.TriggerSolve();
            inspector.OnGreedSet +=  (i,x)=> model.TriggerSolve();
            nodelink.OnLinked +=        ()=> model.TriggerSolve();

            //////////////////////
            // initialise level //
            //////////////////////
            var details = GameManager.Instance.PlayedLevelDetails;

            inspector.HideSizeSlider(details.SizeSliderHidden);
            inspector.HideGreedSlider(details.GreedSliderHidden);
            inspector.AllowConflicts(details.ConflictsAllowed);
            nodelink.AllowSuperfocus = details.SuperfocusAllowed;
            nodelink.ConstrainTrophic = GameManager.Instance.ConstrainTrophic;
            nodelink.DragFromTarget = GameManager.Instance.ReverseDragDirection;

            constraints.ConstrainLeaf(details.NumProducers);
            constraints.ConstrainPaw(details.NumConsumers);
            constraints.ConstrainEdge(details.MinEdges);
            constraints.ConstrainChain(details.MinChain);
            constraints.ConstrainLoop(details.MinLoop);

            // add species
            for (int i=0; i<details.NumInitSpecies; i++)
            {
                inspector.SpawnNotIncubated(i,
                    details.Plants[i],
                    details.Sizes[i],
                    details.Greeds[i],
                    details.RandomSeeds[i],
                    details.Editables[i]);

                inspector.SetSpeciesRemovable(i, false);
                nodelink.OutlineNode(i, details.Editables[i]? cakeslice.Outline.Colour.Clear : cakeslice.Outline.Colour.Blue);
            }
            // add interactions
            for (int ij=0; ij<details.NumInitInteractions; ij++)
            {
                int i = details.Sources[ij];
                int j = details.Targets[ij];

                nodelink.AddLink(i, j);
                nodelink.SetIfLinkRemovable(i, j, false);
                nodelink.OutlineLink(i, j, cakeslice.Outline.Colour.Blue);
            }

            // set up scoring
            switch (GameManager.Instance.PlayedLevelDetails.Metric)
            {
            case LevelDetails.ScoreMetric.None:
                break;
            case LevelDetails.ScoreMetric.Standard:
                score.AttachScoreSource(()=> model.GetNormalisedComplexity() * details.MainMultiplier);
                break;
            case LevelDetails.ScoreMetric.Richness:
                score.AttachScoreSource(()=> model.GetNormalisedComplexity() * details.MainMultiplier);
                score.AttachScoreSource(()=> constraints.PawValue * details.AltMultiplier);
                break;
            case LevelDetails.ScoreMetric.Chain:
                score.AttachScoreSource(()=> model.GetNormalisedComplexity() * details.MainMultiplier);
                score.AttachScoreSource(()=> nodelink.MaxChain * details.AltMultiplier);
                break;
            case LevelDetails.ScoreMetric.Loop:
                score.AttachScoreSource(()=> model.GetNormalisedComplexity() * details.MainMultiplier);
                score.AttachScoreSource(()=> nodelink.MaxLoop * details.AltMultiplier);
                break;
            }
            score.AttachConstraintsSatisfied(()=> constraints.AllSatisfied());
            score.AttachScoreValidity(()=> nodelink.GraphLayedOut && model.EquilibriumSolved);

            score.SetStarThresholds(details.Metric, details.TargetScore1, details.TargetScore2);
            score.OnLevelCompletabled +=  ()=> GameManager.Instance.MakePlayedLevelFinishable();
            score.OnLevelCompletabled +=  ()=> nodelink.ForceUnfocus();
            score.OnLevelCompletabled +=  ()=> GameManager.Instance.HelpText.DelayThenShow(1, details.CompletedMessage);
            score.OnThreeStarsAchieved += ()=> nodelink.ForceUnfocus();
            score.OnThreeStarsAchieved += ()=> GameManager.Instance.HelpText.DelayThenShow(1, details.ThreeStarsMessage);

            GameManager.Instance.OnPlayedLevelFinished += FinishPlaythrough;
            nodelink.AddDropShadow(GameManager.Instance.TakePlanet(), -1.5f);

            GameManager.Instance.BeginPlayedLevel();
        }
        void OnDestroy()
        {
            // TODO: GameManager is null on quit?
            GameManager.Instance.OnPlayedLevelFinished -= FinishPlaythrough;
            GameManager.Instance.ReturnPlanet();
        }
        void FinishPlaythrough()
        {
            inspector.Finish();
            nodelink.Finish();
            recorder.Finish();
            score.Finish();
            constraints.Finish();

            GameManager.Instance.SetResultsScreen(score.HighestStars, score.HighestScore, model.GetMatrix(), recorder.GetActions());
            Destroy(gameObject);
        }

#if UNITY_EDITOR
        void Update()
        {
            // save a level for convenience
            if (Input.GetKeyDown(KeyCode.Q) &&
                Input.GetKeyDown(KeyCode.W) &&
                Input.GetKeyDown(KeyCode.E))
            {
                var randomSeeds = new List<int>();
                var plants = new List<bool>();
                var sizes = new List<int>();
                var greeds = new List<int>();
                var editables = new List<bool>();

                var squishedIdxs = new Dictionary<int, int>();
                int counter = 0;
                foreach (var info in inspector.GetSpawnedSpeciesInfo())
                {
                    int idx = info.Item1;
                    randomSeeds.Add(info.Item2);
                    plants.Add(info.Item3);
                    sizes.Add(info.Item4);
                    greeds.Add(info.Item5);
                    editables.Add(false);

                    squishedIdxs[idx] = counter++;
                }
                int numInitSpecies = squishedIdxs.Count;
                
                var sources = new List<int>();
                var targets = new List<int>();
                int numInteractions = 0;
                foreach (var info in inspector.GetSpawnedSpeciesInfo())
                {
                    int i = info.Item1;
                    foreach (int j in nodelink.GetActiveTargets(i))
                    {
                        sources.Add(squishedIdxs[i]);
                        targets.Add(squishedIdxs[j]);
                        numInteractions += 1;
                    }
                }
                Level.SaveAsNewPrefab(randomSeeds, plants, sizes, greeds, editables, sources, targets, DateTime.Now.Ticks.ToString());
            }
        }
#endif
    }
}