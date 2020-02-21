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
            score.Hide();
            constraints.Show(false);
            recorder.gameObject.SetActive(false);
            score.DisableStarCalculation(true);

            inspector.SetConsumerAvailability(false);

            ExplainIntro(false);
        }

        void ExplainIntro(bool reshowText)
        {
            DetachSmellyListeners();

            targetSize = new Vector2(100,100);
            targetPos = new Vector2(-61,115);
            targetAnchor = new Vector2(1,0);
            targetZRot = 315;

            // in case the user goes back
            if (reshowText)
            {
                help.Message = GameManager.Instance.PlayedLevelDetails.Introduction;
                help.Showing = true;
                help.ResetPosition();
            }

            AttachSmellyListener(inspector, "OnIncubated", ExplainInspector);
        }
        void ExplainInspector()
        {
            DetachSmellyListeners();

            targetPos = new Vector2(160, 50);
            targetAnchor = new Vector2(.5f, 0);
            targetZRot = 450;

            help.Message = "Here you can choose a new name for your species. You can then introduce it by dragging it into the world.";
            help.Showing = true;
            help.SetSide(true);
            help.SetAnchorHeight(.35f);

            AttachSmellyListener<int, bool, GameObject>(inspector, "OnSpawned", ExplainSpawn);
            AttachSmellyListener(nodelink, "OnEmptyTapped", ()=>ExplainIntro(true));
        }
        GameObject firstSpecies;
        int firstIdx;
        void ExplainSpawn(int idx, bool isProducer, GameObject first)
        {
            Assert.IsTrue(isProducer, "first species should be producer");

            DetachSmellyListeners();
            targetAnchor = new Vector2(1,0);
            targetPos = new Vector2(-61, 60);
            targetZRot = 270;

            firstSpecies = first;
            firstIdx = idx;
            inspector.SetConsumerAvailability(true);
            inspector.SetProducerAvailability(false);

            help.Message = "Your " + firstSpecies.name + " is born! Plants grow on their own, and so do not need to eat any other species to survive. Now try adding an animal by tapping the paw button.";
            help.SetAnchorHeight(.85f, true);
            help.Showing = true;

            AttachSmellyListener<int, bool, GameObject>(inspector, "OnSpawned", ExplainInteraction);
            AttachSmellyListener(inspector, "OnIncubated", ()=>{ help.Showing = false; targetSize = Vector2.zero; });
            AttachSmellyListener(inspector, "OnUnincubated", ()=>{ help.Showing = true; targetSize = new Vector2(100,100); });
        }

        GameObject secondSpecies;
        int secondIdx;
        void ExplainInteraction(int idx, bool isProducer, GameObject second)
        {
            Assert.IsFalse(isProducer, "second species should be consumer");

            DetachSmellyListeners();
            secondSpecies = second;
            secondIdx = idx;

            smoothTime = .7f;
            if (GameManager.Instance.ReverseDragDirection)
            {
                help.Message = "Your " + secondSpecies.name + " is hungry! It is flashing because it is going extinct, as it has no food source. Drag from it to the " + firstSpecies.name + " to make them interact.";
                StartCoroutine(Shuffle(firstSpecies.transform, secondSpecies.transform, 2f));
            }
            else
            {
                help.Message = "Your " + secondSpecies.name + " is hungry! It is flashing because it is going extinct, as it has no food source. Drag from the " + firstSpecies.name + " to it to make them interact.";
                StartCoroutine(Shuffle(secondSpecies.transform, firstSpecies.transform, 2f));
            }
            help.Showing = true;

            targetSize = new Vector3(100,100);
            targetAnchor = new Vector3(0f,0f);
            targetZRot = 360;
            inspector.HideIncubatorButtons();

            AttachSmellyListener<int,int>(nodelink, "OnUserLinked", (i,j)=>ExplainFirstEcosystem(2));
        }
        void ExplainFirstEcosystem(float delay)
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            targetSize = new Vector2(0,0);
            help.Showing = false;
            nodelink.ForceUnfocus();

            smoothTime = .3f;
            if (GameManager.Instance.ReverseDragDirection)
            {
                StartCoroutine(WaitThenDo(delay, ()=> { help.Showing = true; help.Message = "You have created your very own ecosystem. Well done! Now try removing the link you just made, by performing the same dragging action from the animal to the plant."; }));
            }
            else
            {
                StartCoroutine(WaitThenDo(delay, ()=> { help.Showing = true; help.Message = "You have created your very own ecosystem. Well done! Now try removing the link you just made, by performing the same dragging action from the plant to the animal."; }));
            }

            AttachSmellyListener<int,int>(nodelink, "OnUserUnlinked", (i,j)=>ExplainRemove1(1));
        }
        void ExplainRemove1(float delay)
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            help.Showing = false;
            inspector.HideRemoveButton(false);
            targetAnchor = new Vector2(0,0);

            nodelink.ForceUnfocus();
            nodelink.SetIfNodeInteractable(firstIdx, false);

            StartCoroutine(WaitThenDo(delay, ()=> { help.Showing = true; help.SetAnchorHeight(.7f); help.Message = "You can also remove species entirely if you wish. Try tapping on one of your species to focus on it."; targetSize = new Vector3(100,100); targetZRot = 360; }));
            StartCoroutine(Track(secondSpecies.transform));

            AttachSmellyListener<int>(nodelink, "OnFocused", i=>ExplainRemove2());
        }
        void ExplainRemove2()
        {
            DetachSmellyListeners();
            StopAllCoroutines(); // stop tracking

            targetAnchor = new Vector2(.5f, 0);
            targetPos = new Vector2(160, 50);
            targetZRot = 450;

            help.Showing = true;
            help.SetPivotHeight(0);
            help.SetAnchorHeight(.1f);
            help.Message = "And tap this skull button to remove the species.";

            AttachSmellyListener<int>(nodelink, "OnUnfocused", i=>ExplainRemove1(0));
            AttachSmellyListener<int>(inspector, "OnUserDespawned", i=>ExplainUndo(1.5f));
        }
        void ExplainUndo(float delay)
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            targetAnchor = new Vector2(0, 0);
            targetZRot = 405;
            targetSize = new Vector2(100,100);
            targetPos = new Vector2(40, 120);

            help.Showing = false;
            help.SetPivotHeight(1);
            help.SetAnchorHeight(.8f);
            recorder.gameObject.SetActive(true); 

            StartCoroutine(WaitThenDo(delay, ()=> { help.Showing = true; help.Message = "And don't worry if you make mistakes! You can always undo any move you make, using these controls in the bottom left. Try that now."; }));

            AttachSmellyListener(recorder, "OnUndoOrRedo", ()=>ExplainFinishCondition(1.5f));
        }
        void ExplainFinishCondition(float delay)
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            targetSize = new Vector2(0,0);
            targetZRot = 315;

            score.DisableStarCalculation(false); 
            help.SetAnchorHeight(.9f);
            help.Showing = false;
            nodelink.SetIfNodeInteractable(firstIdx, true);

            StartCoroutine(WaitThenDo(delay, ()=> { help.Showing = true; help.Message = "You may finish the game by tapping the button in the top right, but only once all of your species can coexist. Reconstruct your ecosystem to complete this tutorial!"; }));

            AttachSmellyListener(score, "OnLevelCompletabled", ExplainFinish);
        }
        void ExplainFinish()
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            targetSize = new Vector2(100,100);

            // do not smooth for this
            targetAnchor = pointerRT.anchorMin = pointerRT.anchorMax = new Vector2(1,1);
            targetPos = pointerRT.anchoredPosition = new Vector2(-90,-90);

            // this should be taken care of by EventMediator
            // help.Message = "Well done! Tap this button to finish the level.";
            // help.Showing = true;
            help.SetPixelWidth(350);

            AttachSmellyListener(GameManager.Instance, "OnPlayedLevelFinished", ()=>ExplainNavigation(2.5f));
        }
        void ExplainNavigation(float delay)
        {
            DetachSmellyListeners();
            targetAnchor = new Vector2(.5f,0);
            targetZRot = 405;
            targetPos = new Vector2(140, 70);
            smoothTime = .5f;

            help.Showing = false;
            help.SetPixelWidth(400);
            StartCoroutine(WaitThenDo(delay, ()=> { help.Showing = true; help.SetPivotHeight(0); help.SetAnchorHeight(.2f); help.Message = "Great! You can access the next level by tapping it here."; }));

        }

        IEnumerator WaitThenDo(float seconds, Action Todo)
        {
            if (seconds > 0) {
                yield return new WaitForSeconds(seconds);
            }
            Todo();
        }
        IEnumerator Shuffle(Transform grab, Transform drop, float time)
        {
            float start = Time.time;
            transform.position = ScreenPos(mainCam.WorldToViewportPoint(grab.position));

            float prevSmoothTime = smoothTime;
            while (true)
            {
                if (((Time.time - start) % time) < (time/2f))
                {
                    targetPos = ScreenPos(mainCam.WorldToViewportPoint(grab.position)) + new Vector2(0,-20);
                    Grab();
                }
                else
                {
                    targetPos = ScreenPos(mainCam.WorldToViewportPoint(drop.position)) + new Vector2(0,-20);
                    Pan();
                }
                yield return null;
            }
        }
        IEnumerator Track(Transform tracked)
        {
            Point();
            while (true)
            {
                targetPos = ScreenPos(mainCam.WorldToViewportPoint(tracked.position)) + new Vector2(0,-20);
                yield return null;
            }
        }
    }
}