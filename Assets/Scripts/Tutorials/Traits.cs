using System;
using System.Collections;
using UnityEngine;

namespace EcoBuilder.Tutorials
{
    // this tutorial teaches the size slider and its effects
    public class Traits : Tutorial
    {
        GameObject plant, animal;
        protected override void StartLesson()
        {
            incubator.HideStartButtons(true);
            inspector.HideRemoveButton(true);
            score.HideScore(true);
            score.HideConstraints(true);
            score.DisableStarCalculation(true);
            targetSize = new Vector2(100,100);
            targetZRot = 45;

            var nodes = nodelink.gameObject.GetComponentsInChildren<NodeLink.Node>();
            animal = nodes[1].gameObject;
            plant = nodes[0].gameObject;

            ExplainIntro();
        }
        void ExplainIntro()
        {
            DetachSmellyListeners();
            StopAllCoroutines();
            
            StartCoroutine(Track(plant.transform));
            AttachSmellyListener<int>(nodelink, "OnFocused", i=>ExplainSize());
        }

        void ExplainSize()
        {
            DetachSmellyListeners();
            StopAllCoroutines();
            help.Message = "You can change the weight of your species by moving this slider. Here, your animal is going extinct because it is not getting enough food. See if you can save it!";
            help.Showing = true;
            help.SetSide(true, false);
            help.SetAnchorHeight(.95f);

            StartCoroutine(ShuffleOnSlider(3, 40));

            AttachSmellyListener<int>(model, "OnRescued", i=>ExplainMetabolism(2));
            AttachSmellyListener(nodelink, "OnUnfocused", ExplainIntro);
        }
        void ExplainMetabolism(float delay)
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            help.Showing = false;
            // inspector.Uninspect();
            nodelink.ForceUnfocus();

            smoothTime = .3f;
            targetSize = Vector2.zero;

            StartCoroutine(WaitThenDo(delay, ()=>{ help.Message = "Well done! You saved the animal here by giving it more food. This is achieved by its food source lighter, as lighter species grow faster. This is exactly what happens in the real world! For example, an Oak tree takes many years to grow, while grass can cover a field within weeks. Try tapping the animal this time."; help.SetPixelWidth(600); help.Showing = true; StartCoroutine(Track(animal.transform)); targetSize = new Vector2(100,100); }));

            AttachSmellyListener<int>(nodelink, "OnFocused", i=>ExplainInterference());
        }
        void ExplainInterference()
        {
            DetachSmellyListeners();
            StopAllCoroutines();
            help.Showing = false;

            help.Message = "The same concept applies to animals, where lighter animals eat much faster than heavier ones. For example, a swarm of locusts devours a field much faster than a herd of cows. Try changing the animal here in order to make it go extinct again.";
            help.Showing = true;

            StartCoroutine(ShuffleOnSlider(3, 40));

            AttachSmellyListener(nodelink, "OnUnfocused", ()=>ExplainMetabolism(0));
            AttachSmellyListener<int>(model, "OnEndangered", i=>ExplainScore());
        }
        void ExplainScore()
        {
            DetachSmellyListeners();
            StopAllCoroutines();
            help.Showing = false;
            targetSize = new Vector2(0,0);
            smoothTime = .2f;

            // inspector.Uninspect();
            nodelink.ForceUnfocus();
            help.SetSide(false,false);
            help.SetAnchorHeight(.9f);

            print("TODO: fix the report card");
            StartCoroutine(WaitThenDo(2, ()=>{ help.Message = "Good job! This bar at the top displays your score, which is determined by the number of species and their populations, shown by the health bars next to each species. You can tap your score to get a detailed report of what is coming from where. Make both species survive again to complete this level!"; help.Showing = true; score.HideScore(false); score.DisableStarCalculation(false); }));
            AttachSmellyListener<Level>(GameManager.Instance.PlayedLevel, "OnFinished", l=>Finish());
        }
        void Finish()
        {
            // should be dealt with by PlayManager
            // help.SetPixel(.7f);
            // help.SetDistFromTop(.25f);
        }
        IEnumerator WaitThenDo(float seconds, Action Todo)
        {
            if (seconds > 0) {
                yield return new WaitForSeconds(seconds);
            }
            Todo();
        }
        IEnumerator Track(Transform tracked)
        {
            targetAnchor = new Vector2(0,0);
            targetSize = new Vector2(100,100);
            smoothTime = .2f;
            Point();
            while (true)
            {
                targetPos = ScreenPos(Camera.main.WorldToViewportPoint(tracked.position)) + new Vector2(0,-20);
                yield return null;
            }
        }
        IEnumerator ShuffleOnSlider(float period, float yPos)
        {
            float start = Time.time - period/4;
            targetAnchor = new Vector2(.5f, 0);
            targetSize = new Vector2(50,50);
            targetZRot = 0;
            smoothTime = .7f;
            Grab();
            while (true)
            {
                if (((Time.time - start) % period) < (period/2f))
                {
                    targetPos = new Vector2(-60,yPos);
                }
                else
                {
                    targetPos = new Vector2(130,yPos);
                    smoothTime = 1f;
                }
                yield return null;
            }
        }
    }
}