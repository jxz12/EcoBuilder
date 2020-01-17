using UnityEngine;
using System;
using System.Collections.Generic;

namespace EcoBuilder
{
    // this class handles communication between all the top level components
    public class EventMediator : MonoBehaviour
    {
        [SerializeField] NodeLink.NodeLink nodelink;
        [SerializeField] Model.Model model;

        [SerializeField] UI.Inspector inspector;
        [SerializeField] UI.MoveRecorder recorder;
        [SerializeField] UI.Score score;
        [SerializeField] UI.Constraints constraints;

        void Start()
        {
            ////////////////////////////////////
            // hook up events between objects //
            ////////////////////////////////////
            inspector.OnIncubated +=        ()=> nodelink.ForceUnfocus();
            inspector.OnIncubated +=        ()=> nodelink.MoveHorizontal(-.5f); // TODO: magic number
            inspector.OnUnincubated +=      ()=> nodelink.MoveHorizontal(0);
            inspector.OnSpawned +=       (i,g)=> nodelink.AddNode(i,g);
            inspector.OnSpawned +=       (i,g)=> model.AddSpecies(i);
            inspector.OnDespawned +=       (i)=> nodelink.RemoveNode(i);
            inspector.OnDespawned +=       (i)=> model.RemoveSpecies(i);
            inspector.OnDespawned +=       (i)=> constraints.RemoveIdx(i);
            inspector.OnIsProducerSet += (i,x)=> nodelink.SetIfNodeCanBeTarget(i,!x);
            inspector.OnIsProducerSet += (i,x)=> model.SetSpeciesIsProducer(i,x);
            inspector.OnIsProducerSet += (i,x)=> constraints.AddType(i,x);
            inspector.OnSizeSet +=       (i,x)=> model.SetSpeciesBodySize(i,x);
            inspector.OnGreedSet +=      (i,x)=> model.SetSpeciesInterference(i,x);
            inspector.OnConflicted +=    (i,m)=> nodelink.OutlineNode(i, cakeslice.Outline.Colour.Red);
            inspector.OnConflicted +=    (i,m)=> nodelink.TooltipNode(i, m);
            inspector.OnUnconflicted +=    (i)=> nodelink.UnoutlineNode(i);
            inspector.OnUnconflicted +=    (i)=> nodelink.UntooltipNode(i);

            inspector.OnUserSpawned += (i)=> nodelink.FocusNode(i);
            nodelink.OnFocused     +=  (i)=> inspector.InspectSpecies(i);
            nodelink.OnUnfocused +=     ()=> inspector.Uninspect();
            nodelink.OnEmptyTapped +=   ()=> inspector.Unincubate();
            nodelink.OnConstraints +=   ()=> constraints.DisplayDisjoint(nodelink.Disjoint);
            nodelink.OnConstraints +=   ()=> constraints.DisplayNumEdges(nodelink.NumEdges);
            nodelink.OnConstraints +=   ()=> constraints.DisplayMaxChain(nodelink.MaxChain);
            nodelink.OnConstraints +=   ()=> constraints.DisplayMaxLoop(nodelink.MaxLoop);

            model.OnEquilibrium += ()=> inspector.DrawHealthBars(i=> model.GetNormalisedAbundance(i));
            model.OnEquilibrium += ()=> nodelink.ReflowLinks((i,j)=> model.GetNormalisedFlux(i,j));
            model.OnEquilibrium += ()=> constraints.DisplayFeasibility(model.Feasible);
            model.OnEquilibrium += ()=> constraints.DisplayStability(model.Stable);
            model.OnEndangered += (i)=> nodelink.FlashNode(i);
            model.OnEndangered += (i)=> nodelink.SkullEffectNode(i);
            model.OnEndangered += (i)=> nodelink.LieDownNode(i);
            model.OnRescued +=    (i)=> nodelink.UnflashNode(i);
            model.OnRescued +=    (i)=> nodelink.HeartEffectNode(i);
            model.OnRescued +=    (i)=> nodelink.BounceNode(i);

            constraints.OnProducersAvailable += (b)=> inspector.SetProducerAvailability(b);
            constraints.OnConsumersAvailable += (b)=> inspector.SetConsumerAvailability(b);
            constraints.OnChainHovered +=       (b)=> nodelink.OutlineChain(b, cakeslice.Outline.Colour.Cyan);
            constraints.OnLoopHovered +=        (b)=> nodelink.OutlineLoop(b, cakeslice.Outline.Colour.Cyan);

            inspector.OnSpawned +=       (i,g)=> atEquilibrium = false;
            inspector.OnSpawned +=       (i,g)=> graphSolved = false;
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


            //////////////////////
            // initialise level //
            //////////////////////
            var level = GameManager.Instance.PlayedLevel;

            inspector.HideSizeSlider(level.Details.sizeSliderHidden);
            inspector.HideGreedSlider(level.Details.greedSliderHidden);
            inspector.AllowConflicts(level.Details.conflictsAllowed);
            nodelink.AllowSuperfocus = level.Details.superfocusAllowed;
            nodelink.ConstrainTrophic = GameManager.Instance.ConstrainTrophic;
            nodelink.DragFromTarget = GameManager.Instance.ReverseDragDirection;
            nodelink.AddDropShadow(level.Landscape);

            score.SetScoreThresholds(level.Details.targetScore1, level.Details.targetScore2);
            score.OnLevelCompletabled +=   ()=> level.ShowFinishFlag();
            score.OnLevelIncompletabled += ()=> level.ShowThumbnail();

            constraints.Constrain("Leaf", level.Details.numProducers);
            constraints.Constrain("Paw", level.Details.numConsumers);
            constraints.Constrain("Count", level.Details.minEdges);
            constraints.Constrain("Chain", level.Details.minChain);
            constraints.Constrain("Loop", level.Details.minLoop);

            for (int i=0; i<level.Details.numInitSpecies; i++)
            {
                inspector.SpawnNotIncubated(i,
                    level.Details.plants[i],
                    level.Details.sizes[i],
                    level.Details.greeds[i],
                    level.Details.randomSeeds[i],
                    level.Details.sizeEditables[i],
                    level.Details.greedEditables[i]);

                inspector.SetSpeciesRemovable(i, false);
                nodelink.SetIfNodeRemovable(i, false);
                nodelink.OutlineNode(i, cakeslice.Outline.Colour.Blue);
            }
            for (int ij=0; ij<level.Details.numInteractions; ij++)
            {
                int i = level.Details.resources[ij];
                int j = level.Details.consumers[ij];

                nodelink.AddLink(i, j);
                nodelink.SetIfLinkRemovable(i, j, false);
                nodelink.OutlineLink(i, j, cakeslice.Outline.Colour.Blue);
            }

            level.OnFinished += FinishPlaythrough;
            playedLevel = level;
            level.StartTutorialIfAvailable();
        }
        Levels.Level playedLevel;
        void OnDestroy()
        {
            if (playedLevel != null) { // I think this is ugly
                playedLevel.OnFinished -= FinishPlaythrough;
            }
        }
        void FinishPlaythrough()
        {
            inspector.Hide();
            nodelink.Freeze();
            score.CompleteLevel();

            double[,] matrix = model.RecordState();
            int[,] actions = recorder.RecordMoves();
            playedLevel.SavePlaythrough(score.NormalisedScore, matrix, actions);
        }

        // perform calculations if necessary
        bool atEquilibrium = true, graphSolved = true;
        void LateUpdate()
        {
            if (!graphSolved && !nodelink.IsCalculating)
            {
                graphSolved = true;
// threads are not supported on webgl
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
            // we want the score to update even if the model is calculating
            score.DisplayScore(model.Complexity, model.ScoreExplanation());
            // but no events triggered in case of false positive due to being out of sync
            if (atEquilibrium && !model.IsCalculating &&
                graphSolved && !nodelink.IsCalculating)
            {
                score.TriggerScoreEvents();
            }
        }

#if UNITY_EDITOR
        void Update()
        {
            // save a level for convenience
            if (Input.GetKeyDown(KeyCode.Q) &&
                Input.GetKeyDown(KeyCode.W) &&
                Input.GetKeyDown(KeyCode.E))
            {
                var details = new Levels.LevelDetails();
                details.plants = new List<bool>();
                details.randomSeeds = new List<int>();
                details.sizes = new List<float>();
                details.greeds = new List<float>();
                details.sizeEditables = new List<bool>();
                details.greedEditables = new List<bool>();

                var squishedIdxs = new Dictionary<int, int>();
                int counter = 0;
                foreach (var kvp in inspector.GetSpeciesInfo())
                {
                    int idx = kvp.Key;
                    UI.Inspector.Species s = kvp.Value;
                    details.plants.Add(s.IsProducer);
                    details.randomSeeds.Add(s.RandomSeed);
                    details.sizes.Add(s.BodySize);
                    details.greeds.Add(s.Greediness);
                    details.sizeEditables.Add(s.SizeEditable);
                    details.greedEditables.Add(s.GreedEditable);

                    squishedIdxs[idx] = counter++;
                }
                details.numInitSpecies = squishedIdxs.Count;
                
                details.resources = new List<int>();
                details.consumers = new List<int>();
                details.numInteractions = 0;
                foreach (var kvp in inspector.GetSpeciesInfo())
                {
                    int i = kvp.Key;
                    foreach (int j in nodelink.GetTargets(i))
                    {
                        details.resources.Add(squishedIdxs[i]);
                        details.consumers.Add(squishedIdxs[j]);
                        details.numInteractions += 1;
                    }
                }
                Levels.Level.SaveAsNewPrefab(details, DateTime.Now.Ticks.ToString());
            }
        }
#endif
    }
}
