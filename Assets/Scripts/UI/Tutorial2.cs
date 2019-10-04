using System;
using System.Collections;
using UnityEngine;

namespace EcoBuilder.UI
{
    public class Tutorial2 : Tutorial
    {
        GameObject plant;
        protected override void StartLesson()
        {
            inspector.HideSizeSlider(false);
            inspector.HideGreedSlider(true);
            inspector.HidePlantPawButton(true);
            inspector.HideRemoveButton(true);
            status.HideScore(true);
            status.HideConstraints(true);
            status.PauseScoreCalculation(true);
            targetSize = new Vector2(100,100);

            plant = FindObjectsOfType<NodeLink.Node>()[1].gameObject;

            ExplainIntro();
        }
        Action Detach;
        void ExplainIntro()
        {
            help.SetText("Let's edit your species! Please start by focusing on a species, by pressing either of them.");
            help.SetSide(false);
            help.SetDistFromTop(.15f);

            if (Detach != null)
                Detach();
            
            track = true;
            StartCoroutine(Track(plant.transform));
            shuffle = false;

            Action<int> foo = (i)=> ExplainSize();
            nodelink.OnNodeFocused += foo;
            Detach = ()=> nodelink.OnNodeFocused -= foo;
        }

        void ExplainSize()
        {
            help.SetText("You can change the body size of your species by moving this slider. Try moving the slider to see the effects on your ecosystem.");
            help.Show(true);
            help.SetSide(true);
            help.SetDistFromTop(.05f);

            shuffle = true;
            StartCoroutine(ShuffleOnSlider(3, 40));
            track = false;

            Detach();
            Action<int,float,float> foo = (i,x,y)=> ExplainMetabolism(i);
            Action fooo = ()=> ExplainIntro();
            inspector.OnUserSizeSet += foo;
            nodelink.OnUnfocused += fooo;
            Detach = ()=> { inspector.OnUserSizeSet -= foo; nodelink.OnUnfocused -= fooo; };
        }
        int firstSized = -1;
        void ExplainMetabolism(int firstIdx)
        {
            help.Show(false);

            StartCoroutine(WaitThenDo(3, ()=>{ help.SetText("The size of its node indicates the size of a species' population. You should notice that smaller species have bigger populations. This is exactly what happens in the real world! Grass spreads much faster than oak trees. A swarm of locusts devours a field much faster than a herd of cows. Try changing the weight of the other species as well."); help.SetWidth(.8f); help.Show(true); help.SetDistFromTop(.03f); }));

            firstSized = firstIdx;
            shuffle = false;
            targetSize = Vector2.zero;

            Detach();
            Action<int,float,float> foo = (i,x,y)=> {if (i != firstSized) ExplainInterference(i);};
            Action<int> fooo = (i)=> help.Show(false);
            Action foooo = ()=> help.Show(true);
            inspector.OnUserSizeSet += foo;
            nodelink.OnNodeFocused += fooo;
            nodelink.OnUnfocused += foooo;
            Detach = ()=> { inspector.OnUserSizeSet -= foo; nodelink.OnNodeFocused -= fooo; nodelink.OnUnfocused -= foooo; };
        }
        int secondSized = -1;
        void ExplainInterference(int secondIdx)
        {
            help.Show(false);

            StartCoroutine(WaitThenDo(3, ()=> { help.SetText("If you choose the correct values, you should notice that sometimes the animal goes extinct! This is marked by it flashing. The other trait you can change is a species' interference. The higher the interference, the lower its maximum population. Try changing it to see the effects."); help.Show(true); inspector.HideGreedSlider(false); shuffle = true; StartCoroutine(ShuffleOnSlider(3, 90)); }));

            Detach();
            Action<int,float,float> foo = (i,x,y)=> ExplainScore();
            inspector.OnUserGreedSet += foo;
            Action fooo = ()=> help.Show(false);
            nodelink.OnUnfocused += fooo;
            Detach = ()=> { inspector.OnUserGreedSet -= foo; nodelink.OnUnfocused -= fooo; };
        }
        void ExplainScore()
        {
            help.Show(false);
            targetSize = new Vector2(0,0);
            // targetSize = new Vector2(100,100);
            // targetAnchor = new Vector2(.5f,1);
            // targetPos = new Vector2(100, -50);
            // targetZRot = 45;
            // Point();
            shuffle = false;

            StartCoroutine(WaitThenDo(3, ()=> { help.SetSide(false,false); help.SetDistFromTop(.13f); help.SetText("This bar at the top displays your score. It measures the 'complexity' of your ecosystem, and is based on three things: the number of species, the number of links, and the strengths of interaction, indicated by the flow along the links. Getting enough points will earn you more stars – good luck!"); help.Show(true); status.HideScore(false); status.PauseScoreCalculation(false); }));

            Detach();
            Action foo = ()=> Finish();
            status.OnLevelCompleted += foo;
        }
        void Finish()
        {
            help.SetDistFromTop(.2f);
        }
        bool shuffle = false;
        IEnumerator ShuffleOnSlider(float time, float yPos)
        {
            float start = Time.time;
            targetAnchor = new Vector2(.5f, 0);
            targetSize = new Vector2(50,50);
            // rt.anchoredPosition = new Vector2(130,yPos);
            float prevSmooth = smoothTime;
            Grab();
            while (shuffle)
            {
                if (((Time.time - start) % time) < (time/2f))
                {
                    targetPos = new Vector2(-60,yPos);
                }
                else
                {
                    targetPos = new Vector2(130,yPos);
                    smoothTime = 1f;
                }
                yield return null;
            }
            smoothTime = prevSmooth;
        }
        IEnumerator WaitThenDo(float seconds, Action Todo)
        {
            yield return new WaitForSeconds(seconds);
            Todo();
        }
        bool track = false;
        IEnumerator Track(Transform tracked)
        {
            targetAnchor = new Vector2(0,0);
            targetSize = new Vector2(100,100);
            Point();
            while (track)
            {
                targetPos = ScreenPos(Camera.main.WorldToViewportPoint(tracked.position)) + new Vector2(0,-20);
                yield return null;
            }
        }
    }
}