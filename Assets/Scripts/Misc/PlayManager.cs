using UnityEngine;
using System;
using System.Collections.Generic;

namespace EcoBuilder
{
    // this class handles communication between all the top level components
    public class PlayManager : MonoBehaviour
    {
        [SerializeField] Model.Model model;
        [SerializeField] NodeLink.NodeLink nodelink;

        [SerializeField] UI.Inspector inspector;
        [SerializeField] UI.Incubator incubator;
        [SerializeField] UI.Recorder recorder;
        [SerializeField] UI.Score score;
        [SerializeField] UI.Constraints constraints;

        void Start()
        {
            ////////////////////////////////////
            // hook up events between objects //
            ////////////////////////////////////
            incubator.OnIncubated +=  (b)=> inspector.IncubateNew(b);
            incubator.OnIncubated +=  (b)=> nodelink.ForceUnfocus();
            incubator.OnIncubated +=  (b)=> nodelink.MoveHorizontal(-.5f);
            incubator.OnDropped +=     ()=> inspector.SpawnIncubated();
            incubator.OnUnincubated += ()=> nodelink.MoveHorizontal(0);

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

            inspector.OnUserSpawned += (i)=> nodelink.SwitchFocus(i);
            nodelink.OnFocused +=      (i)=> inspector.InspectSpecies(i);

            nodelink.OnUnfocused +=     ()=> inspector.Uninspect();
            nodelink.OnEmptyTapped +=   ()=> inspector.Unincubate();
            nodelink.OnLayedOut +=      ()=> constraints.DisplayNumComponents(nodelink.NumComponents);
            nodelink.OnLayedOut +=      ()=> constraints.DisplayNumEdges(nodelink.NumEdges);
            nodelink.OnLayedOut +=      ()=> constraints.DisplayMaxChain(nodelink.MaxChain);
            nodelink.OnLayedOut +=      ()=> constraints.DisplayMaxLoop(nodelink.MaxLoop);

            model.OnEquilibrium += ()=> score.UpdateScore(model.GetNormalisedComplexity(), model.GetComplexityExplanation());
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

            constraints.OnProducersAvailable += (b)=> incubator.SetProducerAvailability(b);
            constraints.OnConsumersAvailable += (b)=> incubator.SetConsumerAvailability(b);
            constraints.OnChainHovered +=       (b)=> nodelink.OutlineChain(b, cakeslice.Outline.Colour.Red);
            constraints.OnLoopHovered +=        (b)=> nodelink.OutlineLoop(b, cakeslice.Outline.Colour.Red);

            inspector.OnSpawned +=       (i,g)=> atEquilibrium = false;
            inspector.OnSpawned +=       (i,g)=> graphDrawn = false;
            inspector.OnDespawned +=       (i)=> atEquilibrium = false;
            inspector.OnDespawned +=       (i)=> graphDrawn = false;
            inspector.OnIsProducerSet += (i,x)=> atEquilibrium = false;
            inspector.OnSizeSet +=       (i,x)=> atEquilibrium = false;
            inspector.OnGreedSet +=      (i,x)=> atEquilibrium = false;
            nodelink.OnLinked +=            ()=> atEquilibrium = false;
            nodelink.OnLinked +=            ()=> graphDrawn = false;

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
            var details = GameManager.Instance.PlayedLevelDetails;

            inspector.HideSizeSlider(details.SizeSliderHidden);
            inspector.HideGreedSlider(details.GreedSliderHidden);
            inspector.AllowConflicts(details.ConflictsAllowed);
            nodelink.AllowSuperfocus = details.SuperfocusAllowed;
            nodelink.ConstrainTrophic = GameManager.Instance.ConstrainTrophic;
            nodelink.DragFromTarget = GameManager.Instance.ReverseDragDirection;

            constraints.Constrain("Leaf", details.NumProducers);
            constraints.Constrain("Paw", details.NumConsumers);
            constraints.Constrain("Count", details.MinEdges);
            constraints.Constrain("Chain", details.MinChain);
            constraints.Constrain("Loop", details.MinLoop);

            for (int i=0; i<details.NumInitSpecies; i++)
            {
                inspector.SpawnNotIncubated(i,
                    details.Plants[i],
                    details.Sizes[i],
                    details.Greeds[i],
                    details.RandomSeeds[i],
                    details.Editables[i]);

                inspector.SetSpeciesRemovable(i, false);
                nodelink.SetIfNodeRemovable(i, false);
                nodelink.OutlineNode(i, cakeslice.Outline.Colour.Blue);
            }
            for (int ij=0; ij<details.NumInitInteractions; ij++)
            {
                int i = details.Sources[ij];
                int j = details.Targets[ij];

                nodelink.AddLink(i, j);
                nodelink.SetIfLinkRemovable(i, j, false);
                nodelink.OutlineLink(i, j, cakeslice.Outline.Colour.Blue);
            }

            score.SetStarThresholds(details.Metric, details.TargetScore1, details.TargetScore2);
            score.OnLevelCompletabled +=  ()=> GameManager.Instance.MakePlayedLevelFinishable();
            score.OnLevelCompletabled +=  ()=> nodelink.ForceUnfocus();
            score.OnLevelCompletabled +=  ()=> GameManager.Instance.HelpText.DelayThenShow(.5f, details.CompletedMessage);
            score.OnThreeStarsAchieved += ()=> nodelink.ForceUnfocus();
            score.OnThreeStarsAchieved += ()=> GameManager.Instance.HelpText.DelayThenShow(.5f, details.ThreeStarsMessage);

            GameManager.Instance.HelpText.Showing = false;
            GameManager.Instance.HelpText.DelayThenShow(2, details.Introduction);
            GameManager.Instance.OnPlayedLevelFinished += FinishPlaythrough;
        }
        void OnDestroy()
        {
            GameManager.Instance.OnPlayedLevelFinished -= FinishPlaythrough;
        }
        void FinishPlaythrough()
        {
            inspector.Finish();
            nodelink.Finish();
            recorder.Finish();

            var details = GameManager.Instance.PlayedLevelDetails;
            int oldScore = GameManager.Instance.GetHighScoreLocal(details.Idx);
            int worldAvg = GameManager.Instance.GetLeaderboardMedian(details.Idx);
            score.Finish(oldScore, worldAvg);
            GameManager.Instance.SaveHighScoreLocal(details.Idx, score.HighestScore);

            if (GameManager.Instance.LoggedIn) {
                GameManager.Instance.SavePlaythroughRemote(details.Idx, score.HighestScore, model.GetMatrix(), recorder.GetActions());
            }
            Destroy(gameObject);
        }

        // perform calculations if necessary
        // LateUpdate to ensure all changes are done
        // TODO: this is weird and should probably be moved back into nodelink and model
        bool atEquilibrium = true, graphDrawn = true;
        void LateUpdate()
        {
            if (!atEquilibrium && !model.IsCalculatingAsync)
            {
                atEquilibrium = true;
#if UNITY_WEBGL
                model.EquilibriumSync(nodelink.GetTargets);
#else
                model.EquilibriumAsync(nodelink.GetTargets);
#endif
            }
            if (!graphDrawn && !nodelink.IsCalculatingAsync)
            {
                graphDrawn = true;
// threads are not supported on webgl
#if UNITY_WEBGL
                nodelink.LayoutSync();
#else
                nodelink.LayoutAsync();
                #endif
            }
            // this needs to be here to ensure that the calculated components
            // are synced before moving every frame
            if (graphDrawn && !nodelink.IsCalculatingAsync)
            {
                nodelink.SeparateConnectedComponents();
            }
            // we want the score to update even if the model is calculating
            // but no events triggered in case of false positive due to being out of sync
            if (atEquilibrium && !model.IsCalculatingAsync &&
                graphDrawn && !nodelink.IsCalculatingAsync)
            {
                score.UpdateStars(constraints.AllSatisfied());
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
                var plants = new List<bool>();
                var randomSeeds = new List<int>();
                var sizes = new List<float>();
                var greeds = new List<float>();
                var editables = new List<bool>();

                var squishedIdxs = new Dictionary<int, int>();
                int counter = 0;
                foreach (var kvp in inspector.GetSpeciesInfo())
                {
                    int idx = kvp.Key;
                    UI.Inspector.Species s = kvp.Value;
                    plants.Add(s.IsProducer);
                    randomSeeds.Add(s.RandomSeed);
                    sizes.Add(s.BodySize);
                    greeds.Add(s.Greediness);
                    editables.Add(s.Editable);

                    squishedIdxs[idx] = counter++;
                }
                int numInitSpecies = squishedIdxs.Count;
                
                var resources = new List<int>();
                var consumers = new List<int>();
                int numInteractions = 0;
                foreach (var kvp in inspector.GetSpeciesInfo())
                {
                    int i = kvp.Key;
                    foreach (int j in nodelink.GetTargets(i))
                    {
                        resources.Add(squishedIdxs[i]);
                        consumers.Add(squishedIdxs[j]);
                        numInteractions += 1;
                    }
                }
                // Level.SaveToNewPrefab(DateTime.Now.Ticks.ToString(), plants, randomSeeds, sizes, greeds, editables);
            }
        }
#endif
    }
}