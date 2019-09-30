using System;
using System.Collections;
using UnityEngine;

namespace EcoBuilder.UI
{
    public class Tutorial2 : Tutorial
    {
        protected override void StartLesson()
        {
            inspector.HideSizeSlider(false);
            inspector.HideGreedSlider(true);
            inspector.HidePlantPawButton(true);
            inspector.HideRemoveButton(true);
            status.HideScore(true);
            status.HideConstraints(true);
            status.PauseScoreCalculation(true);

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

            Action<int> foo = (i)=> ExplainSize();
            nodelink.OnNodeFocused += foo;
            Detach = ()=> nodelink.OnNodeFocused -= foo;
        }

        void ExplainSize()
        {
            help.SetText("You can change the body size of your species by moving this slider. Try moving the slider to see the effects on your ecosystem.");
            help.Show(true);
            help.SetSide(true);
            help.SetDistFromTop(.1f);

            shuffle = true;
            StartCoroutine(ShuffleOnSlider(3));

            Detach();
            Action<int,float,float> foo = (i,x,y)=> ExplainMetabolism(i);
            Action fooo = ()=> ExplainIntro();
            inspector.OnUserSizeSet += foo;
            nodelink.OnEmptyPressed += fooo;
            Detach = ()=> { inspector.OnUserSizeSet -= foo; nodelink.OnEmptyPressed -= fooo; };
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
            inspector.OnUserSizeSet += foo;
            nodelink.OnNodeFocused += fooo;
            Detach = ()=> { inspector.OnUserSizeSet -= foo; nodelink.OnNodeFocused -= fooo; };
        }
        int secondSized = -1;
        void ExplainInterference(int secondIdx)
        {
            help.Show(false);

            StartCoroutine(WaitThenDo(3, ()=> { help.SetText("If you choose the correct values, you should notice that sometimes the animal cannot be sustained by the plant. The other trait you can change is a species' interference, which determines the maximum population a species can reach. Try changing both sliders, and see how it affects each species!"); help.Show(true); inspector.HideGreedSlider(false); shuffle = true; StartCoroutine(ShuffleOnSlider(3)); }));

            Detach();
            Action<int,float,float> foo = (i,x,y)=> ExplainScore();
            inspector.OnUserGreedSet += foo;
            Detach = ()=> inspector.OnUserGreedSet -= foo;
        }
        void ExplainScore()
        {
            help.Show(false);
            targetSize = Vector2.zero;

            StartCoroutine(WaitThenDo(3, ()=> { help.SetSide(false); help.SetDistFromTop(.13f); help.SetText("This bar at the top displays your score. It measures the 'complexity' of your ecosystem, and is based on three things: the number of species, the number of links, and the strengths of these interactions, which is indicated by the flow along its link. Getting enough points will earn you more stars – good luck!"); help.Show(true); }));

            shuffle = false;

            status.HideScore(false);
            status.PauseScoreCalculation(false);
        }
        bool shuffle = false;
        IEnumerator ShuffleOnSlider(float time)
        {
            float start = Time.time;
            targetAnchor = new Vector2(.5f, 0);
            targetSize = new Vector2(50,50);
            float prevSmooth = smoothTime;
            smoothTime = .9f;
            Grab();
            while (shuffle)
            {
                if (((Time.time - start) % time) < (time/2f))
                {
                    targetPos = new Vector2(-60,40);
                }
                else
                {
                    targetPos = new Vector2(130,40);
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
    }
}