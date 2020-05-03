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

            ExplainIntro(false);
        }
        void ExplainIntro(bool reshowText)
        {
            DetachSmellyListeners();
            targetSize = new Vector2(100,100);
            targetPos = new Vector2(100,-220);
            targetAnchor = new Vector2(0,1);
            targetZRot = 30;

            help.Message = "Well done for finishing Learning World! This is where the true challenge begins, as you will now attempt to take on players from around the world in problems that even researchers do not know the solution to! But first, there are some extra rules that you will need to know. Start by adding a plant to your ecosystem, or skip this tutorial at any time by TODO: spawn a skip button and put it somewhere on the screen";

            AttachSmellyListener(inspector, "OnIncubated", ExplainInterference);
        }
        void ExplainInterference()
        {
            help.Message = "Look, there is a new slider available! This new trait is called 'interference', and measures the amount that individuals in a species compete with each other. Try adding the plant to your ecosystem to start tweaking it.";

            AttachSmellyListener<int, bool, GameObject>(inspector, "OnSpawn", (i,b,g)=>ExplainInterference());
            AttachSmellyListener(graph, "OnEmptyTapped", ()=>ExplainIntro(true));
        }
        void ExplainRegulation()
        {
            help.Message = "Try";

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