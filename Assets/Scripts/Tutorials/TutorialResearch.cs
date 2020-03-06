using System;
using System.Collections;
using UnityEngine;

namespace EcoBuilder.Tutorials
{
    // this tutorial teaches the greed slider and superfocus
    public class TutorialResearch : Tutorial
    {
        protected override void StartLesson()
        {
            targetSize = new Vector2(100,100);

            ExplainIntro();
        }
        void ExplainIntro()
        {
            targetSize = new Vector2(100,100);
            targetPos = new Vector2(100,-220);
            targetAnchor = new Vector2(0,1);
            targetZRot = 30;

            help.Message = "Well done for finishing Learning World! This is where the true challenge begins, as you will now attempt to score challenges. There is one more rule: any two animals also cannot have the same weight. Good luck!";

            DetachSmellyListeners();
            print("TODO: 1 start with a simple two species system, 2 then show the effects of interference (examples of exclusion/competition), 3 then add another species and show the conflicts, 4 explain superfocus");
        }
        IEnumerator WaitThenDo(float seconds, Action Todo)
        {
            yield return new WaitForSeconds(seconds);
            Todo();
        }
    }
}