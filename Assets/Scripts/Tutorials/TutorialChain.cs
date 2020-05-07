using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace EcoBuilder.Tutorials
{
    public class TutorialChain : Tutorial
    {
        protected override void StartLesson()
        {
            Hide();
            recorder.Hide();
            inspector.HideSizeSlider();
            inspector.HideRemoveButton();
            score.Hide();
            score.DisableStarCalculation();

            constraints.LimitLeaf(1);
            constraints.LimitPaw(1);

            ExplainIntro();
        }
        int producerIdx=-1, herbivoreIdx=-1;
        Transform producerTransform, herbivoreTransform;
        void ListenForInitIdxs(int idx, bool isProducer, GameObject gameObject)
        {
            if (isProducer) {
                producerIdx = idx;
                producerTransform = gameObject.transform;
            } else {
                herbivoreIdx = idx;
                herbivoreTransform = gameObject.transform;
            }
        }
        void ExplainIntro()
        {
            DetachSmellyListeners();
            AttachSmellyListener<int, bool, GameObject>(inspector, "OnSpawned", ListenForInitIdxs);
            AttachSmellyListener(graph, "OnLayedOut", ()=>CheckChainOfHeight(1, ExplainChainOfOne));
        }
        void CheckChainOfHeight(int heightGoal, Action Todo)
        {
            if (graph.MaxChain == heightGoal) {
                Todo.Invoke();
            }
        }
        void ExplainChainOfOne()
        {
            DetachSmellyListeners();
            Assert.IsTrue(producerIdx!=-1 && herbivoreIdx!=-1);

            help.Message = "This number on the left has changed! This is because the animal is one link away from a plant, which means it has a chain of one. By touching this icon, you can highlight all species with that chain length. Now try adding another animal.";
            help.Showing = true;

            // point at constraints panel
            targetAnchor = new Vector2(0,1);
            Point(new Vector2(55, -350 + hud.ConstraintsOffset), 30); 

            // allow animal again, but only one more
            constraints.LimitPaw(2);

            // stop user from removing the link
            graph.SetIfLinkRemovable(producerIdx, herbivoreIdx, false);
            graph.SetIfNodeCanBeTarget(herbivoreIdx, false);
            graph.SetIfNodeCanBeSource(producerIdx, false);

            AttachSmellyListener(inspector, "OnIncubated", ()=>{ help.Showing = false; Hide(); });
            AttachSmellyListener<int, bool, GameObject>(inspector, "OnSpawned", (i,b,g)=>carnivoreTransform=g.transform);
            AttachSmellyListener(graph, "OnLayedOut", ()=>RequestChainOfTwo());
        }
        void RequestChainOfTwo()
        {
            DetachSmellyListeners();

            help.Message = "And now make it eat the previous animal.";
            help.Showing = true;
            DragAndDrop(herbivoreTransform, carnivoreTransform, 2.5f);
            
            AttachSmellyListener(graph, "OnLayedOut", ()=>CheckChainOfHeight(2, ()=>ExplainChainOfTwo(1.5f)));
        }
        int carnivoreIdx=2; // nodes are not removable so this must be true
        Transform carnivoreTransform;
        void ExplainChainOfTwo(float delay)
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            Hide();
            graph.ForceUnfocus();
            help.Showing = false;
            WaitThenDo(delay, ()=>{ help.Message = "Great job. It takes two links to get from the plant to the second animal, and so the maximum chain of your ecosystem is two. However, you will notice that it is going extinct! This is because biomass is lost at each level of an ecosystem, and so the top species does not have enough food to sustain it. Try saving it, but without adding any more links."; help.Showing = true; inspector.HideSizeSlider(false); });

            graph.SetIfLinkRemovable(herbivoreIdx, carnivoreIdx, false);

            AttachSmellyListener(model, "OnEquilibrium", ()=>{ if (model.Feasible) ExplainTopSurvival(1.5f); });
        }
        void ExplainTopSurvival(float delay)
        {
            DetachSmellyListeners();

            graph.ForceUnfocus();
            help.Showing = false;
            WaitThenDo(delay, ()=>{ help.Message = "Great! In general, it is more difficult to get a species to survive, the longer its chain length. Let's try one more thing: make the species with a chain of two also eat the plant."; help.Showing = true; });

            graph.SetIfNodeCanBeSource(producerIdx, true);

            AttachSmellyListener(graph, "OnLayedOut", ()=>CheckChainOfHeight(1, ()=>ExplainBackToOne(1)));
        }
        void ExplainBackToOne(float delay)
        {
            DetachSmellyListeners();

            graph.ForceUnfocus();
            help.Showing = false;
            WaitThenDo(delay, ()=>{ help.Message = "The max chain of your ecosystem has fallen back to one! This is because the height of any given species only considers its shortest path to any plant, and so the path going through both animals is overridden by the path from the plant. To finish this level, reconstruct the ecosystem with a chain of 2."; help.Showing = true; score.Hide(false); score.DisableStarCalculation(false); }); 

            constraints.ConstrainChain(2);
            recorder.Hide(false);
            graph.SetIfNodeCanBeTarget(herbivoreIdx, true);
            score.Hide(false);
            score.DisableStarCalculation(false);
        }
    }
}