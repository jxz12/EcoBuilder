using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EcoBuilder
{
    // this class handles communication between all the top level components
    public class PlayManager : MonoBehaviour
    {
        [SerializeField] FoodWeb.Model model;
        [SerializeField] NodeLink.Graph graph;

        [SerializeField] UI.Inspector inspector;
        [SerializeField] UI.Constraints constraints;
        [SerializeField] UI.Recorder recorder;
        [SerializeField] UI.Score score;

        [SerializeField] Effect heartPrefab, skullPrefab, poofPrefab, confettiPrefab;

        void Start()
        {
            ////////////////////////////////////
            // hook up events between objects //
            ////////////////////////////////////
            inspector.OnSpawned +=  (i,b,g)=> { graph.AddNode(i); graph.ShapeNode(i,g); graph.SetIfNodeCanBeTarget(i,!b); };
            inspector.OnSpawned +=  (i,b,g)=> model.AddSpecies(i,b);
            inspector.OnSpawned +=  (i,b,g)=> { if (b) constraints.AddLeafIdx(i); else constraints.AddPawIdx(i); };

            inspector.OnDespawned +=    (i)=> graph.ArchiveNode(i);
            inspector.OnDespawned +=    (i)=> model.RemoveSpecies(i);
            inspector.OnDespawned +=    (i)=> constraints.RemoveIdx(i);
            inspector.OnSizeSet +=    (i,x)=> model.SetSpeciesBodySize(i,x);
            inspector.OnGreedSet +=   (i,x)=> model.SetSpeciesInterference(i,x);
            inspector.OnConflicted += (i,m)=> graph.OutlineNode(i, cakeslice.Outline.Colour.Red);
            inspector.OnConflicted += (i,m)=> graph.TooltipNode(i, m);
            inspector.OnUnconflicted += (i)=> graph.UnoutlineNode(i);
            inspector.OnUnconflicted += (i)=> graph.UntooltipNode(i);
            inspector.OnIncubated +=     ()=> graph.ForceUnfocus();
            inspector.OnIncubated +=     ()=> graph.MoveHorizontal(-.5f);
            inspector.OnUnincubated +=   ()=> graph.MoveHorizontal(0);

            graph.OnEmptyTapped +=   ()=> inspector.CancelIncubation();
            recorder.OnUndoOrRedo += ()=> inspector.CancelIncubation();

            ////////////////////////////
            // the above will always (but not exclusively) cause the below in the next three things
            // smelly because the second callback does something that should already be done
            inspector.OnUserSpawned += (i)=> graph.ForceFocus(i);
            graph.OnNodeTapped +=      (i)=> inspector.InspectSpecies(i);

            inspector.OnUserDespawned += (i)=> graph.ForceUnfocus();
            graph.OnUnfocused +=          ()=> inspector.Uninspect();
            ////////////////////////////

            // constraints
            graph.OnLayedOut += ()=> constraints.UpdateDisjoint(graph.NumComponents > 1);
            graph.OnLayedOut += ()=> constraints.DisplayEdge(graph.NumLinks);
            graph.OnLayedOut += ()=> constraints.DisplayChain(graph.MaxChain);
            graph.OnLayedOut += ()=> constraints.DisplayLoop(graph.MaxLoop);

            model.OnEquilibrium += ()=> inspector.DrawHealthBars(i=> (float)model.GetNormalisedAbundance(i));
            model.OnEquilibrium += ()=> graph.ReflowLinks((i,j)=> (float)model.GetNormalisedFlux(i,j));
            model.OnEquilibrium += ()=> constraints.UpdateFeasibility(model.Feasible);
            model.OnEquilibrium += ()=> constraints.UpdateStability(model.Stable);
            model.OnEndangered += (i)=> inspector.MakeSpeciesObjectExtinct(i);
            model.OnRescued +=    (i)=> inspector.MakeSpeciesObjectRescued(i);
            model.OnEndangered += (i)=> graph.SpawnEffectOnNode(i, skullPrefab.gameObject);
            model.OnRescued +=    (i)=> graph.SpawnEffectOnNode(i, heartPrefab.gameObject);
            model.OnEndangered += (i)=> graph.FlashNode(i);
            model.OnRescued +=    (i)=> graph.UnflashNode(i);

            constraints.OnLeafFilled +=    (b)=> inspector.SetProducerAvailability(b);
            constraints.OnPawFilled +=     (b)=> inspector.SetConsumerAvailability(b);
            constraints.OnChainHovered +=   ()=> graph.OutlineChain(cakeslice.Outline.Colour.Red);
            constraints.OnLoopHovered +=    ()=> graph.OutlineLoop(cakeslice.Outline.Colour.Red);
            constraints.OnChainUnhovered += ()=> graph.UnoutlineChainOrLoop();
            constraints.OnLoopUnhovered +=  ()=> graph.UnoutlineChainOrLoop();

            inspector.OnUserSpawned +=      (i)=> recorder.SpeciesSpawn(i, inspector.Respawn, inspector.Despawn);
            inspector.OnUserDespawned +=    (i)=> recorder.SpeciesDespawn(i, inspector.Respawn, inspector.Despawn);
            inspector.OnUserSizeSet +=  (i,x,y)=> recorder.SizeSet(i, x, y, inspector.SetSize);
            inspector.OnUserGreedSet += (i,x,y)=> recorder.GreedSet(i, x, y, inspector.SetGreed);
            graph.OnUserLinked +=         (i,j)=> recorder.InteractionAdded(i, j, graph.AddLink, graph.RemoveLink);
            graph.OnUserUnlinked +=       (i,j)=> recorder.InteractionRemoved(i, j, graph.AddLink, graph.RemoveLink);
            graph.OnUserUnlinked +=       (i,j)=> graph.SpawnEffectOnLink(i, j, poofPrefab.gameObject);

            recorder.OnSpeciesTraitsChanged += (i)=> graph.ForceFocus(i);
            recorder.OnSpeciesMemoryLeak +=    (i)=> graph.RemoveNode(i);
            recorder.OnSpeciesMemoryLeak +=    (i)=> inspector.DespawnCompletely(i);

            inspector.OnUserSpawned +=   (i)=> model.TriggerSolve();
            inspector.OnUserDespawned += (i)=> model.TriggerSolve();
            inspector.OnSizeSet +=     (i,x)=> model.TriggerSolve();
            inspector.OnGreedSet +=    (i,x)=> model.TriggerSolve();
            graph.OnLayedOut +=           ()=> model.TriggerSolve();
            // graph.OnUserLinked +=      (i,j)=> model.TriggerSolve();
            // graph.OnUserUnlinked +=    (i,j)=> model.TriggerSolve();

            //////////////////////
            // initialise level //
            //////////////////////
            var details = GameManager.Instance.PlayedLevelDetails;

            inspector.HideSizeSlider(details.SizeSliderHidden);
            inspector.HideGreedSlider(details.GreedSliderHidden);
            if (details.SizeSliderHidden) {
                inspector.FixSizeInitialValue();
            }
            if (details.GreedSliderHidden) {
                inspector.FixGreedInitialValue();
            }
            inspector.AllowConflicts(details.ConflictsAllowed);
            graph.AllowSuperfocus = details.SuperfocusAllowed;
            graph.DragFromTarget = GameManager.Instance.ReverseDragDirection;
            graph.ConstrainTrophic = GameManager.Instance.ConstrainTrophic;
            graph.FindLoops = details.MinLoop >= 0;

            constraints.LimitLeaf(details.NumProducers);
            constraints.LimitPaw(details.NumConsumers);
            constraints.ConstrainEdge(details.MinEdges);
            constraints.ConstrainChain(details.MinChain);
            constraints.ConstrainLoop(details.MinLoop);

            // add species
            for (int i=0; i<details.NumInitSpecies; i++)
            {
                inspector.SpawnNotIncubated( i, details.Types[i]==LevelDetails.SpeciesType.Producer, details.Sizes[i], details.Greeds[i]);

                inspector.SetSpeciesRemovable(i, false);
                if (details.Edits[i]==LevelDetails.SpeciesEdit.None || details.Edits[i]==LevelDetails.SpeciesEdit.GreedOnly) {
                    inspector.FixSpeciesSize(i);
                }
                if (details.Edits[i]==LevelDetails.SpeciesEdit.None || details.Edits[i]==LevelDetails.SpeciesEdit.SizeOnly) {
                    inspector.FixSpeciesGreed(i);
                }

                // standard producer or consumer should be covered already
                if (details.Types[i]==LevelDetails.SpeciesType.Apex) {
                    graph.SetIfNodeCanBeSource(i, false);
                }
                if (details.Types[i]==LevelDetails.SpeciesType.Specialist) {
                    graph.SetIfNodeCanBeTarget(i, false);
                }
                graph.OutlineNode(i, details.Edits[i]==LevelDetails.SpeciesEdit.None? cakeslice.Outline.Colour.Blue : cakeslice.Outline.Colour.Clear);
            }
            // add interactions
            for (int ij=0; ij<details.NumInitInteractions; ij++)
            {
                int i = details.Sources[ij];
                int j = details.Targets[ij];

                graph.AddLink(i, j);
                graph.SetIfLinkRemovable(i, j, false);
                graph.OutlineLink(i, j, cakeslice.Outline.Colour.Blue);
            }
            // to give the model info on what interactions are made
            model.AttachAdjacency(graph.GetActiveTargets);

            // set up scoring
            if (details.Metric != LevelDetails.ScoreMetric.None) {
                score.AttachScoreSource(()=> model.GetNormalisedComplexity() * details.MainMultiplier, x=>$"{graph.NumNodes} species, multiplied by {graph.NumLinks} interaction(s), multiplied by {(x/graph.NumNodes/graph.NumLinks).ToString("F1")} health");
            }
            switch (GameManager.Instance.PlayedLevelDetails.Metric)
            {
            case LevelDetails.ScoreMetric.None:
                break;
            case LevelDetails.ScoreMetric.Producers:
                score.AttachScoreSource(()=> constraints.LeafValue * details.AltMultiplier, x=>$"{constraints.LeafValue} plant(s) multiplied by {details.AltMultiplier.ToString("N0")}");
                constraints.HighlightPaw();
                break;
            case LevelDetails.ScoreMetric.Consumers:
                score.AttachScoreSource(()=> constraints.PawValue * details.AltMultiplier, x=>$"{constraints.PawValue} animal(s) multiplied by {details.AltMultiplier.ToString("N0")}");
                constraints.HighlightPaw();
                break;
            case LevelDetails.ScoreMetric.Chain:
                score.AttachScoreSource(()=> graph.MaxChain * details.AltMultiplier, x=>$"{graph.MaxChain} chain length multiplied by {details.AltMultiplier.ToString("N0")}");
                constraints.HighlightChain();
                break;
            case LevelDetails.ScoreMetric.Loop:
                score.AttachScoreSource(()=> graph.MaxLoop * details.AltMultiplier, x=>$"{graph.MaxLoop} loop length multiplied by {details.AltMultiplier.ToString("N0")}");
                constraints.HighlightLoop();
                break;
            }
            score.OnHighestScoreBroken += ()=> model.SaveMatrix();

            long? prevHighScore = GameManager.Instance.GetHighScoreLocal(details.Idx);
            if (!details.ResearchMode)
            {
                score.SetStarThresholds(prevHighScore, details.TwoStarScore, details.ThreeStarScore);
                score.OnOneStarAchieved +=    ()=> GameManager.Instance.MakePlayedLevelFinishable();
                score.OnOneStarAchieved +=    ()=> graph.ForceUnfocus();
                score.OnOneStarAchieved +=    ()=> GameManager.Instance.HelpText.DelayThenShow(1, details.CompletedMessage);
                score.OnThreeStarsAchieved += ()=> graph.ForceUnfocus();
                score.OnThreeStarsAchieved += ()=> GameManager.Instance.HelpText.DelayThenShow(1, details.ThreeStarsMessage);
            }
            else
            {
                if (GameManager.Instance.GetCachedMedian(details.Idx) == null) {
                    GameManager.Instance.CacheMediansRemote(); // try once to get median
                }
                score.OnRankStaled += ()=> GameManager.Instance.GetSingleRankRemote(details.Idx, score.HighestScore, (b,s)=> score.SetStatsText(b? s:null, GameManager.Instance.GetCachedMedian(details.Idx)));
                score.EnableStatsText(prevHighScore);
                score.OnOneStarAchieved +=    ()=> GameManager.Instance.MakePlayedLevelFinishable(); // don't unfocus or show message, unlike a stars level
            }

            score.AttachConstraintsSatisfied(()=> constraints.AllSatisfied());
            score.AttachScoreValidity(()=> graph.GraphLayedOut && model.EquilibriumSolved);

            GameManager.Instance.OnPlayedLevelFinished += FinishPlaythrough;
            graph.AddDropShadow(GameManager.Instance.TakePlanet(), -1.5f);

            GameManager.Instance.BeginPlayedLevel();
        }
        void OnDestroy()
        {
            if (!applicationQuitting) { // for if GameManager is destroyed in its scene first, yes it's ugly
                GameManager.Instance.OnPlayedLevelFinished -= FinishPlaythrough;
            }
        }
        bool applicationQuitting = false;
        void OnApplicationQuit() { applicationQuitting = true; }

        void FinishPlaythrough()
        {
            // spreads computation across frames to avoid too big a lag spike
            StartCoroutine(FinishGradually());
            IEnumerator FinishGradually()
            {
                GameManager.Instance.SetReportCard(score.HighestStars, score.HighestScore, score.LastStatsRank, model.GetMatrix(), recorder.GetActions(), score.GetDescription());
                yield return null;
                Instantiate(confettiPrefab, GameManager.Instance.CardAnchor);
                yield return null;
                graph.Finish();
                yield return null;
                inspector.Finish();
                recorder.Finish();
                yield return null;
                score.Finish();
                constraints.Finish();
                yield return null;
                Destroy(gameObject);
                yield return null;
                GameManager.Instance.HelpText.Showing = false;
            }
        }

#if UNITY_EDITOR
        void Update()
        {
            // save a level for convenience
            if (Input.GetKeyDown(KeyCode.Space))
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
                    foreach (int j in graph.GetActiveTargets(i))
                    {
                        sources.Add(squishedIdxs[i]);
                        targets.Add(squishedIdxs[j]);
                        numInteractions += 1;
                    }
                }
                Level.SaveAsNewPrefab(randomSeeds, plants, sizes, greeds, sources, targets, DateTime.Now.Ticks.ToString());
            }
        }
#endif
    }
}