using System;
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

        [SerializeField] UI.Effect heartPrefab, skullPrefab, poofPrefab;

        void Start()
        {
            ////////////////////////////////////
            // hook up events between objects //
            ////////////////////////////////////
            inspector.OnSpawned +=  (i,b,g)=> { nodelink.AddNode(i,g); nodelink.SetIfNodeCanBeTarget(i,!b); };
            inspector.OnSpawned +=  (i,b,g)=> model.AddSpecies(i,b);
            inspector.OnSpawned +=  (i,b,g)=> { if (b) constraints.AddLeafIdx(i); else constraints.AddPawIdx(i); };

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
            nodelink.OnEmptyTapped += ()=> print("TODO: maybe make this come back on focus");

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
            model.OnEndangered += (i)=> nodelink.SpawnEffectOnNode(i, skullPrefab.gameObject);
            model.OnRescued +=    (i)=> nodelink.SpawnEffectOnNode(i, heartPrefab.gameObject);
            model.OnEndangered += (i)=> nodelink.FlashNode(i);
            model.OnRescued +=    (i)=> nodelink.UnflashNode(i);

            constraints.OnLeafFilled +=    (b)=> inspector.SetProducerAvailability(b);
            constraints.OnPawFilled +=     (b)=> inspector.SetConsumerAvailability(b);
            constraints.OnChainHovered +=   ()=> nodelink.OutlineChain(cakeslice.Outline.Colour.Red);
            constraints.OnLoopHovered +=    ()=> nodelink.OutlineLoop(cakeslice.Outline.Colour.Red);
            constraints.OnChainUnhovered += ()=> nodelink.UnoutlineChainOrLoop();
            constraints.OnLoopUnhovered +=  ()=> nodelink.UnoutlineChainOrLoop();

            inspector.OnUserSpawned +=      (i)=> recorder.SpeciesSpawn(i, inspector.Respawn, inspector.Despawn);
            inspector.OnUserDespawned +=    (i)=> recorder.SpeciesDespawn(i, inspector.Respawn, inspector.Despawn);
            inspector.OnUserSizeSet +=  (i,x,y)=> recorder.SizeSet(i, x, y, inspector.SetSize);
            inspector.OnUserGreedSet += (i,x,y)=> recorder.GreedSet(i, x, y, inspector.SetGreed);
            nodelink.OnUserLinked +=      (i,j)=> recorder.InteractionAdded(i, j, nodelink.AddLink, nodelink.RemoveLink);
            nodelink.OnUserUnlinked +=    (i,j)=> recorder.InteractionRemoved(i, j, nodelink.AddLink, nodelink.RemoveLink);
            nodelink.OnUserUnlinked +=    (i,j)=> nodelink.SpawnEffectOnLink(i, j, poofPrefab.gameObject);

            recorder.OnSpeciesTraitsChanged += (i)=> nodelink.ForceFocus(i);
            recorder.OnSpeciesMemoryLeak +=    (i)=> nodelink.RemoveNodeCompletely(i);
            recorder.OnSpeciesMemoryLeak +=    (i)=> inspector.DespawnCompletely(i);

            inspector.OnUserSpawned +=   (i)=> model.TriggerSolve();
            inspector.OnUserDespawned += (i)=> model.TriggerSolve();
            inspector.OnSizeSet +=     (i,x)=> model.TriggerSolve();
            inspector.OnGreedSet +=    (i,x)=> model.TriggerSolve();
            nodelink.OnUserLinked +=   (i,j)=> model.TriggerSolve();
            nodelink.OnUserUnlinked += (i,j)=> model.TriggerSolve();

            //////////////////////
            // initialise level //
            //////////////////////
            var details = GameManager.Instance.PlayedLevelDetails;

            if (details.SizeSliderHidden)
            {
                inspector.HideSizeSlider();
                inspector.FixSizeInitialValue();
            }
            if (details.GreedSliderHidden)
            {
                inspector.HideGreedSlider();
                inspector.FixGreedInitialValue();
            }
            inspector.AllowConflicts(details.ConflictsAllowed);
            nodelink.AllowSuperfocus = details.SuperfocusAllowed;
            nodelink.ConstrainTrophic = GameManager.Instance.ConstrainTrophic;
            nodelink.DragFromTarget = GameManager.Instance.ReverseDragDirection;

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
                    nodelink.SetIfNodeCanBeSource(i, false);
                }
                if (details.Types[i]==LevelDetails.SpeciesType.Specialist) {
                    nodelink.SetIfNodeCanBeTarget(i, false);
                }
                nodelink.OutlineNode(i, details.Edits[i]==LevelDetails.SpeciesEdit.None? cakeslice.Outline.Colour.Blue : cakeslice.Outline.Colour.Clear);
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
            // to give the model info on what interactions are made
            model.AttachAdjacency(nodelink.GetActiveTargets);

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
                constraints.HighlightPaw();
                break;
            case LevelDetails.ScoreMetric.Chain:
                score.AttachScoreSource(()=> model.GetNormalisedComplexity() * details.MainMultiplier);
                score.AttachScoreSource(()=> nodelink.MaxChain * details.AltMultiplier);
                constraints.HighlightChain();
                break;
            case LevelDetails.ScoreMetric.Loop:
                score.AttachScoreSource(()=> model.GetNormalisedComplexity() * details.MainMultiplier);
                score.AttachScoreSource(()=> nodelink.MaxLoop * details.AltMultiplier);
                constraints.HighlightLoop();
                break;
            }
            constraints.HighlightChain();
            score.AttachConstraintsSatisfied(()=> constraints.AllSatisfied());
            score.AttachScoreValidity(()=> nodelink.GraphLayedOut && model.EquilibriumSolved);

            score.SetStarThresholds(details.Metric, details.TwoStarScore, details.ThreeStarScore);
            score.OnOneStarAchieved +=    ()=> GameManager.Instance.MakePlayedLevelFinishable();
            score.OnOneStarAchieved +=    ()=> nodelink.ForceUnfocus();
            score.OnOneStarAchieved +=    ()=> GameManager.Instance.HelpText.DelayThenShow(1, details.CompletedMessage);
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
                Level.SaveAsNewPrefab(randomSeeds, plants, sizes, greeds, sources, targets, DateTime.Now.Ticks.ToString());
            }
        }
#endif
    }
}