using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace EcoBuilder.Tutorials
{
    public class TutorialStructure : Tutorial
    {
        protected override void StartLesson()
        {
            inspector.HideRemoveButton();
            constraints.Hide();
            recorder.Hide();
            score.Hide();
            score.DisableStarCalculation(true);
            inspector.HideStatusBars();

            inspector.SetConsumerAvailability(false);

            ExplainIntro(false);
        }

        void ExplainIntro(bool reshowText)
        {
            DetachSmellyListeners();

            targetAnchor = new Vector2(1,0);
            Point(new Vector2(-61,115) * hud.BottomScale, -45);

            // in case the user goes back
            if (reshowText)
            {
                help.Message = GameManager.Instance.PlayedLevelDetails.Introduction;
                help.Showing = true;
                help.ResetLevelPosition();
            }

            AttachSmellyListener(graph, "OnEmptyTapped", ()=>help.Showing=true);
            AttachSmellyListener(inspector, "OnIncubated", ExplainInspector);
        }
        void ExplainInspector()
        {
            DetachSmellyListeners();

            Point(new Vector2(160, 50) * hud.BottomScale, 90);
            targetAnchor = new Vector2(.5f, 0);

            help.Message = "Here you can choose a new name for your species. You can then introduce it by dragging it into the world.";
            help.Showing = true;
            help.SetSide(true);
            help.SetAnchorHeight(.35f);

            AttachSmellyListener<int, bool, GameObject>(inspector, "OnSpawned", ExplainSpawn);
            AttachSmellyListener(graph, "OnEmptyTapped", ()=>ExplainIntro(true));
        }
        GameObject firstSpecies;
        int firstIdx;
        void ExplainSpawn(int idx, bool isProducer, GameObject first)
        {
            DetachSmellyListeners();
            Assert.IsTrue(isProducer, "first species should be producer");

            targetAnchor = new Vector2(1,0);
            Point(new Vector2(-61, 60) * hud.BottomScale, -90);

            firstSpecies = first;
            firstIdx = idx;
            inspector.SetConsumerAvailability(true);
            inspector.SetProducerAvailability(false);

            help.Message = "Your " + firstSpecies.name + " is born! Plants grow on their own, and so do not need to eat any other species to survive. Now try adding an animal by tapping the paw button.";
            help.SetAnchorHeight(.85f, true);
            help.Showing = true;

            AttachSmellyListener<int, bool, GameObject>(inspector, "OnSpawned", ExplainInteraction);
            AttachSmellyListener(inspector, "OnIncubated", ()=>{ help.Showing = false; Hide(); });
            AttachSmellyListener(inspector, "OnUnincubated", ()=>{ help.Showing = true; Point(); });
        }

        GameObject secondSpecies;
        int secondIdx;
        void ExplainInteraction(int idx, bool isProducer, GameObject second)
        {
            DetachSmellyListeners();
            Assert.IsFalse(isProducer, "second species should be consumer");

            inspector.HideInitiatorButtons();

            secondSpecies = second;
            secondIdx = idx;

            if (GameManager.Instance.ReverseDragDirection) {
                help.Message = "Your " + secondSpecies.name + " is hungry! It is flashing because it is going extinct, as it has no food source. Drag from it to the " + firstSpecies.name + " to make them interact.";
            } else {
                help.Message = "Your " + secondSpecies.name + " is hungry! It is flashing because it is going extinct, as it has no food source. Drag from the " + firstSpecies.name + " to it to make them interact.";
            }
            DragAndDrop(firstSpecies.transform, secondSpecies.transform, 2f);
            help.Showing = true;

            AttachSmellyListener<int,int>(graph, "OnUserLinked", (i,j)=>ExplainFirstEcosystem(2));
        }
        void ExplainFirstEcosystem(float delay)
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            Hide(0);
            help.Showing = false;
            graph.ForceUnfocus();

            if (GameManager.Instance.ReverseDragDirection) {
                WaitThenDo(delay, ()=> { help.Showing = true; help.Message = "You have created your very own ecosystem. Well done! Now try removing the link you just made, by performing the same dragging action from the animal to the plant."; });
            } else {
                WaitThenDo(delay, ()=> { help.Showing = true; help.Message = "You have created your very own ecosystem. Well done! Now try removing the link you just made, by performing the same dragging action from the plant to the animal."; });
            }

            AttachSmellyListener<int,int>(graph, "OnUserUnlinked", (i,j)=>ExplainRemove1(1));
        }
        void ExplainRemove1(float delay)
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            help.Showing = false;
            inspector.HideRemoveButton(false);
            targetAnchor = new Vector2(0,0);

            graph.ForceUnfocus();
            graph.SetIfNodeCanBeFocused(firstIdx, false);

            WaitThenDo(delay, ()=> { help.Showing = true; help.SetAnchorHeight(.7f); help.Message = "You can also remove species entirely if you wish. Try tapping on one of your species to focus on it."; Track(secondSpecies.transform); }); 


            AttachSmellyListener<int>(graph, "OnNodeTapped", i=>ExplainRemove2());
        }
        void ExplainRemove2()
        {
            DetachSmellyListeners();
            StopAllCoroutines(); // stop tracking

            targetAnchor = new Vector2(.5f, 0);
            Point(new Vector2(160, 50) * hud.BottomScale, 90);

            help.Showing = true;
            help.SetPivotHeight(0);
            help.SetAnchorHeight(.1f);
            help.Message = "And tap this skull button to remove the species.";

            AttachSmellyListener(graph, "OnUnfocused", ()=>ExplainRemove1(0));
            AttachSmellyListener<int>(inspector, "OnUserDespawned", i=>ExplainUndo(1.5f));
        }
        void ExplainUndo(float delay)
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            targetAnchor = new Vector2(0, 0);
            Point(new Vector2(60, 120) * hud.BottomScale, 45, .3f);

            help.Showing = false;
            help.SetPivotHeight(1);
            help.SetAnchorHeight(.8f);
            recorder.Hide(false); 

            WaitThenDo(delay, ()=> { help.Showing = true; help.Message = "And don't worry if you make mistakes! You can always undo any move you make, using these controls in the bottom left. Try that now."; });

            AttachSmellyListener(recorder, "OnUndoOrRedo", ()=>ExplainFinishCondition(1.5f));
        }
        void ExplainFinishCondition(float delay)
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            Hide();

            score.DisableStarCalculation(false); 
            help.SetAnchorHeight(.9f);
            help.Showing = false;
            graph.SetIfNodeCanBeFocused(firstIdx, true);

            WaitThenDo(delay, ()=> { help.Showing = true; help.Message = "You may finish the game by tapping the button in the top right, but only once all of your species can coexist. Reconstruct your ecosystem to complete this tutorial!"; });

            AttachSmellyListener(score, "OnOneStarAchieved", ExplainFinish);
        }
        void ExplainFinish()
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            // do not smooth for this
            Point(new Vector2(-90,-90), -45);
            targetAnchor = pointerRT.anchorMin = pointerRT.anchorMax = new Vector2(1,1);
            pointerRT.anchoredPosition = new Vector2(-90,-90);

            // this should be taken care of by PlayManager
            // help.Message = "Well done! Tap this button to finish the level.";
            // help.Showing = true;
            help.SetPixelWidth(350);
            help.SetAnchorHeight(.8f);

            AttachSmellyListener(GameManager.Instance, "OnPlayedLevelFinished", ()=>ExplainNavigation(2.5f));
        }
        void ExplainNavigation(float delay)
        {
            DetachSmellyListeners();
            Hide();
        }
    }
}