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
            targetSize = Vector2.zero;
            ExplainIntro(false);
        }
        void ExplainIntro(bool reshowText)
        {
            // point at leaf icon
            targetSize = new Vector2(100,100);
            targetPos = new Vector2(-61,115);
            targetAnchor = new Vector2(1,0);

            score.Hide();
            score.DisableStarCalculation(true);
            recorder.Hide();

            inspector.HideSizeSlider();
            inspector.HideGreedSlider();
            inspector.SetConsumerAvailability(false);
            inspector.FixGreedInitialValue();

            AttachSmellyListener(inspector, "OnIncubated", ()=>targetSize=Vector2.zero);
            AttachSmellyListener(graph, "OnEmptyTapped", ()=>targetSize=new Vector2(100,100));
            AttachSmellyListener<int, bool, GameObject>(inspector, "OnSpawned", (i,b,g)=>ExplainInterference());
        }

        void ExplainInterference()
        {
            DetachSmellyListeners();
            help.Message = "Look, there is a new slider available! This new trait is called 'interference', and measures how much species competes with itself. Low interference results in a high population, and a high interference results in a low population. Try dragging both sliders down as low as possible.";

            inspector.HideSizeSlider(false);
            inspector.HideGreedSlider(false);
            inspector.SetProducerAvailability(false);

            WaitThenDo(1f, ()=>{ help.Showing=true; ShuffleOnSlider(3, 40); });

            float plantSize=-1, plantGreed=-1;
            void TrackPlantTrait(bool sizeOrGreed, float val)
            {
                if (sizeOrGreed) {
                    plantSize = val;
                } else {
                    plantGreed = val;
                }
                if (plantSize==0 && plantGreed==0) {
                    ExplainThiccPlant();
                }
            }
            AttachSmellyListener<int, float>(inspector, "OnSizeSet", (i,x)=>TrackPlantTrait(true, x));
            AttachSmellyListener<int, float>(inspector, "OnGreedSet", (i,x)=>TrackPlantTrait(false, x));
        }
        void ExplainThiccPlant()
        {
            DetachSmellyListeners();

            graph.ForceUnfocus();
            inspector.SetConsumerAvailability(true);

            help.Showing = false;
            WaitThenDo(1f, ()=>{ help.Message = "This causes plants to have the maximum population possible! This is useful if you think your animals need more food. Now try adding an animal."; help.Showing = true; });

            AttachSmellyListener<int, bool, GameObject>(inspector, "OnSpawned", (i,b,g)=>ExplainAnimal());
        }
        void ExplainAnimal()
        {
            DetachSmellyListeners();

            // point at paw button
            targetAnchor = new Vector2(1,0);
            targetPos = new Vector2(-61, 60);
            targetZRot = 270;

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
            score.DisableStarCalculation(false);
            score.Hide(false);
            inspector.UnfixGreedInitialValue();
            help.Message = "The final feature of Research World is the a scoring system! You will be ranked against players from around the world on how well you score, with your current rank shown in the top left. The higher you score, the more your strategies will help researchers understand ecosystems, so please try your best, and good luck!";

        }
    }
}