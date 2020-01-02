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
            Detach?.Invoke();
            // ask the user to make a chain of three first

            Action foo = ()=> CheckChainOfHeight(3, ExplainChainThree);
            nodelink.OnConstraints += foo;
            Detach = ()=> nodelink.OnConstraints -= foo;
        }
        void CheckChainOfHeight(int heightGoal, Action Todo)
        {
            if (nodelink.MaxChain == heightGoal)
                Todo.Invoke();
        }
        void ExplainChainThree()
        {
            Detach?.Invoke();
            help.SetText("Now make the lowest animal eat the top animal");
            help.Show(true);

            // now make them connect the lowest animal to the highest
            // disallow other actions somehow
            Action foo = ()=> CheckLoopOfLength(3, ExplainLoopThree);
            nodelink.OnConstraints += foo;
            Detach = ()=> nodelink.OnConstraints -= foo;
        }
        void CheckLoopOfLength(int lengthGoal, Action Todo)
        {
            if (nodelink.MaxLoop == lengthGoal)
                Todo.Invoke();
        }
        void ExplainLoopThree()
        {
            Detach?.Invoke();
            help.SetText("Good! Now add another animal to make a loop of 4.");
            help.Show(true);
            // ask them to try and make a loop of 4

            Action foo = ()=> CheckLoopOfLength(4, ExplainLoopFour);
            nodelink.OnConstraints += foo;
            Detach = ()=> nodelink.OnConstraints -= foo;
        }
        void ExplainLoopFour()
        {
            Detach?.Invoke();
            help.SetText("Yay!");
            help.Show(true);
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