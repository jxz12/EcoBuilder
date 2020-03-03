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
            targetSize = Vector2.zero;
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
            AttachSmellyListener(nodelink, "OnLayedOut", ()=>CheckChainOfHeight(1, ExplainChainOfOne));
        }
        void CheckChainOfHeight(int heightGoal, Action Todo)
        {
            if (nodelink.MaxChain == heightGoal) {
                Todo.Invoke();
            }
        }
        void ExplainChainOfOne()
        {
            DetachSmellyListeners();
            Assert.IsTrue(producerIdx!=-1 && herbivoreIdx!=-1);

            help.Message = "You should notice that this number on the left has changed! This is because the animal is one link away from a plant, which means it has a chain of one. By pressing this icon, you can highlight species with that chain. Now try adding another animal.";
            help.Showing = true;

            // point at constraints panel
            Point();
            targetZRot = 30;
            targetSize = new Vector2(100,100);
            targetAnchor = new Vector2(0,1);
            targetPos = new Vector2(55, -350);

            // allow animal again, but only one more
            constraints.LimitPaw(2);

            // stop user from removing the link
            nodelink.SetIfLinkRemovable(producerIdx, herbivoreIdx, false);
            nodelink.SetIfNodeCanBeTarget(herbivoreIdx, false);
            nodelink.SetIfNodeCanBeSource(producerIdx, false);

            AttachSmellyListener(inspector, "OnIncubated", ()=>{ help.Showing = false; targetSize=Vector2.zero; });
            AttachSmellyListener<int, bool, GameObject>(inspector, "OnSpawned", (i,b,g)=>carnivoreTransform=g.transform);
            AttachSmellyListener(nodelink, "OnLayedOut", ()=>RequestChainOfTwo());
        }
        void RequestChainOfTwo()
        {
            DetachSmellyListeners();

            help.Message = "And now make it eat the previous animal.";
            help.Showing = true;
            targetSize = new Vector2(100,100);
            smoothTime = .5f;
            StartCoroutine(DragAndDrop(herbivoreTransform, carnivoreTransform, 2.5f));
            
            AttachSmellyListener(nodelink, "OnLayedOut", ()=>CheckChainOfHeight(2, ()=>ExplainChainOfTwo(1.5f)));
        }
        int carnivoreIdx=2; // nodes are not removable so this must be true
        Transform carnivoreTransform;
        void ExplainChainOfTwo(float delay)
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            targetSize = Vector2.zero;
            nodelink.ForceUnfocus();
            help.Showing = false;
            StartCoroutine(WaitThenDo(delay, ()=>{ help.Message = "Great job. It takes two links to get from the plant to the second animal, and so the maximum chain of your ecosystem is two. However, you will notice that it is going extinct! This is because biomass is lost at each level of an ecosystem, and so the top species does not have enough food to sustain it. Try saving it, but without adding any more links."; help.Showing = true; inspector.HideSizeSlider(false); }));

            nodelink.SetIfLinkRemovable(herbivoreIdx, carnivoreIdx, false);

            AttachSmellyListener(model, "OnEquilibrium", ()=>{ if (model.Feasible) ExplainTopSurvival(1.5f); });
        }
        void ExplainTopSurvival(float delay)
        {
            DetachSmellyListeners();

            nodelink.ForceUnfocus();
            help.Showing = false;
            StartCoroutine(WaitThenDo(delay, ()=>{ help.Message = "Great! In general, it is more difficult to get a species to survive, the longer its chain length. Let's try one more thing: make the species with a chain of two also eat the plant."; help.Showing = true; }));

            nodelink.SetIfNodeCanBeSource(producerIdx, true);

            AttachSmellyListener(nodelink, "OnLayedOut", ()=>CheckChainOfHeight(1, ()=>ExplainBackToOne(1)));
        }
        void ExplainBackToOne(float delay)
        {
            DetachSmellyListeners();

            nodelink.ForceUnfocus();
            help.Showing = false;
            StartCoroutine(WaitThenDo(delay, ()=>{ help.Message = "The max chain of your ecosystem has fallen back to one! This is because the height of any given species only considers its shortest path to any plant, and so the path going through both animals is overridden by the path from the plant. To finish this level, reconstruct the ecosystem with a chain of 2."; help.Showing = true; score.Hide(false); score.DisableStarCalculation(false); })); 

            constraints.ConstrainChain(2);
            recorder.Hide(false);
            nodelink.SetIfNodeCanBeTarget(herbivoreIdx, true);
            score.Hide(false);
            score.DisableStarCalculation(false);
        }
    }
}