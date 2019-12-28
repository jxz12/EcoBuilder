using System;
using System.Collections;
using UnityEngine;

namespace EcoBuilder.Levels
{
    // this tutorial teaches the idea of loops
    public class Tutorial5 : Tutorial
    {
        protected override void StartLesson()
        {
            targetSize = new Vector2(100,100);

            ExplainIntro();
        }
        void ExplainIntro()
        {
            help.SetText("Let's put your skills to the test! Try to construct the best ecosystem you can, given the constraints shown in the left. Here you must add two plants, four animals, and have at least 6 interactions between them. If you get stuck and do not know why, then you can press and hold this panel to the left to receive an explanation. There is one more rule: any two animals also cannot have the same weight. Good luck!");

            Detach?.Invoke();
        }
        // create a loop of 3
        // create another loop attached to the same one
        // task with 
        IEnumerator WaitThenDo(float seconds, Action Todo)
        {
            yield return new WaitForSeconds(seconds);
            Todo();
        }
    }
}