using System;
using System.Collections;
using UnityEngine;

namespace EcoBuilder.Tutorials
{
    public class Chain : Tutorial
    {
        protected override void StartLesson()
        {
            targetSize = Vector2.zero;

            ExplainIntro();
        }
        void ExplainIntro()
        {
            DetachSmellyListeners();
            AttachSmellyListener(nodelink, "OnConstraints", ()=>CheckChainOfHeight(1, ExplainChainOfOne));
            AttachSmellyListener(nodelink, "OnConstraints", ()=>LimitNumAnimals(1));
        }
        void CheckChainOfHeight(int heightGoal, Action Todo)
        {
            if (nodelink.MaxChain == heightGoal)
                Todo.Invoke();
        }
        // ensure only one animal is in system at beginning
        void LimitNumAnimals(int limit)
        {
            if (constraints.GetValue("Paw") >= limit)
                incubator.SetConsumerAvailability(false);
            else
                incubator.SetConsumerAvailability(true);
        }
        void ExplainChainOfOne()
        {
            help.Message = "chain of one done! Now do two";
            help.Showing = true;
            incubator.SetConsumerAvailability(true);
            // allow animal again, but only one more
            // ask to make a chain of 2

            DetachSmellyListeners();
            AttachSmellyListener(nodelink, "OnConstraints", ()=>CheckChainOfHeight(2, ExplainChainOfTwo));
        }
        void ExplainChainOfTwo()
        {
            help.Message = "chain of two done! Now do one again";
            help.Showing = true;
            // now that there is a chain of 2, ask them to reduce it again by making a triangle

            DetachSmellyListeners();
            AttachSmellyListener(nodelink, "OnConstraints", ()=>CheckChainOfHeight(1, ExplainBackToOne));
        }
        void ExplainBackToOne()
        {
            help.Message = "triangle! Now do three to finish the level";
            // ask the user to make a chain of three with only one more 

            DetachSmellyListeners();
            AttachSmellyListener(nodelink, "OnConstraints", ()=> CheckChainOfHeight(3, ExplainThree));
        }
        void ExplainThree()
        {
            nodelink.ForceUnfocus();
            help.Showing = false;
            StartCoroutine(WaitThenDo(2, ()=>{ help.Showing = true; help.Message = "Well done! You now understand chains."; }));
            print("TODO: enable mass editing only now?");

            DetachSmellyListeners();
        }
        
        IEnumerator WaitThenDo(float seconds, Action Todo)
        {
            yield return new WaitForSeconds(seconds);
            Todo();
        }
    }
}