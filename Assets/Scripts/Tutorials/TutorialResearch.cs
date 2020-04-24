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

            // ExplainIntro();
            targetSize = Vector2.zero;
        }
        void ExplainIntro()
        {
            DetachSmellyListeners();
            targetSize = new Vector2(100,100);
            targetPos = new Vector2(100,-220);
            targetAnchor = new Vector2(0,1);
            targetZRot = 30;

            help.Message = "Well done for finishing Learning World! This is where the true challenge begins, as you will now attempt to take on players from around the world in problems that even researchers do not know the solution to! But first, there are some extra rules that you will need to know. Start by pressing the plant in your ecosystem.";

            // attach next to focus
        }
        void ExplainInterference()
        {
            help.Message = "Look, there is a new slider available! This trait is called 'interference', and measures the amount that individuals in a species compete with each other. Try moving the new slider up to its minimum value.";
            // attach prev to unfocus
            // attach next to max
        }
        void ExplainRegulation()
        {

            help.Message = "Hopefully you noticed that the health of the plant went up! This is because the plant is not competing against itself as much for resources, such as sunlight or nutrients. Now try adding an animal to your ecosystem and making it eat the plant";

            // attach next to spawn
            // attach hide message to incubation
        }
        void ExplainAnimal1()
        {
            help.Message = "";
        }
        void ExplainConflicts()
        {
            print("TODO: 1 start with a simple two species system, 2 then show the effects of interference (examples of exclusion/competition), 3 then add another species and show the conflicts, 4 explain superfocus");
            help.Message = "There is one more rule: any two animals also cannot have the same weight. Good luck!";
        }
    }
}