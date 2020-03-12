using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace EcoBuilder.Tutorials
{
    // this tutorial teaches the size slider and its effects
    public class TutorialTraits : Tutorial
    {
        NodeLink.Node plant, animal;
        protected override void StartLesson()
        {
            inspector.HideIncubatorButtons();
            inspector.HideRemoveButton();
            score.Hide();
            recorder.Hide();
            constraints.Hide();
            score.DisableStarCalculation(true);
            targetSize = new Vector2(100,100);
            // targetZRot = 45;  

            var nodes = nodelink.gameObject.GetComponentsInChildren<NodeLink.Node>();
            Assert.IsTrue(nodes.Length == 2);

            plant = nodes[0];
            animal = nodes[1];

            nodelink.SetIfNodeCanBeFocused(animal.Idx, false);

            ExplainIntro(false);
        }
        void ExplainIntro(bool showText)
        {
            StopAllCoroutines();

            // in case the user goes back
            if (showText)
            {
                help.Message = GameManager.Instance.PlayedLevelDetails.Introduction;
                help.Showing = true;
                help.ResetPosition();
            }
            
            smoothTime = .3f;
            Point();
            StartCoroutine(Track(plant.transform));

            DetachSmellyListeners();
            AttachSmellyListener<int>(nodelink, "OnFocused", i=>ExplainSize());
        }

        void ExplainSize()
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            help.Message = "You can change the weight of your species by moving this slider. Here, your animal is going extinct because it is not getting enough food. See if you can save it!";
            help.Showing = true;
            help.SetSide(true);
            help.SetAnchorHeight(.95f);

            StartCoroutine(ShuffleOnSlider(3, 40));

            AttachSmellyListener<int>(model, "OnRescued", i=>ExplainMetabolism(2));
            AttachSmellyListener<int>(nodelink, "OnUnfocused", i=>ExplainIntro(true));
        }
        void ExplainMetabolism(float delay)
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            nodelink.ForceUnfocus();

            nodelink.SetIfNodeCanBeFocused(animal.Idx, true);
            nodelink.SetIfNodeCanBeFocused(plant.Idx, false);

            targetSize = Vector2.zero;
            smoothTime = .3f;
            
            help.Showing = false;
            StartCoroutine(WaitThenDo(delay, ()=>{ help.Message = "Well done! You saved the animal here by giving it more food. This is achieved by its food source lighter, as lighter species grow faster. This is exactly what happens in the real world! For example, an Oak tree takes many years to grow, while grass can cover a field within weeks. Try tapping the animal this time."; help.SetPixelWidth(450, false); help.Showing = true; Point(); StartCoroutine(Track(animal.transform)); targetSize = new Vector2(100,100); }));

            AttachSmellyListener<int>(nodelink, "OnFocused", i=>ExplainInterference());
        }
        void ExplainInterference()
        {
            DetachSmellyListeners();
            StopAllCoroutines();
            help.Showing = false;

            help.Message = "The same concept applies to animals, where lighter animals eat much faster than heavier ones. For example, a swarm of locusts devours a field much faster than a herd of cows. You should also notice that heavier species have darker colours, so that you can tell how heavy a species is just by a glance. Try changing the animal here in order to make it go extinct again.";
            help.Showing = true;

            StartCoroutine(ShuffleOnSlider(3, 40));

            AttachSmellyListener<int>(nodelink, "OnUnfocused", i=>ExplainMetabolism(0));
            AttachSmellyListener<int>(model, "OnEndangered", i=>ExplainScore());
        }
        void ExplainScore()
        {
            DetachSmellyListeners();
            StopAllCoroutines();
            help.Showing = false;
            targetSize = new Vector2(0,0);
            smoothTime = .2f;

            recorder.gameObject.SetActive(true); 

            nodelink.SetIfNodeCanBeFocused(plant.Idx, true);

            nodelink.ForceUnfocus();
            help.SetSide(false,false);
            help.SetAnchorHeight(.85f);

            print("TODO: fix the report card");
            StartCoroutine(WaitThenDo(2, ()=>{ help.Message = "Good job! This bar at the top displays your score, which is determined by the number of species, the number of links, and the total health of every species. Make both species survive again to complete this level!"; help.Showing = true; score.Hide(false); score.DisableStarCalculation(false); }));
        }
    }
}