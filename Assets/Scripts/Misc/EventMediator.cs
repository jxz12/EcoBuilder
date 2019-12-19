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
        [SerializeField] UI.Help help;

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
            inspector.OnShaped +=        (i,g)=> { nodelink.ShapeNode(i,g); nodelink.FlashNode(i); nodelink.LieDownNode(i); }; // init as extinct
            inspector.OnIsProducerSet += (i,x)=> nodelink.SetIfNodeCanBeTarget(i,!x);
            inspector.OnIsProducerSet += (i,x)=> model.SetSpeciesIsProducer(i,x);
            inspector.OnIsProducerSet += (i,x)=> score.AddType(i,x);
            inspector.OnSizeSet +=       (i,x)=> model.SetSpeciesBodySize(i,x);
            inspector.OnGreedSet +=      (i,x)=> model.SetSpeciesInterference(i,x);
            inspector.OnUserSpawned +=     (i)=> nodelink.FocusNode(i);
            inspector.OnConflicted +=      (i)=> nodelink.HighlightNode(i);
            inspector.OnConflicted +=      (i)=> nodelink.TooltipNode(i, "Identical");
            inspector.OnUnconflicted +=    (i)=> nodelink.UnhighlightNode(i);
            inspector.OnUnconflicted +=    (i)=> nodelink.UntooltipNode(i);

            nodelink.OnNodeFocused += (i)=> inspector.InspectSpecies(i);
            nodelink.OnUnfocused +=    ()=> inspector.Uninspect();
            nodelink.OnEmptyPressed += ()=> inspector.Unincubate();
            nodelink.OnConstraints +=  ()=> score.DisplayDisjoint(nodelink.Disjoint);
            nodelink.OnConstraints +=  ()=> score.DisplayNumEdges(nodelink.NumEdges);
            nodelink.OnConstraints +=  ()=> score.DisplayMaxChain(nodelink.MaxChain);
            nodelink.OnConstraints +=  ()=> score.DisplayMaxLoop(nodelink.MaxLoop);

            model.OnEquilibrium += ()=> nodelink.RehealthBars(i=> model.GetNormalisedAbundance(i));
            model.OnEquilibrium += ()=> nodelink.ReflowLinks((i,j)=> model.GetNormalisedFlux(i,j));
            model.OnEquilibrium += ()=> score.DisplayScore(model.NormalisedScore, model.ScoreExplanation());
            model.OnEquilibrium += ()=> score.DisplayFeastability(model.Feasible, model.Stable);
            model.OnEndangered += (i)=> nodelink.FlashNode(i);
            model.OnEndangered += (i)=> nodelink.SkullEffectNode(i);
            model.OnEndangered += (i)=> nodelink.LieDownNode(i);
            model.OnRescued +=    (i)=> nodelink.UnflashNode(i);
            model.OnRescued +=    (i)=> nodelink.HeartEffectNode(i);
            model.OnRescued +=    (i)=> nodelink.BounceNode(i);

            score.OnProducersAvailable += (b)=> inspector.SetProducerAvailability(b);
            score.OnConsumersAvailable += (b)=> inspector.SetConsumerAvailability(b);

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

            score.AllowLevelFinishWhen(()=> atEquilibrium &&
                                            !model.IsCalculating &&
                                            graphSolved &&
                                            !nodelink.IsCalculating); 

            /////////////////////
            // initialise level

            var level = GameManager.Instance.PlayedLevel;
            score.ConstrainFromLevel(level);
            if (level == null)
                return; // should never happen in real game

            for (int i=0; i<level.Details.numSpecies; i++)
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
                nodelink.SetNodeDefaultOutline(i);
            }
            for (int ij=0; ij<level.Details.numInteractions; ij++)
            {
                int i = level.Details.resources[ij];
                int j = level.Details.consumers[ij];

                nodelink.AddLink(i, j);
                nodelink.SetIfLinkRemovable(i, j, false);
                nodelink.SetLinkDefaultOutline(i, j);
            }

            // TODO: magic numbers
            if (level.Details.sizeSliderHidden)
            {
                inspector.HideSizeSlider(.5f);
            }
            if (level.Details.greedSliderHidden)
            {
                inspector.HideGreedSlider(.5f);
            }

            nodelink.AddDropShadow(level.Landscape);
            nodelink.ConstrainTrophic = GameManager.Instance.ConstrainTrophic;
            level.StartTutorialIfAvailable();
            help.SetText(level.Details.introduction);

            score.OnLevelCompleted += ()=> inspector.Hide();
            score.OnLevelCompleted += ()=> nodelink.Freeze();
            // score.OnLevelCompleted += ()=> recorder.RecordMoves();
            score.OnLevelCompleted += ()=> help.DelayThenShow(2, level.Details.congratulation);
        }


        // perform calculations if necessary
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

        // This is to be used for level creation
        public void RecordDetailsIntoLevel()
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
            details.numSpecies = squishedIdxs.Count;
            
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
}
