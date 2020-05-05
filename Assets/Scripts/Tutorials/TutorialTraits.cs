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
            inspector.HideInitiatorButtons();
            inspector.HideRemoveButton();
            score.Hide();
            recorder.Hide();
            constraints.Hide();
            score.DisableStarCalculation(true);

            targetSize = new Vector2(100,100);
            // targetZRot = 45;  

            var nodes = graph.gameObject.GetComponentsInChildren<NodeLink.Node>();
            Assert.IsTrue(nodes.Length == 2);

            plant = nodes[0];
            animal = nodes[1];

            graph.SetIfNodeCanBeFocused(animal.Idx, false);

            ExplainIntro(false);
        }
        void ExplainIntro(bool showText)
        {
            StopAllCoroutines();
            inspector.HideStatusBars();

            // in case the user goes back
            if (showText)
            {
                help.Message = GameManager.Instance.PlayedLevelDetails.Introduction;
                help.Showing = true;
                help.ResetLevelPosition();
            }
            
            smoothTime = .3f;
            Point();
            Track(plant.transform);

            DetachSmellyListeners();
            AttachSmellyListener<int>(graph, "OnNodeTapped", i=>ExplainSize());
        }

        void ExplainSize()
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            inspector.HideStatusBars(false);
            help.Message = "The bars next to your species indicate their health, and the number indicates its weight, which can change by dragging this slider. Here, the animal is going extinct because it is not getting enough food. See if you can save it!";
            help.Showing = true;
            help.SetSide(true);
            help.SetAnchorHeight(.95f);

            ShuffleOnSlider(3, 40);

            AttachSmellyListener<int>(model, "OnRescued", i=>ExplainMetabolism(2));
            AttachSmellyListener(graph, "OnUnfocused", ()=>ExplainIntro(true));
        }
        void ExplainMetabolism(float delay)
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            graph.ForceUnfocus();

            graph.SetIfNodeCanBeFocused(animal.Idx, true);
            graph.SetIfNodeCanBeFocused(plant.Idx, false);

            targetSize = Vector2.zero;
            smoothTime = .3f;
            
            help.Showing = false;
            WaitThenDo(delay, ()=>{ help.Message = "Well done! You saved the animal here by giving it more food. This is achieved by its food source lighter, as lighter species grow faster. This is exactly what happens in the real world! For example, an Oak tree takes many years to grow, while grass can cover a field within weeks. Try tapping the animal this time."; help.SetPixelWidth(450, false); help.Showing = true; Point(); Track(animal.transform); targetSize = new Vector2(100,100); });

            AttachSmellyListener<int>(graph, "OnNodeTapped", i=>ExplainInterference());
        }
        void ExplainInterference()
        {
            DetachSmellyListeners();
            StopAllCoroutines();
            help.Showing = false;

            help.Message = "The same concept applies to animals, where lighter animals eat much faster than heavier ones. For example, a swarm of locusts devours a field much faster than a herd of cows. Here, heavier species have darker colours. Try changing the animal here in order to make it go extinct again.";
            help.Showing = true;

            ShuffleOnSlider(3, 40);

            AttachSmellyListener(graph, "OnUnfocused", ()=>ExplainMetabolism(0));
            AttachSmellyListener<int>(model, "OnEndangered", i=>ExplainScore());
        }
        void ExplainScore()
        {
            DetachSmellyListeners();
            StopAllCoroutines();
            targetSize = new Vector2(0,0);
            smoothTime = .2f;

            recorder.gameObject.SetActive(true); 
            graph.SetIfNodeCanBeFocused(plant.Idx, true);
            graph.ForceUnfocus();

            help.Showing = false;
            WaitThenDo(2, ()=>{ help.Message = "Good job! This bar at the top displays your score, which is determined by the number of species, the number of links, and the total health of every species. Make both species survive again to complete this level!"; score.Hide(false); help.SetSide(false,false); help.Showing = true; help.SetAnchorHeight(.85f); score.DisableStarCalculation(false); }); 
        }
    }
}