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
            Detach?.Invoke();
            // ask the user to make a chain of three first
            
            nodelink.OnConstraints += ()=> CheckChainOfHeight(3, ExplainChainThree);
        }
        void CheckChainOfHeight(int heightGoal, Action Todo)
        {
            if (nodelink.MaxChain == heightGoal)
                Todo.Invoke();
        }
        void ExplainChainThree()
        {
            // now make them connect the lowest animal to the highest
            // disallow other actions somehow
            nodelink.OnConstraints += ()=> CheckLoopOfLength(3, ExplainLoopThree);
        }
        void CheckLoopOfLength(int lengthGoal, Action Todo)
        {
            if (nodelink.MaxLoop == lengthGoal)
                Todo.Invoke();
        }
        void ExplainLoopThree()
        {
            // ask them to try and make a loop of 4
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