using System;
using System.Collections;
using UnityEngine;

namespace EcoBuilder.Levels
{
    // this tutorial teaches the size slider and its effects
    public class Tutorial2 : Tutorial
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
            help.SetText("You can change the weight of your species by moving this slider. Here, your animal is going extinct because it is not getting enough food. See if you can save it!");
            help.Show(true);
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

            help.Show(false);
            // inspector.Uninspect();
            nodelink.ForceUnfocus();

            smoothTime = .3f;
            targetSize = Vector2.zero;

            StartCoroutine(WaitThenDo(delay, ()=>{
                help.SetText("Well done! You saved the animal here by giving it more food. This is achieved by its food source lighter, as lighter species grow faster. This is exactly what happens in the real world! For example, an Oak tree takes many years to grow, while grass can cover a field within weeks. Try tapping the animal this time."); help.SetPixelWidth(600); help.Show(true); StartCoroutine(Track(animal.transform)); targetSize = new Vector2(100,100);
            }));

            AttachSmellyListener<int>(nodelink, "OnFocused", i=>ExplainInterference());
        }
        void ExplainInterference()
        {
            DetachSmellyListeners();
            StopAllCoroutines();
            help.Show(false);

            help.SetText("The same concept applies to animals, where lighter animals eat much faster than heavier ones. For example, a swarm of locusts devours a field much faster than a herd of cows. Try changing the animal here in order to make it go extinct again.");
            help.Show(true);

            StartCoroutine(ShuffleOnSlider(3, 40));

            AttachSmellyListener(nodelink, "OnUnfocused", ()=>ExplainMetabolism(0));
            AttachSmellyListener<int>(model, "OnEndangered", i=>ExplainScore());
        }
        void ExplainScore()
        {
            DetachSmellyListeners();
            StopAllCoroutines();
            help.Show(false);
            targetSize = new Vector2(0,0);
            smoothTime = .2f;

            // inspector.Uninspect();
            nodelink.ForceUnfocus();
            help.SetSide(false,false);
            help.SetAnchorHeight(.9f);

            print("TODO: fix the report card");
            StartCoroutine(WaitThenDo(2, ()=>{
                help.SetText("Good job! This bar at the top displays your score, and is based on the size and health of your ecosystem. You can tap your score to get a detailed report of what is coming from where. Getting enough points will earn you more stars – good luck!"); help.Show(true); score.HideScore(false); score.DisableStarCalculation(false);
            }));
            AttachSmellyListener<Levels.Level>(GameManager.Instance.PlayedLevel, "OnFinished", l=>Finish());
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