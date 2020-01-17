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
            targetSize = Vector2.zero;

            ExplainIntro();
        }
        void ExplainIntro()
        {
            // ask the user to make a chain of three first

            DetachSmellyListeners();
            AttachSmellyListener(nodelink, "OnConstraints", ()=>CheckChainOfHeight(3, ExplainChainThree));
        }
        void CheckChainOfHeight(int heightGoal, Action Todo)
        {
            if (nodelink.MaxChain == heightGoal)
                Todo.Invoke();
        }
        void ExplainChainThree()
        {
            help.SetText("Now make the lowest animal eat the top animal");
            help.Show(true);
            // now make them connect the lowest animal to the highest
            print("TODO: disallow other actions somehow, and add track coroutine");

            DetachSmellyListeners();
            AttachSmellyListener(nodelink, "OnConstraints", ()=>CheckLoopOfLength(3, ExplainLoopThree));
        }
        void CheckLoopOfLength(int lengthGoal, Action Todo)
        {
            if (nodelink.MaxLoop == lengthGoal)
                Todo.Invoke();
        }
        void ExplainLoopThree()
        {
            help.SetText("Good! Now add another animal to make a loop of 4.");
            help.Show(true);
            // ask them to try and make a loop of 4

            DetachSmellyListeners();
            AttachSmellyListener(nodelink, "OnConstraints", ()=>CheckLoopOfLength(4, ExplainLoopFour));
        }
        void ExplainLoopFour()
        {
            help.SetText("Yay!");
            help.Show(true);

            DetachSmellyListeners();
        }
        // create another loop attached to the same one
        // task with 
        IEnumerator WaitThenDo(float seconds, Action Todo)
        {
            yield return new WaitForSeconds(seconds);
            Todo();
        }
    }
}