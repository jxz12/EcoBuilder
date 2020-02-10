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

            inspector.OnUserSpawned += (i)=> nodelink.FocusNode(i);
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
            var level = GameManager.Instance.PlayedLevel;

            inspector.HideSizeSlider(level.SizeSliderHidden);
            inspector.HideGreedSlider(level.GreedSliderHidden);
            inspector.AllowConflicts(level.ConflictsAllowed);
            nodelink.AllowSuperfocus = level.SuperfocusAllowed;
            nodelink.ConstrainTrophic = GameManager.Instance.ConstrainTrophic;
            nodelink.DragFromTarget = GameManager.Instance.ReverseDragDirection;

            constraints.Constrain("Leaf", level.NumProducers);
            constraints.Constrain("Paw", level.NumConsumers);
            constraints.Constrain("Count", level.MinEdges);
            constraints.Constrain("Chain", level.MinChain);
            constraints.Constrain("Loop", level.MinLoop);

            for (int i=0; i<level.NumInitSpecies; i++)
            {
                inspector.SpawnNotIncubated(i,
                    level.Plants[i],
                    level.Sizes[i],
                    level.Greeds[i],
                    level.RandomSeeds[i],
                    level.Editables[i]);

                inspector.SetSpeciesRemovable(i, false);
                nodelink.SetIfNodeRemovable(i, false);
                nodelink.OutlineNode(i, cakeslice.Outline.Colour.Blue);
            }
            for (int ij=0; ij<level.NumInitInteractions; ij++)
            {
                int i = level.Sources[ij];
                int j = level.Targets[ij];

                nodelink.AddLink(i, j);
                nodelink.SetIfLinkRemovable(i, j, false);
                nodelink.OutlineLink(i, j, cakeslice.Outline.Colour.Blue);
            }

            score.SetStarThresholds(level.Metric, level.TargetScore1, level.TargetScore2);
            score.OnLevelCompletabled +=  ()=> level.ShowFinishFlag();
            score.OnLevelCompletabled +=  ()=> nodelink.ForceUnfocus();
            score.OnLevelCompletabled +=  ()=> GameManager.Instance.HelpText.DelayThenShow(.5f, level.CompletedMessage);
            score.OnThreeStarsAchieved += ()=> nodelink.ForceUnfocus();
            score.OnThreeStarsAchieved += ()=> GameManager.Instance.HelpText.DelayThenShow(.5f, level.ThreeStarsMessage);

            GameManager.Instance.HelpText.DelayThenShow(2, level.Introduction);
            level.OnFinished += FinishPlaythrough; // will have hanging references on replay, but I'm okay with that
        }
        void FinishPlaythrough(Level finished)
        {
            if (this == null) {
                finished.OnFinished -= FinishPlaythrough;
                print("TODO: there must be a more elegant way of dealing with this hanging reference");
                return;
            }
            inspector.Finish();
            nodelink.Finish();
            score.Finish();
            recorder.Finish();

            bool highscore = GameManager.Instance.SaveHighScoreLocal(finished.Idx, score.HighestScore);
            if (highscore) {
                print("TODO: congratulation on high score on level complete screen");
            }
            if (GameManager.Instance.LoggedIn) {
                GameManager.Instance.SavePlaythroughRemote(finished.Idx, score.HighestScore, model.GetMatrix(), recorder.GetActions());
            }
            Destroy(gameObject);
        }

        // perform calculations if necessary
        // LateUpdate to ensure all changes are done
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
            // we want the score to update even if the model is calculating
            // but no events triggered in case of false positive due to being out of sync
            if (atEquilibrium && !model.IsCalculatingAsync &&
                graphDrawn && !nodelink.IsCalculatingAsync)
            {
                score.UpdateStars();
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
                Level.SaveToNewPrefab(DateTime.Now.Ticks.ToString(), plants, randomSeeds, sizes, greeds, editables);
            }
        }
#endif
    }
}