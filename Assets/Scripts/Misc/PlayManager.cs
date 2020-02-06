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
            nodelink.OnFocused     +=  (i)=> inspector.InspectSpecies(i);
            nodelink.OnUnfocused +=     ()=> inspector.Uninspect();
            nodelink.OnEmptyTapped +=   ()=> inspector.Unincubate();
            nodelink.OnConstraints +=   ()=> constraints.DisplayDisjoint(nodelink.Disjoint);
            nodelink.OnConstraints +=   ()=> constraints.DisplayNumEdges(nodelink.NumEdges);
            nodelink.OnConstraints +=   ()=> constraints.DisplayMaxChain(nodelink.MaxChain);
            nodelink.OnConstraints +=   ()=> constraints.DisplayMaxLoop(nodelink.MaxLoop);

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

            if (level.Details.alternateScore != "") {
                score.UseConstraintAsScoreInstead(level.Details.alternateScore);
            }
            score.SetStarThresholds(level.Details.targetScore1, level.Details.targetScore2);
            score.OnLevelCompletabled += ()=> level.ShowFinishFlag();
            score.OnLevelCompletabled += ()=> nodelink.ForceUnfocus();
            score.OnLevelCompletabled += ()=> GameManager.Instance.SetHelpText(level.Details.completedMessage, true, .5f, true);
            score.OnThreeStarsAchieved += ()=> nodelink.ForceUnfocus();
            score.OnThreeStarsAchieved += ()=> GameManager.Instance.SetHelpText(level.Details.threeStarsMessage, true, .5f, true);

            GameManager.Instance.SetHelpText(level.Details.introduction, true, 2f, true);
            level.OnFinished += FinishPlaythrough; // will have hanging references on replay, but I'm okay with that
        }
        void FinishPlaythrough(Levels.Level finished)
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
            bool highscore = GameManager.Instance.SaveHighScoreLocal(finished.Details.idx, score.HighestScore);
            if (highscore) {
                print("TODO: congratulation message for getting a high score");
            }
            if (GameManager.Instance.LoggedIn) {
                GameManager.Instance.SavePlaythroughRemote(finished.Details.idx, score.HighestScore, model.GetMatrix(), recorder.GetActions());
            }
            Destroy(gameObject);
        }

        // perform calculations if necessary
        bool atEquilibrium = true, graphSolved = true;
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
            if (!graphSolved && !nodelink.IsCalculatingAsync)
            {
                graphSolved = true;
// threads are not supported on webgl
#if UNITY_WEBGL
                nodelink.ConstraintsSync();
#else
                nodelink.ConstraintsAsync();
                #endif
            }
            // we want the score to update even if the model is calculating
            // but no events triggered in case of false positive due to being out of sync
            if (atEquilibrium && !model.IsCalculatingAsync &&
                graphSolved && !nodelink.IsCalculatingAsync)
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