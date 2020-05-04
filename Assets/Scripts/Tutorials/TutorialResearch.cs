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

            AttachSmellyListener(inspector, "OnIncubated", ExplainInterference1);
        }
        void ExplainInterference1()
        {
            help.Message = "Look, there is a new slider available! This new trait is called 'interference', and measures the amount that individuals in a species compete with each other. Try adding the plant to your ecosystem to start tweaking it.";

            AttachSmellyListener<int, bool, GameObject>(inspector, "OnSpawn", (i,b,g)=>ExplainInterference2());
            AttachSmellyListener(graph, "OnEmptyTapped", ()=>ExplainIntro(true));
        }
        void ExplainInterference2()
        {
            help.Message = "The higher the value of the slider, the more the species interferes with itself. This means that a low interference results in a high population, and a high interference results in a low population. Try dragging it as low as possible.";

            // attach next to spawn
            // attach hide message to incubation
        }
        void ExplainHealthyPlant()
        {
            help.Message = "Your plant has a very high population! Interference has a large effect on plants, and you will usually want to keep it as low as possible. Now try adding an animal.";
        }
        void ExplainAnimal()
        {
            help.Message = "And this time try... TODO: super healthy animal too. Interference is not well understood by even researchers, so try different combinations to see what works best!";
        }
        void ExplainConflict1()
        {
            help.Message = "Great job! There is one more rule inside Research World that you must follow. Try adding another plant to see it.";
        }
        void ExplainConflict2()
        {
            help.Message = "The rule is that no two species can be identical. Try moving making the new plant identital to the first.";
        }
        void ExplainConflict3()
        {
            help.Message = "Uh oh! If you try to make two identical species, the original will be outlined with a message telling you that it is not possible. Now try making the animal eat both plants.";
        }
        void ExplainSuperFocus1()
        {
            graph.AllowSuperfocus = true;
            help.Message = "Research World also unlocks a final tool to explore your ecosystem, which is useful in case your ecosystem becomes very complex. If you tap a species twice, the game will focus only on the direct connections to that species. Try tapping this plant twice.";

            // TODO: disable clicking on other two plants
        }
        void ExplainSuperFocus2()
        {
            help.Message = "The first plant has shrunk in order for you to get a more zoomed in picture of your ecosystem. From here you can navigate through your ecosystem one species at a time. Try tapping the animal";
        
            // TODO: back to normal focus foes back to prev step

        }
        void ExplainSuperFocus3()
        {
            help.Message = "And now try tapping the other plant";
        }
        void ExplainSuperFocus4()
        {
            help.Message = "Great job! You can exit this view at any time by simply tapping the background.";
        }
        void ExplainAlternateScore()
        {
            // TODO: show score
            help.Message = "The final feature of Research World is the a scoring system! You will be ranked against players from around the world on how well you score, with your current rank shown in the top left. The higher you score, the more your strategies will help researchers understand ecosystems, so please try your best, and good luck!";
        }
    }
}