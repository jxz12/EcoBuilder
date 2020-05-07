using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace EcoBuilder.Tutorials
{
    // this tutorial teaches the greed slider and superfocus
    public class TutorialResearch : Tutorial
    {
        [SerializeField] Button skipPrefab;
        Button skipButton;
        bool skipped;
        protected override void StartLesson()
        {
            Hide();
            skipButton = Instantiate(skipPrefab, transform.parent);
            skipButton.onClick.AddListener(()=>{ skipped=true; ExplainAlternateScore(); });
            ExplainIntro();
        }
        void ExplainIntro()
        {
            // point at leaf icon
            targetAnchor = new Vector2(1,0);
            Point(new Vector2(-61,115) * hud.BottomScale, -45);

            score.Hide();
            score.DisableStarCalculation(true);
            recorder.Hide();

            inspector.HideRemoveButton();
            inspector.HideSizeSlider();
            inspector.HideGreedSlider();
            inspector.SetConsumerAvailability(false);
            inspector.AllowConflicts();
            inspector.FixSizeInitialValue();
            inspector.FixGreedInitialValue();
            
            graph.AllowSuperfocus = false;

            AttachSmellyListener(inspector, "OnIncubated", ()=>{ Hide(); help.Showing=false; });
            AttachSmellyListener(graph, "OnEmptyTapped", ()=>{ Point(); help.Showing=true; });
            AttachSmellyListener<int, bool, GameObject>(inspector, "OnSpawned", (i,b,g)=>{ plantIdx=i; plantObj=g; ExplainInterference(); });
        }
        int plantIdx=-1;
        float plantSize=-1, plantGreed=-1;
        GameObject plantObj;
        void ExplainInterference()
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            inspector.HideSizeSlider(false);
            inspector.HideGreedSlider(false);
            inspector.SetProducerAvailability(false);

            help.Showing = false;
            Hide(0);
            ShuffleOnSlider(3, 40);
            WaitThenDo(1f, WaitingFor);
            void WaitingFor()
            {
                help.Message = "Look, there is a new slider available! This new trait is called 'interference', and measures how much a species competes with itself. Low interference results in a high population, and a high interference results in a low population. Try dragging both sliders down as low as possible."; 
                help.Showing=true;
                AttachSmellyListener(graph, "OnUnfocused", ()=>{ StopAllCoroutines(); Track(plantObj.transform); });
                AttachSmellyListener<int>(graph, "OnNodeTapped", i=>{ StopAllCoroutines(); ShuffleOnSlider(3, 40); });
            }

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
            StopAllCoroutines();

            graph.ForceUnfocus();
            inspector.SetConsumerAvailability(true);
            graph.SetIfNodeCanBeFocused(plantIdx, false);

            Hide();
            help.Showing = false;
            WaitThenDo(1f, ()=>{ help.Message = "This causes plants to have the maximum population possible! This is often what you will want for plants. Now try adding an animal."; help.Showing = true; targetAnchor = new Vector2(1,0); Point(new Vector2(-61, 60) * hud.BottomScale, -90); });

            AttachSmellyListener(inspector, "OnIncubated", ()=>{ Hide(); help.Showing=false; });
            AttachSmellyListener(graph, "OnEmptyTapped", ()=>{ Point(); help.Showing=true; });
            AttachSmellyListener<int, bool, GameObject>(inspector, "OnSpawned", (i,b,g)=>{ animalIdx=i; animalObj=g; ExplainAnimal(); });
        }

        GameObject animalObj;
        int animalIdx=-1;
        float animalSize=-1, animalGreed=-1;
        bool edge1added = false;
        void ExplainAnimal()
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            inspector.SetConsumerAvailability(false);
            help.Showing = false;
            ShuffleOnSlider(3, 40);
            WaitThenDo(1f, WaitingFor);
            void WaitingFor()
            {
                help.Message = "Then make it eat the plant, and drag both sliders as high as possible.";
                help.Showing = true;
                AttachSmellyListener(graph, "OnUnfocused", ()=>{ StopAllCoroutines(); Track(animalObj.transform); });
                AttachSmellyListener<int>(graph, "OnNodeTapped", i=>{ StopAllCoroutines(); ShuffleOnSlider(3, 40); });
            }

            void TrackAnimalTrait(bool sizeOrGreed, float val)
            {
                if (sizeOrGreed) {
                    animalSize = val;
                } else {
                    animalGreed = val;
                }
                if (animalSize==1 && animalGreed==1)
                {
                    if (graph.NumComponents!=1)
                    {
                        StopAllCoroutines();
                        Transform drag = graph.DragFromTarget? animalObj.transform : plantObj.transform;
                        Transform drop = graph.DragFromTarget? plantObj.transform : animalObj.transform;
                        DragAndDrop(drag, drop, 2f);
                    } else {
                        ExplainConflict1();
                    }
                }
                else
                {
                    // start coroutine if not already running
                }
            }
            void TrackConnected()
            {
                if (animalSize==1 && animalGreed==1 && graph.NumComponents==1) {
                    ExplainConflict1();
                }
            }
            AttachSmellyListener(graph, "OnLayedOut", TrackConnected);
            AttachSmellyListener<int, float>(inspector, "OnSizeSet", (i,x)=>TrackAnimalTrait(true, x));
            AttachSmellyListener<int, float>(inspector, "OnGreedSet", (i,x)=>TrackAnimalTrait(false, x));
        }
        int plantIdx2 = -1;
        GameObject plantObj2;
        bool edge2added = false;
        void ExplainConflict1()
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            edge1added = true;
            graph.ForceUnfocus();
            graph.SetIfLinkRemovable(plantIdx, animalIdx, false);
            graph.SetIfNodeCanBeFocused(animalIdx, false);
            graph.SetIfNodeCanBeTarget(animalIdx, false);
            inspector.SetProducerAvailability(true);

            Hide();
            help.Showing = false;
            // point at plant again
            WaitThenDo(1f, ()=>{ Point(new Vector2(-61,115) * hud.BottomScale, -45); targetAnchor = new Vector2(1,0); help.Message = "Great job! A plant with low interference can enable even the heaviest species survive. Now try adding one final plant."; help.Showing = true; });

            AttachSmellyListener(inspector, "OnIncubated", ()=>{ Hide(); help.Showing=false; });
            AttachSmellyListener(graph, "OnEmptyTapped", ()=>{ Point(); help.Showing=true; });
            AttachSmellyListener<int, bool, GameObject>(inspector, "OnSpawned", (i,b,g)=>{ plantIdx2=i; plantObj2=g; ExplainConflict2(); });
        }
        void ExplainConflict2()
        {
            DetachSmellyListeners();
            StopAllCoroutines();
            inspector.SetProducerAvailability(false);
            inspector.UnfixSizeInitialValue();
            inspector.UnfixGreedInitialValue();
            inspector.AllowConflicts(false);

            help.Showing = false;
            ShuffleOnSlider(3, 40);
            WaitThenDo(1f, WaitingFor);
            void WaitingFor()
            {
                help.Message = "There is one more rule in Research world that you must follow: no two species can be identical! Try making the new plant identical to the first by dragging both sliders as low as possible.";
                help.Showing = true;
                AttachSmellyListener(graph, "OnUnfocused", ()=>{ StopAllCoroutines(); Track(plantObj2.transform); });
                AttachSmellyListener<int>(graph, "OnNodeTapped", i=>{ StopAllCoroutines(); ShuffleOnSlider(3, 40); });
            }
            AttachSmellyListener<int, string>(inspector, "OnConflicted", (i,s)=>ExplainConflict3());
        }
        void ExplainConflict3()
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            Hide();
            graph.SetIfNodeCanBeTarget(animalIdx, true);
            help.Message = "Uh oh! If you try to make two identical species, the original will be outlined with a message telling you that it is not possible. Now try making the animal eat both plants.";
            help.SetAnchorHeight(.7f);
            help.Showing = true;

            AttachSmellyListener(graph, "OnLayedOut", ()=>{ if (graph.NumComponents==1) ExplainSuperFocus1(); });
        }
        void ExplainSuperFocus1()
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            edge2added = true;
            graph.ForceUnfocus();
            graph.SetIfLinkRemovable(plantIdx2, animalIdx, false);
            graph.SetIfNodeCanBeFocused(animalIdx, false);
            graph.SetIfNodeCanBeFocused(plantIdx, false);
            graph.AllowSuperfocus = true;

            help.Showing = false;
            WaitThenDo(1, ()=>{ help.Message = "Research World also unlocks a final tool which is useful in case your ecosystem becomes very complex: if you tap a species twice, the game will show only its direct connections. Try that now."; help.Showing = true; Track(plantObj2.transform); help.SetAnchorHeight(.85f); });

            void CheckSuperFocus()
            {
                if (graph.FocusedState == NodeLink.Graph.FocusState.SuperFocus) {
                    ExplainSuperFocus2();
                }
            }
            AttachSmellyListener<int>(graph, "OnNodeTapped", (i)=>CheckSuperFocus());
        }
        void ExplainSuperFocus2()
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            graph.SetIfNodeCanBeFocused(animalIdx, true);

            Hide();
            help.Showing = false;
            WaitThenDo(2, ()=>{ help.Message = "The second plant has been centered on the screen in to give you a zoomed in view of your ecosystem. From here you can navigate through your ecosystem one species at a time. Try tapping the animal."; help.SetAnchorHeight(.4f); help.Showing = true; Track(animalObj.transform); });

            void CheckSuperFocus(int tappedIdx)
            {
                if (graph.FocusedState == NodeLink.Graph.FocusState.SuperFocus && tappedIdx == animalIdx)
                {
                    ExplainSuperFocus3();
                }
            }
            AttachSmellyListener<int>(graph, "OnNodeTapped", CheckSuperFocus);
        }
        void ExplainSuperFocus3()
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            graph.SetIfNodeCanBeFocused(plantIdx, true);
            help.Showing = false;
            Hide();
            WaitThenDo(1f, ()=>{ help.Message = "Great job! You can exit this view completely by simply tapping empty space twice."; help.SetAnchorHeight(.85f); help.Showing = true; });

            AttachSmellyListener(graph, "OnUnfocused", ExplainAlternateScore);
        }
        void ExplainAlternateScore()
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            score.DisableStarCalculation(false);
            score.Hide(false);
            inspector.HideRemoveButton(false);
            inspector.SetProducerAvailability(true);
            inspector.SetConsumerAvailability(true);
            inspector.HideSizeSlider(false);
            inspector.HideGreedSlider(false);
            inspector.AllowConflicts(false);
            inspector.UnfixSizeInitialValue();
            inspector.UnfixGreedInitialValue();
            graph.AllowSuperfocus = true;

            if (plantIdx >= 0) {
                graph.SetIfNodeCanBeFocused(plantIdx, true);
            }
            if (animalIdx >= 0) {
                graph.SetIfNodeCanBeFocused(animalIdx, true);
                graph.SetIfNodeCanBeTarget(animalIdx, true);
            }
            if (plantIdx2 >= 0) {
                graph.SetIfNodeCanBeFocused(plantIdx2, true);
            }
            if (edge1added) {
                graph.SetIfLinkRemovable(plantIdx, animalIdx, true);
            }
            if (edge2added) {
                graph.SetIfLinkRemovable(plantIdx2, animalIdx, true);
            }

            Hide();
            help.Showing = false;
            if (!skipped) {
                WaitThenDo(2, ShowAll);
            } else {
                help.ResetLevelPosition(true);
                help.Message = "The higher your score, the more you will help researchers to understand real ecosystems. Good luck!";
                help.Showing = false;
            }
            StartCoroutine(DestroySkipButton());

            void ShowAll()
            {
                help.ResetLevelPosition();
                help.Message = "The final feature of Research World is a new scoring system! You will be ranked against players from around the world on how well you score, with your current rank shown in the top left. Good luck!";
                help.Showing = true;
            }
            IEnumerator DestroySkipButton(float duration=.5f)
            {
                skipButton.interactable = false;
                float tStart = Time.time;
                while (Time.time < tStart+duration)
                {
                    float t = Tweens.QuadraticInOut((Time.time-tStart)/duration);
                    float size = Mathf.Lerp(1,0,t);
                    skipButton.transform.localScale = new Vector3(size,size,1);
                    yield return null;
                }
                Destroy(skipButton.gameObject);
            }
        }
    }
}