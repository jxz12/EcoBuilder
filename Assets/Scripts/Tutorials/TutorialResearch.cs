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
            targetPos = new Vector2(-61,115) * hud.BottomScale;
            targetAnchor = new Vector2(1,0);
            targetZRot = 315;

            score.Hide();
            score.DisableStarCalculation(true);
            recorder.Hide();

            inspector.HideRemoveButton();
            inspector.HideSizeSlider();
            inspector.HideGreedSlider();
            inspector.SetConsumerAvailability(false);
            inspector.AllowConflicts();
            inspector.FixGreedInitialValue();
            inspector.FixGreedInitialValue();
            
            graph.AllowSuperfocus = false;

            AttachSmellyListener(inspector, "OnIncubated", ()=>{ targetSize=Vector2.zero; help.Showing=false; });
            AttachSmellyListener(graph, "OnEmptyTapped", ()=>{ targetSize=new Vector2(100,100); help.Showing=true; });
            AttachSmellyListener<int, bool, GameObject>(inspector, "OnSpawned", (i,b,g)=>{ plantIdx=i; ExplainInterference(); });
        }
        int plantIdx=-1;
        float plantSize=-1, plantGreed=-1;
        void ExplainInterference()
        {
            DetachSmellyListeners();

            inspector.HideSizeSlider(false);
            inspector.HideGreedSlider(false);
            inspector.SetProducerAvailability(false);

            help.Showing = false;
            targetSize = Vector2.zero;
            WaitThenDo(1f, ()=>{ help.Message = "Look, there is a new slider available! This new trait is called 'interference', and measures how much a species competes with itself. Low interference results in a high population, and a high interference results in a low population. Try dragging both sliders down as low as possible."; help.Showing=true; ShuffleOnSlider(3, 40); });

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
            AttachSmellyListener(graph, "OnEmptyTapped", ()=>targetSize=Vector2.zero);
            AttachSmellyListener<int>(graph, "OnFocused", (i)=>targetSize=new Vector2(50,50));
            AttachSmellyListener<int, float>(inspector, "OnSizeSet", (i,x)=>TrackPlantTrait(true, x));
            AttachSmellyListener<int, float>(inspector, "OnGreedSet", (i,x)=>TrackPlantTrait(false, x));
        }
        void ExplainThiccPlant()
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            graph.ForceUnfocus();
            inspector.SetConsumerAvailability(true);
            graph.SetIfNodeCanBeFocused(plantIdx, false);

            targetSize = Vector2.zero;
            smoothTime = .2f;
            help.Showing = false;
            WaitThenDo(1f, ()=>{ help.Message = "This causes plants to have the maximum population possible! This is useful if you think your animals need more food. Now try adding an animal."; help.Showing = true; targetAnchor = new Vector2(1,0); targetPos = new Vector2(-61, 60) * hud.BottomScale; targetZRot = 270; targetSize=new Vector2(100,100); Point(); });

            AttachSmellyListener(inspector, "OnIncubated", ()=>{ targetSize=Vector2.zero; help.Showing=false; });
            AttachSmellyListener(graph, "OnEmptyTapped", ()=>{ targetSize=new Vector2(100,100); help.Showing=true; });
            AttachSmellyListener<int, bool, GameObject>(inspector, "OnSpawned", (i,b,g)=>{ animalIdx=i; ExplainAnimal(); });
        }

        int animalIdx=-1;
        float animalSize=-1, animalGreed=-1;
        void ExplainAnimal()
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            inspector.SetConsumerAvailability(false);

            help.Message = "And try dragging both sliders as high as possible.";
            help.Showing = true;

            void TrackAnimalTrait(bool sizeOrGreed, float val)
            {
                if (sizeOrGreed) {
                    animalSize = val;
                } else {
                    animalGreed = val;
                }
                if (animalSize==0 && animalGreed==0) {
                    ExplainConflict1();
                }
            }
            AttachSmellyListener<int, float>(inspector, "OnSizeSet", (i,x)=>TrackAnimalTrait(true, x));
            AttachSmellyListener<int, float>(inspector, "OnGreedSet", (i,x)=>TrackAnimalTrait(false, x));
        }
        int plantIdx2 = -1;
        void ExplainConflict1()
        {
            DetachSmellyListeners();

            graph.ForceUnfocus();
            graph.SetIfNodeCanBeFocused(animalIdx, false);
            inspector.SetProducerAvailability(true);

            help.Showing = false;
            // point at plant again
            WaitThenDo(1f, ()=>{ targetSize = new Vector2(100,100); targetPos = new Vector2(-61,115) * hud.BottomScale; targetAnchor = new Vector2(1,0); help.Message = "Great job! A plant with low interference can enable even the heaviest of species to survive. Also notice how much higher your score is! Try adding one final plant."; help.Showing = true; });

            AttachSmellyListener(inspector, "OnIncubated", ExplainConflict2);
        }
        void ExplainConflict2()
        {
            help.SetSide(true);
            help.Message = "There is one more rule in Research world that you must follow: no two species can be identical! Try making the new plant identical to the first by dragging both sliders as low as they can go.";
            help.Showing = true;


            AttachSmellyListener<int, string>(inspector, "OnConflicted", (i,s)=>ExplainConflict3());
        }
        void ExplainConflict3()
        {
            help.Showing = false;
            WaitThenDo(1f, ()=>{ help.Message = "Uh oh! If you try to make two identical species, the original will be outlined with a message telling you that it is not possible. Now try making the animal eat both plants."; help.Showing = true; });
        }
        void ExplainSuperFocus1()
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            graph.SetIfNodeCanBeFocused(animalIdx, false);
            help.SetSide(false);
            graph.AllowSuperfocus = true;
            help.Message = "Research World also unlocks a final tool which is useful in case your ecosystem becomes very complex. If you tap a species twice, the game will show only on the direct connections to it. Try that now.";
            help.Showing = true;

            // TODO: track ome species
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

            inspector.HideRemoveButton();
            inspector.SetProducerAvailability(true);
            inspector.SetConsumerAvailability(true);
            inspector.HideSizeSlider(false);
            inspector.HideGreedSlider(false);
            inspector.UnfixSizeInitialValue();
            inspector.UnfixGreedInitialValue();
            inspector.AllowConflicts(false);
            graph.AllowSuperfocus = true;

            help.SetPixelWidth(400);
            help.Message = "The final feature of Research World is the new scoring system! You will be ranked against players from around the world on how well you score, with your current rank shown in the top left. The higher you score, the more your strategies will help researchers understand ecosystems, so please try your best, and good luck!";

        }
    }
}