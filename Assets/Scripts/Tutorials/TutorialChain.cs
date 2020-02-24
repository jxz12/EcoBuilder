using System;
using System.Collections;
using UnityEngine;

namespace EcoBuilder.Tutorials
{
    public class TutorialChain : Tutorial
    {
        protected override void StartLesson()
        {
            targetSize = Vector2.zero;
            recorder.gameObject.SetActive(false);

            ExplainIntro();
        }
        void ExplainIntro()
        {
            DetachSmellyListeners();
            AttachSmellyListener(nodelink, "OnLayedOut", ()=>CheckChainOfHeight(1, ExplainChainOfOne));
            AttachSmellyListener(nodelink, "OnLayedOut", ()=>LimitNumAnimals(1));
        }
        void CheckChainOfHeight(int heightGoal, Action Todo)
        {
            if (nodelink.MaxChain == heightGoal) {
                Todo.Invoke();
            }
        }
        // ensure only one animal is in system at beginning
        void LimitNumAnimals(int limit)
        {
            if (constraints.PawValue >= limit) {
                inspector.SetConsumerAvailability(false);
            } else {
                inspector.SetConsumerAvailability(true);
            }
        }
        void ExplainChainOfOne()
        {
            help.Message = "You should notice that the number on the left has changed! This is because the animal is one link away from the plant, which means it has a chain of one. Try adding another animal and making it eat the current one.";
            help.Showing = true;
            inspector.SetConsumerAvailability(true);
            // allow animal again, but only one more
            // ask to make a chain of 2
            print("TODO: make plant uninteractable (cannot be source) for now");

            DetachSmellyListeners();
            AttachSmellyListener(nodelink, "OnLayedOut", ()=>CheckChainOfHeight(2, ExplainChainOfTwo));
        }
        void ExplainChainOfTwo()
        {
            help.Message = "Great job. It takes two hops to get from the plant to the second animal, and so the maximum chain of your ecosystem is two. Now try making the second animal also eat the plant.";
            help.Showing = true;
            // now that there is a chain of 2, ask them to reduce it again by making a triangle

            DetachSmellyListeners();
            AttachSmellyListener(nodelink, "OnLayedOut", ()=>CheckChainOfHeight(1, ExplainBackToOne));
        }
        void ExplainBackToOne()
        {
            help.Message = "The max chain of your ecosystem has fallen back to one! This is because the height of any given species only considers its shortest path to any plant, and so the path going through both animals is overridden by the path from the plant. To finish this level, create an ecosystem with a chain of three!";
            // ask the user to make a chain of three with only one more 

            DetachSmellyListeners();
            // AttachSmellyListener(nodelink, "OnLayedOut", ()=> CheckChainOfHeight(3, ExplainThree));
        }
        // void ExplainThree()
        // {
        //     nodelink.ForceUnfocus();
        //     help.Showing = false;
        //     StartCoroutine(WaitThenDo(2, ()=>{ help.Showing = true; help.Message = "Well done! You should now understand the concept of chain length. At any point, if you press the chain icon in the panel to the left you can highlight your species with the longest chain. Construct a chain of length four to finish this level!"; }));

        //     constraints.ConstrainLoop(4);

        //     print("TODO: enable mass editing only now?");

        //     DetachSmellyListeners();
        // }
        
        IEnumerator WaitThenDo(float seconds, Action Todo)
        {
            yield return new WaitForSeconds(seconds);
            Todo();
        }
    }
}