using System;
using System.Collections;
using UnityEngine;

namespace EcoBuilder.Levels
{
    // this tutorial teaches the greed slider and superfocus
    public class Tutorial6 : Tutorial
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

            help.SetText("Let's put your skills to the test! Try to construct the best ecosystem you can, given the constraints shown in the left. Here you must add two plants, four animals, and have at least 6 interactions between them. If you get stuck and do not know why, then you can press and hold this panel to the left to receive an explanation. There is one more rule: any two animals also cannot have the same weight. Good luck!");

            Detach?.Invoke();
        }
        IEnumerator WaitThenDo(float seconds, Action Todo)
        {
            yield return new WaitForSeconds(seconds);
            Todo();
        }
    }
}