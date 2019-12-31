using System;
using System.Collections;
using UnityEngine;

namespace EcoBuilder.Levels
{
    // this tutorial teaches the concept of a chain
    public class Tutorial4 : Tutorial
    {
        protected override void StartLesson()
        {
            targetSize = Vector2.zero;

            ExplainIntro();
        }
        void ExplainIntro()
        {
            Detach?.Invoke();

            // disallow animal on animal, disallow plan on plant
            //   to ensure only one of each is in system

            Action foo = ()=> CheckChainOfHeight(1, ExplainChainOfOne);
            nodelink.OnConstraints += foo;
            Detach = ()=> nodelink.OnConstraints -= foo;
        }
        void CheckChainOfHeight(int heightGoal, Action Todo)
        {
            if (nodelink.MaxChain == heightGoal)
                Todo.Invoke();
        }
        void ExplainChainOfOne()
        {
            Detach?.Invoke();
            help.SetText("chain of one done! Now do two");
            help.Show(true);
            // allow animal again, but only one more
            // ask to make a chain of 2

            Action foo = ()=> CheckChainOfHeight(2, ExplainChainOfTwo);
            nodelink.OnConstraints += foo;
            Detach = ()=> nodelink.OnConstraints -= foo;
        }
        void ExplainChainOfTwo()
        {
            Detach?.Invoke();
            help.SetText("chain of two done! Now do one again");
            help.Show(true);
            // now that there is a chain of 2, ask them to reduce it again by making a triangle

            Action foo = ()=> CheckChainOfHeight(1, ExplainBackToOne); // TODO: make sure it's a triangle
            nodelink.OnConstraints += foo;
            Detach = ()=> nodelink.OnConstraints -= foo;
        }
        void ExplainBackToOne()
        {
            Detach?.Invoke();
            help.SetText("triangle! Now do three to finish the level");
            // ask the user to make a chain of three with only one more 
            Action foo = ()=> CheckChainOfHeight(3, ExplainThree);
            nodelink.OnConstraints += foo;
            Detach = ()=> nodelink.OnConstraints -= foo;
        }
        void ExplainThree()
        {
            Detach?.Invoke();
            nodelink.ForceUnfocus();
            help.Show(false);
            StartCoroutine(WaitThenDo(2, ()=>{ help.Show(true); help.SetText("Well done! You now understand chains.");}));
            // TODO: enable mass editing only now?
        }
        
        IEnumerator WaitThenDo(float seconds, Action Todo)
        {
            yield return new WaitForSeconds(seconds);
            Todo();
        }
    }
}