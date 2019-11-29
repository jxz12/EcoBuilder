using System;
using System.Collections;
using UnityEngine;

namespace EcoBuilder.Levels
{
    public class Tutorial1 : Tutorial
    {
        protected override void StartLesson()
        {
            inspector.HideSizeSlider(true);
            inspector.HideGreedSlider(true);
            inspector.FixIncubatedSize(.5f);
            inspector.FixIncubatedGreed(.5f);
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

            help.SetText("Welcome to EcoBuilder! Let's build your first ecosystem. Try spinning the world around by dragging it, or add your first species by tapping the leaf in the bottom right.");
            help.SetSide(false);
            help.SetDistFromTop(.2f);
            help.SetWidth(.6f);
            inspector.SetConsumerAvailability(false);

            Detach?.Invoke();
            inspector.OnIncubated += ExplainInspector;
            Detach = ()=> { inspector.OnIncubated -= ExplainInspector; };
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

            Detach();
            Action<int, GameObject> foo = (x,g)=> ExplainSpawn(g,x);
            Action fooo = ()=> ExplainIntro();
            inspector.OnShaped += foo;
            nodelink.OnEmptyPressed += fooo;
            Detach = ()=> { inspector.OnShaped -= foo; nodelink.OnEmptyPressed -= fooo; };
        }
        GameObject firstSpecies;
        int firstIdx;
        void ExplainSpawn(GameObject first, int idx)
        {
            targetAnchor = new Vector2(1,0);
            targetPos = new Vector2(-61, 60);
            targetZRot = 270;

            firstSpecies = first;
            firstIdx = idx;
            inspector.SetConsumerAvailability(true);
            inspector.SetProducerAvailability(false);

            help.SetText("Your " + firstSpecies.name + " is born! Plants grow on their own, and so do not need to eat any other species. Now try adding an animal by pressing the paw button.");
            help.SetDistFromTop(.02f, false);
            help.Show(true);

            Detach();
            Action<int, GameObject> foo = (x,g)=> ExplainInteraction(g,x);
            Action fooo = ()=> { help.Show(false); targetSize = Vector2.zero; };
            Action foooo = ()=> { help.Show(true); targetSize = new Vector2(100,100); };
            inspector.OnShaped += foo;
            inspector.OnIncubated += fooo;
            inspector.OnUnincubated += foooo;
            Detach = ()=> { inspector.OnShaped -= foo; inspector.OnIncubated -= fooo; inspector.OnUnincubated -= foooo; };
        }

        GameObject secondSpecies;
        int secondIdx;
        void ExplainInteraction(GameObject second, int idx)
        {
            secondSpecies = second;
            secondIdx = idx;
            inspector.SetConsumerAvailability(false);
            help.SetText("Your " + secondSpecies.name + " is hungry! Drag from it to the " + firstSpecies.name + " to give it some food.");
            help.Show(true);

            shuffle = true;
            targetSize = new Vector3(100,100);
            targetAnchor = new Vector3(0f,0f);
            targetZRot = 360;
            StartCoroutine(Shuffle(firstSpecies.transform, secondSpecies.transform, 2f));
            inspector.HidePlantPawButton();

            Detach();
            Action<int, int> foo = (i,j)=> ExplainFirstEcosystem(true);
            nodelink.OnUserLinked += foo;
            Detach = ()=> nodelink.OnUserLinked -= foo;
        }
        void ExplainFirstEcosystem(bool wait)
        {
            shuffle = false;
            targetSize = new Vector2(0,0);
            help.Show(false);
            inspector.Uninspect();
            nodelink.FullUnfocus();

            StartCoroutine(WaitThenDo(wait?2:0, ()=> { help.Show(true); help.SetText("You have created your very own ecosystem. Well done! Now try removing the link you just made, by performing the same dragging action from the animal to the plant."); }));

            Detach();
            Action<int,int> foo = (i,j)=> ExplainRemove1(1);
            nodelink.OnUserUnlinked += foo;
            Detach = ()=> nodelink.OnUserUnlinked -= foo;
        }
        void ExplainRemove1(float wait)
        {
            help.Show(false);
            inspector.Uninspect();
            nodelink.FullUnfocus();
            inspector.HideRemoveButton(false);
            targetAnchor = new Vector2(0,0);

            StartCoroutine(WaitThenDo(wait, ()=> { help.Show(true); help.SetDistFromTop(.3f); help.SetText("You can also remove species entirely if you wish. Try clicking on one of your species to focus on it."); targetSize = new Vector3(100,100); targetZRot = 360; }));

            track = true;
            StartCoroutine(Track(secondSpecies.transform));

            Detach();
            Action<int> foo = (i)=>ExplainRemove2();
            nodelink.OnNodeFocused += foo;
            Detach = ()=> nodelink.OnNodeFocused -= foo;
        }
        void ExplainRemove2()
        {
            targetAnchor = new Vector2(.5f, 0);
            targetPos = new Vector2(160, 50);
            targetZRot = 450;
            track = false;

            help.Show(true);
            help.SetDistFromTop(.7f);
            help.SetText("And click this skull button to remove the species.");

            Detach();
            Action foo = ()=> ExplainRemove1(0);
            nodelink.OnUnfocused += foo;
            Action<int> fooo = (i)=> ExplainUndo(1.5f);
            inspector.OnUserDespawned += fooo;
            Detach = ()=> { nodelink.OnUnfocused -= foo; inspector.OnUserDespawned -= fooo; };
        }
        void ExplainUndo(float wait)
        {
            targetAnchor = new Vector2(0, 0);
            targetZRot = 405;

            help.Show(false);
            help.SetDistFromTop(.2f);
            targetSize = new Vector2(100,100);
            targetPos = new Vector2(40, 85);
            recorder.gameObject.SetActive(true); 

            StartCoroutine(WaitThenDo(wait, ()=> { help.Show(true); help.SetText("And don't worry if you make mistakes! You can always undo any move you make, using these controls in the bottom left. Try that now."); }));

            Detach();
            Action<int> foo = (i)=> ExplainFinishCondition(1.5f);
            recorder.OnSpeciesUndone += foo;
            Detach = ()=> recorder.OnSpeciesUndone -= foo;
        }
        void ExplainFinishCondition(float waitSeconds)
        {
            targetSize = new Vector2(0,0);
            targetZRot = 315;

            help.SetDistFromTop(.05f);
            help.Show(false);
            // help.SetSide(false, false);

            StartCoroutine(WaitThenDo(waitSeconds, ()=> { help.Show(true); help.SetText("You may finish the game by pressing the button in the top right, but only once all of your species can coexist. Reconstruct your ecosystem to complete this tutorial!"); score.DisableFinish(false); }));
            // TODO: this breaks if they add the link back before removing species

            // score.HideConstraints(false);
            Detach();
            Action foo = ()=> ExplainFinish();
            score.OnLevelCompletabled += foo;
            Detach = ()=> score.OnLevelCompletabled -= foo;
        }
        void ExplainFinish()
        {
            targetSize = new Vector2(100,100);
            targetAnchor = rt.anchorMin = rt.anchorMax = new Vector2(1,1);
            targetPos = rt.anchoredPosition = new Vector2(-90,-90);

            help.SetText("Well done! Press this button to finish the level.");
            help.Show(true);

            Detach();
            Action foo = ()=> Finish();
            Action fooo = ()=> ExplainFinishCondition(0);
            score.OnLevelCompleted += foo;
            score.OnLevelIncompletabled += fooo;
            Detach = ()=>{ score.OnLevelCompleted -= foo; score.OnLevelIncompletabled -= fooo; };
        }
        Level nextLevel = null;
        void Finish()
        {
            targetAnchor = new Vector2(.5f,0);
            targetZRot = 405;
            targetPos = new Vector2(140, 70);
            smoothTime = .5f;

            help.SetSide(false, false);
            help.SetDistFromTop(.1f);
            help.SetWidth(.7f);
            // help.SetText("Well done! You have built your first ecosystem. In the next level you will learn how to edit the traits of your species, changing the way they interact with each other. Press this button at the bottom to select the next level.");

            if (nextLevel == null)
                nextLevel = GameManager.Instance.PlayedLevel.NextLevel;

            Detach();
            Action foo = ()=>ExplainNextLevel();
            nextLevel.OnCarded += foo;
            Detach = ()=>nextLevel.OnCarded -= foo;
        }
        Canvas onTop;
        void ExplainNextLevel()
        {
            // targetSize = new Vector2(140, 140);
            targetPos = new Vector2(50, 300);

            onTop = gameObject.AddComponent<Canvas>();
            onTop.overrideSorting = true;
            onTop.sortingOrder = 3;

            Detach();
            Action foo = ()=>Finish();
            nextLevel.OnThumbnailed += foo;
            Detach = ()=>nextLevel.OnThumbnailed -= foo;
        }

        // bool waiting = false;
        IEnumerator WaitThenDo(float seconds, Action Todo)
        {
            if (seconds > 0)
                yield return new WaitForSeconds(seconds);
            Todo();
        }
        bool shuffle = false;
        IEnumerator Shuffle(Transform grab, Transform drop, float time)
        {
            float start = Time.time;
            transform.position = ScreenPos(Camera.main.WorldToViewportPoint(grab.position));
            float prevSmoothTime = smoothTime;
            smoothTime = .7f;
            while (shuffle)
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
            smoothTime = prevSmoothTime;
        }
        bool track = false;
        IEnumerator Track(Transform tracked)
        {
            Point();
            while (track)
            {
                targetPos = ScreenPos(Camera.main.WorldToViewportPoint(tracked.position)) + new Vector2(0,-20);
                yield return null;
            }
        }
    }
}