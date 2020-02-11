using System;
using System.Collections;
using UnityEngine;

namespace EcoBuilder.Tutorials
{
    public class Structure : Tutorial
    {
        protected override void StartLesson()
        {
            inspector.HideRemoveButton();
            score.Hide(true);
            constraints.Show(false);
            recorder.gameObject.SetActive(false);
            score.DisableStarCalculation(true);

            ExplainIntro();
        }

        void ExplainIntro()
        {
            DetachSmellyListeners();

            targetSize = new Vector2(100,100);
            targetPos = new Vector2(-61,115);
            targetAnchor = new Vector2(1,0);
            targetZRot = 315;

            incubator.SetConsumerAvailability(false);

            AttachSmellyListener<bool>(incubator, "OnIncubated", b=>ExplainInspector());
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
            help.SetAnchorHeight(.5f);

            AttachSmellyListener<int, GameObject>(inspector, "OnSpawned", ExplainSpawn);
            AttachSmellyListener(nodelink, "OnEmptyTapped", ExplainIntro);
        }
        GameObject firstSpecies;
        int firstIdx;
        void ExplainSpawn(int idx, GameObject first)
        {
            DetachSmellyListeners();
            targetAnchor = new Vector2(1,0);
            targetPos = new Vector2(-61, 60);
            targetZRot = 270;

            firstSpecies = first;
            firstIdx = idx;
            incubator.SetConsumerAvailability(true);
            incubator.SetProducerAvailability(false);

            help.Message = "Your " + firstSpecies.name + " is born! Plants grow on their own, and so do not need to eat any other species. Now try adding an animal by tapping the paw button.";
            help.SetAnchorHeight(.95f, false);
            help.Showing = true;

            AttachSmellyListener<int, GameObject>(inspector, "OnSpawned", ExplainInteraction);
            AttachSmellyListener<bool>(incubator, "OnIncubated", (b)=>{ help.Showing = false; targetSize = Vector2.zero; });
            AttachSmellyListener(incubator, "OnUnincubated", ()=>{ help.Showing = true; targetSize = new Vector2(100,100); });
        }

        GameObject secondSpecies;
        int secondIdx;
        void ExplainInteraction(int idx, GameObject second)
        {
            DetachSmellyListeners();
            secondSpecies = second;
            secondIdx = idx;

            smoothTime = .7f;
            if (GameManager.Instance.ReverseDragDirection)
            {
                help.Message = "Your " + secondSpecies.name + " is hungry! Drag from it to the " + firstSpecies.name + " to give it some food.";
                StartCoroutine(Shuffle(firstSpecies.transform, secondSpecies.transform, 2f));
            }
            else
            {
                help.Message = "Your " + secondSpecies.name + " is hungry! Drag from the " + firstSpecies.name + " to it to give it some food.";
                StartCoroutine(Shuffle(secondSpecies.transform, firstSpecies.transform, 2f));
            }
            help.Showing = true;

            targetSize = new Vector3(100,100);
            targetAnchor = new Vector3(0f,0f);
            targetZRot = 360;
            incubator.HideStartButtons();

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
            nodelink.ForceUnfocus();
            inspector.HideRemoveButton(false);
            targetAnchor = new Vector2(0,0);

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
            help.SetAnchorHeight(.2f);
            help.Message = "And tap this skull button to remove the species.";

            AttachSmellyListener(nodelink, "OnUnfocused", ()=>ExplainRemove1(0));
            AttachSmellyListener<int>(inspector, "OnUserDespawned", i=>ExplainUndo(1.5f));
        }
        void ExplainUndo(float wait)
        {
            DetachSmellyListeners();
            targetAnchor = new Vector2(0, 0);
            targetZRot = 405;

            help.Showing = false;
            help.SetPivotHeight(1);
            help.SetAnchorHeight(.8f);
            targetSize = new Vector2(100,100);
            targetPos = new Vector2(40, 120);
            recorder.gameObject.SetActive(true); 

            StartCoroutine(WaitThenDo(wait, ()=> { help.Showing = true; help.Message = "And don't worry if you make mistakes! You can always undo any move you make, using these controls in the bottom left. Try that now."; }));

            AttachSmellyListener<int>(recorder, "OnSpeciesUndone", (i)=>ExplainFinishCondition(1.5f));
        }
        void ExplainFinishCondition(float delay)
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            targetSize = new Vector2(0,0);
            targetZRot = 315;

            score.DisableStarCalculation(false); 
            help.SetAnchorHeight(.95f);
            help.Showing = false;
            // help.SetSide(false, false);

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

            AttachSmellyListener(GameManager.Instance, "OnPlayedLevelFinished", ()=>ExplainNavigation(2f));
        }
        void ExplainNavigation(float delay)
        {
            DetachSmellyListeners();
            targetAnchor = new Vector2(.5f,0);
            targetZRot = 405;
            targetPos = new Vector2(140, 70);
            smoothTime = .5f;

            help.Showing = false;
            StartCoroutine(WaitThenDo(delay, ()=> { help.SetSide(false, false); help.Showing = true; help.SetPivotHeight(0); help.SetAnchorHeight(.4f); help.Message = "Great! You can access the next level by tapping it here."; }));

            // AttachSmellyListener(finishedLevel.NextLevelInstantiated, "OnCarded", ExplainNextLevel);
        }
        // Canvas onTop;
        // void ExplainNextLevel()
        // {
        //     // targetSize = new Vector2(140, 140);
        //     targetPos = new Vector2(50, 300);

        //     if (onTop == null)
        //     {
        //         onTop = gameObject.AddComponent<Canvas>();
        //         onTop.overrideSorting = true;
        //         onTop.sortingOrder = 3;
        //         // no need to remove this if undone because on top is always okay
        //     }

        //     DetachSmellyListeners();
        //     // AttachSmellyListener(finishedLevel.NextLevelInstantiated, "OnThumbnailed", ()=>ExplainNavigation(finishedLevel, 0));
        // }

        // bool waiting = false;
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
            transform.position = ScreenPos(Camera.main.WorldToViewportPoint(grab.position));

            float prevSmoothTime = smoothTime;
            while (true)
            {
                if (((Time.time - start) % time) < (time/2f))
                {
                    targetPos = ScreenPos(Camera.main.WorldToViewportPoint(grab.position)) + new Vector2(0,-20);
                    Grab();
                }
                else
                {
                    targetPos = ScreenPos(Camera.main.WorldToViewportPoint(drop.position)) + new Vector2(0,-20);
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
                targetPos = ScreenPos(Camera.main.WorldToViewportPoint(tracked.position)) + new Vector2(0,-20);
                yield return null;
            }
        }
    }
}