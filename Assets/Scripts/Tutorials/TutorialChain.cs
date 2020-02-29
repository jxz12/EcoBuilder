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
            // inspector.HideGreedSlider();
            inspector.HideRemoveButton();
            score.Hide();
            score.DisableStarCalculation();

            constraints.ConstrainLeaf(1);
            constraints.ConstrainPaw(1);

            ExplainIntro();
        }
        int producerIdx=-1, herbivoreIdx=-1;
        void ListenForIdxs(int idx, bool isProducer, GameObject gameObject)
        {
            if (isProducer) {
                producerIdx = idx;
            } else {
                herbivoreIdx = idx;
            }
        }
        void ExplainIntro()
        {
            DetachSmellyListeners();
            AttachSmellyListener<int, bool, GameObject>(inspector, "OnSpawned", ListenForIdxs);
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

            help.Message = "You should notice that the number on the left has changed! This is because the animal is one link away from a plant, which means it has a chain of one. If you press that icon, you can highlight all species with that chain length. Try adding another animal and making it eat the current one.";
            help.Showing = true;


            // allow animal again, but only one more
            constraints.ConstrainPaw(2);
            inspector.SetConsumerAvailability(true); // this is a hack because I didn't code constraints well

            // stop user from removing the link
            nodelink.SetIfLinkRemovable(producerIdx, herbivoreIdx, false);
            nodelink.SetIfNodeCanBeTarget(herbivoreIdx, false);
            nodelink.SetIfNodeCanBeSource(producerIdx, false);

            AttachSmellyListener<int, bool, GameObject>(inspector, "OnSpawned", ListenForIdxs2);
            AttachSmellyListener(nodelink, "OnLayedOut", ()=>CheckChainOfHeight(2, ()=>ExplainChainOfTwo(1.5f)));
        }
        int carnivoreIdx=-1;
        void ListenForIdxs2(int idx, bool isProducer, GameObject gameObject)
        {
            carnivoreIdx = idx;
        }
        void ExplainChainOfTwo(float delay)
        {
            DetachSmellyListeners();
            Assert.IsTrue(carnivoreIdx!=-1);

            nodelink.ForceUnfocus();
            help.Showing = false;
            StartCoroutine(WaitThenDo(delay, ()=>{ help.Message = "Great job. It takes two links to get from the plant to the second animal, and so the maximum chain of your ecosystem is two. However, you will notice that it cannot survive! This is because biomass is lost at each level of an ecosystem, and so the top species does not have enough food. Try saving it, but without adding any more links."; help.Showing = true; inspector.HideSizeSlider(false); }));

            // inspector.FixSpeciesSize(carnivoreIdx);
            nodelink.SetIfLinkRemovable(herbivoreIdx, carnivoreIdx, false);

            AttachSmellyListener(model, "OnEquilibrium", ()=>{if (model.Feasible) ExplainTopSurvival(1.5f); });
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

            recorder.Hide(false);
            constraints.ConstrainChain(2);
            nodelink.SetIfNodeCanBeTarget(herbivoreIdx, true);
        }

        
        IEnumerator WaitThenDo(float seconds, Action Todo)
        {
            yield return new WaitForSeconds(seconds);
            Todo();
        }
    }
}