using System;
using System.Collections;
using UnityEngine;

namespace EcoBuilder.Levels
{
    // this tutorial teaches about how to build a structure
    public class Tutorial1 : Tutorial
    {
        protected override void StartLesson()
        {
            inspector.HideRemoveButton();
            score.HideScore(true);
            score.HideConstraints(true);
            score.DisableFinish(true);
            recorder.gameObject.SetActive(false);

            ExplainIntro();
        }

        void ExplainIntro()
        {
            targetSize = new Vector2(100,100);
            targetPos = new Vector2(-61,115);
            targetAnchor = new Vector2(1,0);
            targetZRot = 315;

            help.SetSide(false);
            help.SetDistFromTop(.2f);
            help.SetWidth(.6f);
            incubator.SetConsumerAvailability(false);

            DetachSmellyListeners();
            AttachSmellyListener<bool>(incubator, "OnIncubated", b=>ExplainInspector());
        }
        void ExplainInspector()
        {
            targetPos = new Vector2(160, 50);
            targetAnchor = new Vector2(.5f, 0);
            targetZRot = 450;

            help.SetText("Here you can choose a new name for your species. You can then introduce it by dragging it into the world.");
            help.Show(true);
            help.SetSide(true);
            help.SetDistFromTop(.1f);

            DetachSmellyListeners();
            AttachSmellyListener<int, GameObject>(inspector, "OnSpawned", ExplainSpawn);
            AttachSmellyListener(nodelink, "OnEmptyTapped", ExplainIntro);
        }
        GameObject firstSpecies;
        int firstIdx;
        void ExplainSpawn(int idx, GameObject first)
        {
            targetAnchor = new Vector2(1,0);
            targetPos = new Vector2(-61, 60);
            targetZRot = 270;

            firstSpecies = first;
            firstIdx = idx;
            incubator.SetConsumerAvailability(true);
            incubator.SetProducerAvailability(false);

            help.SetText("Your " + firstSpecies.name + " is born! Plants grow on their own, and so do not need to eat any other species. Now try adding an animal by tapping the paw button.");
            help.SetDistFromTop(.02f, false);
            help.Show(true);

            DetachSmellyListeners();
            AttachSmellyListener<int, GameObject>(inspector, "OnSpawned", ExplainInteraction);
            AttachSmellyListener<bool>(incubator, "OnIncubated", (b)=>{ help.Show(false); targetSize = Vector2.zero; });
            AttachSmellyListener(incubator, "OnUnincubated", ()=>{ help.Show(true); targetSize = new Vector2(100,100); });
        }

        GameObject secondSpecies;
        int secondIdx;
        void ExplainInteraction(int idx, GameObject second)
        {
            secondSpecies = second;
            secondIdx = idx;

            smoothTime = .7f;
            if (GameManager.Instance.ReverseDragDirection)
            {
                help.SetText("Your " + secondSpecies.name + " is hungry! Drag from it to the " + firstSpecies.name + " to give it some food.");
                StartCoroutine(Shuffle(firstSpecies.transform, secondSpecies.transform, 2f));
            }
            else
            {
                help.SetText("Your " + secondSpecies.name + " is hungry! Drag from the " + firstSpecies.name + " to it to give it some food.");
                StartCoroutine(Shuffle(secondSpecies.transform, firstSpecies.transform, 2f));
            }
            help.Show(true);

            targetSize = new Vector3(100,100);
            targetAnchor = new Vector3(0f,0f);
            targetZRot = 360;
            incubator.HideStartButtons();

            DetachSmellyListeners();
            AttachSmellyListener<int,int>(nodelink, "OnUserLinked", (i,j)=>ExplainFirstEcosystem(2));
        }
        void ExplainFirstEcosystem(float delay)
        {
            StopAllCoroutines();
            targetSize = new Vector2(0,0);
            help.Show(false);
            nodelink.ForceUnfocus();

            smoothTime = .3f;
            if (GameManager.Instance.ReverseDragDirection)
            {
                StartCoroutine(WaitThenDo(delay, ()=> { help.Show(true); help.SetText("You have created your very own ecosystem. Well done! Now try removing the link you just made, by performing the same dragging action from the animal to the plant."); }));
            }
            else
            {
                StartCoroutine(WaitThenDo(delay, ()=> { help.Show(true); help.SetText("You have created your very own ecosystem. Well done! Now try removing the link you just made, by performing the same dragging action from the plant to the animal."); }));
            }

            DetachSmellyListeners();
            AttachSmellyListener<int,int>(nodelink, "OnUserUnlinked", (i,j)=>ExplainRemove1(1));
        }
        void ExplainRemove1(float delay)
        {
            StopAllCoroutines();
            help.Show(false);
            nodelink.ForceUnfocus();
            inspector.HideRemoveButton(false);
            targetAnchor = new Vector2(0,0);

            StartCoroutine(WaitThenDo(delay, ()=> { help.Show(true); help.SetDistFromTop(.3f); help.SetText("You can also remove species entirely if you wish. Try tapping on one of your species to focus on it."); targetSize = new Vector3(100,100); targetZRot = 360; }));
            StartCoroutine(Track(secondSpecies.transform));

            DetachSmellyListeners();
            AttachSmellyListener<int>(nodelink, "OnFocused", i=>ExplainRemove2());
        }
        void ExplainRemove2()
        {
            StopAllCoroutines(); // stop tracking
            targetAnchor = new Vector2(.5f, 0);
            targetPos = new Vector2(160, 50);
            targetZRot = 450;

            help.Show(true);
            help.SetDistFromTop(.7f);
            help.SetText("And tap this skull button to remove the species.");

            DetachSmellyListeners();
            AttachSmellyListener(nodelink, "OnUnfocused", ()=>ExplainRemove1(0));
            AttachSmellyListener<int>(inspector, "OnUserDespawned", i=>ExplainUndo(1.5f));
        }
        void ExplainUndo(float wait)
        {
            targetAnchor = new Vector2(0, 0);
            targetZRot = 405;

            help.Show(false);
            help.SetDistFromTop(.2f);
            targetSize = new Vector2(100,100);
            targetPos = new Vector2(40, 120);
            recorder.gameObject.SetActive(true); 

            StartCoroutine(WaitThenDo(wait, ()=> { help.Show(true); help.SetText("And don't worry if you make mistakes! You can always undo any move you make, using these controls in the bottom left. Try that now."); }));

            DetachSmellyListeners();
            AttachSmellyListener<int>(recorder, "OnSpeciesUndone", (i)=>ExplainFinishCondition(1.5f));
        }
        void ExplainFinishCondition(float delay)
        {
            StopAllCoroutines();
            targetSize = new Vector2(0,0);
            targetZRot = 315;

            help.SetDistFromTop(.05f);
            help.Show(false);
            // help.SetSide(false, false);

            StartCoroutine(WaitThenDo(delay, ()=> { help.Show(true); help.SetText("You may finish the game by tapping the button in the top right, but only once all of your species can coexist. Reconstruct your ecosystem to complete this tutorial!"); score.DisableFinish(false); }));

            DetachSmellyListeners();
            AttachSmellyListener(score, "OnLevelCompletabled", ExplainFinish);
        }
        void ExplainFinish()
        {
            StopAllCoroutines();
            targetSize = new Vector2(100,100);
            targetAnchor = rt.anchorMin = rt.anchorMax = new Vector2(1,1);
            targetPos = rt.anchoredPosition = new Vector2(-90,-90);

            help.SetText("Well done! Tap this button to finish the level.");
            help.Show(true);

            DetachSmellyListeners();
            AttachSmellyListener<Levels.Level>(GameManager.Instance.PlayedLevel, "OnFinished", Finish);
            AttachSmellyListener(score, "OnLevelIncompletabled", ()=>ExplainFinishCondition(0));
        }
        Level finishedLevel = null;
        void Finish(Levels.Level finished)
        {
            targetAnchor = new Vector2(.5f,0);
            targetZRot = 405;
            targetPos = new Vector2(140, 70);
            smoothTime = .5f;

            help.SetSide(false, false);
            help.SetDistFromTop(.1f);
            help.SetWidth(.7f);

            finishedLevel = finished;
            DetachSmellyListeners();
            AttachSmellyListener(finishedLevel.NextLevel, "OnCarded", ExplainNextLevel);
        }
        Canvas onTop;
        void ExplainNextLevel()
        {
            // targetSize = new Vector2(140, 140);
            targetPos = new Vector2(50, 300);

            if (onTop == null)
            {
                onTop = gameObject.AddComponent<Canvas>();
                onTop.overrideSorting = true;
                onTop.sortingOrder = 3;
            }

            DetachSmellyListeners();
            AttachSmellyListener(finishedLevel.NextLevel, "OnThumbnailed", ()=>Finish(finishedLevel));
        }

        // bool waiting = false;
        IEnumerator WaitThenDo(float seconds, Action Todo)
        {
            if (seconds > 0)
                yield return new WaitForSeconds(seconds);
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