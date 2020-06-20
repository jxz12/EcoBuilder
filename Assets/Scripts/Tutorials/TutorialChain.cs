using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace EcoBuilder.Tutorials
{
    public class TutorialChain : Tutorial
    {
        protected override void StartLesson()
        {
            Hide();
            recorder.Hide();
            inspector.HideSizeSlider();
            inspector.HideRemoveButton();
            inspector.FixSizeInitialValue(-1);
            score.Hide();
            score.DisableStarCalculation();

            constraints.LimitLeaf(1);
            constraints.LimitPaw(1);

            ExplainIntro();
        }
        int producerIdx=-1, herbivoreIdx=-1;
        Transform producerTransform, herbivoreTransform;
        void ListenForInitIdxs(int idx, bool isProducer, GameObject gameObject)
        {
            if (isProducer) {
                producerIdx = idx;
                producerTransform = gameObject.transform;
            } else {
                herbivoreIdx = idx;
                herbivoreTransform = gameObject.transform;
            }
        }
        void ExplainIntro()
        {
            DetachSmellyListeners();
            AttachSmellyListener<int, bool, GameObject>(inspector, "OnSpawned", ListenForInitIdxs);
            AttachSmellyListener(graph, "OnLayedOut", ()=>CheckChainOfHeight(1, ExplainChainOfOne));
        }
        void CheckChainOfHeight(int heightGoal, Action Todo)
        {
            if (graph.MaxChain == heightGoal) {
                Todo.Invoke();
            }
        }
        void ExplainChainOfOne()
        {
            DetachSmellyListeners();
            Assert.IsTrue(producerIdx!=-1 && herbivoreIdx!=-1);

            graph.ForceUnfocus();
            help.Showing = false;
            WaitThenDo(1, Show);
            void Show()
            {
                help.Message = "This number on the left has changed! This is because the animal is one link away from a plant, which means it has a chain length of one. By pressing this icon, you can highlight all species with that chain length.";
                help.SetSide(true);
                help.SetAnchorHeight(.6f);
                help.Showing = true;

                // point at constraints panel
                targetAnchor = new Vector2(0,1);
                Point(new Vector2(70, -300 + hud.ConstraintsOffset), 30); 
            }

            // allow animal again, but only one more
            constraints.LimitPaw(2);

            // stop user from removing the link
            graph.SetIfLinkRemovable(producerIdx, herbivoreIdx, false);
            graph.SetIfNodeCanBeTarget(herbivoreIdx, false);
            graph.SetIfNodeCanBeSource(producerIdx, false);

            float delay = 1.5f;
            void PointAtPaw()
            {
                if (delay > 0) {
                    Hide();
                    help.Showing = false;
                }
                WaitThenDo(delay, DoPoint);
                delay = 0; // lol at this
                void DoPoint() // and this
                {
                    targetAnchor = new Vector2(1,0);
                    Point(new Vector2(-61, 60) * hud.BottomScale, -90);
                    help.SetAnchorHeight(.25f);
                    help.Message = "Now try adding another animal.";
                    help.Showing = true;
                }
            }
            AttachSmellyListener(constraints, "OnChainHovered", PointAtPaw);
            AttachSmellyListener(inspector, "OnIncubated", ()=>{ help.Showing = false; Hide(); });
            AttachSmellyListener(graph, "OnEmptyTapped", PointAtPaw);
            AttachSmellyListener<int, bool, GameObject>(inspector, "OnSpawned", (i,b,g)=>carnivoreTransform=g.transform);
            AttachSmellyListener(graph, "OnLayedOut", ()=>RequestChainOfTwo());
        }
        void RequestChainOfTwo()
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            help.SetAnchorHeight(.7f);
            help.Message = "And make it eat the previous animal.";
            help.Showing = true;
            DragAndDrop(herbivoreTransform, carnivoreTransform, 2.5f);
            
            AttachSmellyListener(graph, "OnLayedOut", ()=>CheckChainOfHeight(2, ()=>ExplainChainOfTwo(1.5f)));
        }
        int carnivoreIdx=2; // nodes are not removable so this must be true
        Transform carnivoreTransform;
        void ExplainChainOfTwo(float delay)
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            Hide();
            graph.ForceUnfocus();
            help.Showing = false;
            help.SetAnchorHeight(.95f, true);
            WaitThenDo(delay, Todo);
            void Todo()
            {
                help.SetPixelWidth(430);
                help.Message = "Great job. It takes two links to get from the plant to the second animal, and so the maximum chain length of your ecosystem is now two. However it is going extinct! This is because biomass is lost at each level of an ecosystem, and so the top species does not have enough food to sustain it. Try saving it by changing the plant.";
                help.Showing = true;
                inspector.HideSizeSlider(false);
                Track(producerTransform);
            }

            graph.SetIfLinkRemovable(herbivoreIdx, carnivoreIdx, false);
            graph.SetIfNodeCanBeTarget(carnivoreIdx, false);
            graph.SetIfNodeCanBeFocused(herbivoreIdx, false);
            graph.SetIfNodeCanBeFocused(carnivoreIdx, false);

            inspector.HideSizeSlider(false);

            AttachSmellyListener(graph, "OnUnfocused", ()=>{ help.Showing = true; StopAllCoroutines(); Track(producerTransform); });
            AttachSmellyListener<int>(graph, "OnNodeTapped", i=>{ help.Showing = false; StopAllCoroutines(); ShuffleOnSlider(3, 40); });
            AttachSmellyListener(model, "OnEquilibrium", ()=>{ if (model.Feasible) ExplainTopSurvival(1.5f); });
        }
        void ExplainTopSurvival(float delay)
        {
            DetachSmellyListeners();
            StopAllCoroutines();
            Hide();

            graph.ForceUnfocus();
            help.Showing = false;
            WaitThenDo(delay, ()=>{ help.Message = "Great! In general, it is more difficult to get a species to survive, the longer its chain length. Let's try one more thing: make the species with a chain length of two also eat the plant."; help.SetPixelWidth(400); help.Showing = true; help.SetAnchorHeight(.85f); DragAndDrop(producerTransform, carnivoreTransform, 2.5f, .5f); });

            graph.SetIfNodeCanBeFocused(producerIdx, false);
            graph.SetIfNodeCanBeSource(producerIdx, true);
            graph.SetIfNodeCanBeTarget(carnivoreIdx, true);

            AttachSmellyListener(graph, "OnLayedOut", ()=>CheckChainOfHeight(1, ()=>ExplainBackToOne(1)));
        }
        void ExplainBackToOne(float delay)
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            Hide();
            graph.ForceUnfocus();
            graph.SetIfNodeCanBeFocused(producerIdx, true);
            graph.SetIfNodeCanBeFocused(herbivoreIdx, true);
            graph.SetIfNodeCanBeFocused(carnivoreIdx, true);
            graph.SetIfNodeCanBeTarget(herbivoreIdx, true);

            help.Showing = false;
            WaitThenDo(delay, Finish);
            void Finish()
            {
                recorder.Hide(false);
                score.Hide(false);
                score.DisableStarCalculation(false);
                help.Message = "The max chain length of your ecosystem has fallen back to one! This is because the height of any given species only considers its shortest path to a plant, and so the path going through both animals is overridden by linking to the plant. To finish this level, reconstruct a chain length of 2.";
                help.SetSide(false, true);
                help.SetAnchorHeight(.85f, true);
                help.SetPixelWidth(380);
                help.Showing = true;
                score.DisableStarCalculation(false); 
                score.Hide(false);
            }

        }
    }
}