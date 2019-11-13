using System;
using System.Collections;
using UnityEngine;

namespace EcoBuilder.Levels
{
    public class Tutorial2 : Tutorial
    {
        GameObject plant, animal;
        protected override void StartLesson()
        {
            inspector.HideSizeSlider(false);
            inspector.HideGreedSlider(true);
            inspector.HidePlantPawButton(true);
            inspector.HideRemoveButton(true);
            score.HideScore(true);
            score.HideConstraints(true);
            score.DisableFinish(true);
            targetSize = new Vector2(100,100);

            var nodes = nodelink.gameObject.GetComponentsInChildren<NodeLink.Node>();
            animal = nodes[1].gameObject;
            plant = nodes[0].gameObject;

            ExplainIntro();
        }
        void ExplainIntro()
        {
            help.SetText("The animal below is going extinct, even though it has food! Let's fix that. Start by focusing on a species, by pressing it. A blue outline means that a species or link cannot be removed.");
            help.SetSide(false);
            help.SetDistFromTop(.15f);

            Detach?.Invoke();
            
            track = true;
            StartCoroutine(Track(plant.transform));
            shuffle = false;

            Action<int> foo = (i)=> ExplainSize();
            nodelink.OnNodeFocused += foo;
            Detach = ()=> nodelink.OnNodeFocused -= foo;
        }

        void ExplainSize()
        {
            help.SetText("You can change the weight of your species by moving this slider. Here, your animal is going extinct because it is not getting enough food. See if you can save it!");
            help.Show(true);
            help.SetSide(true, false);
            help.SetDistFromTop(.01f);

            shuffle = true;
            StartCoroutine(ShuffleOnSlider(3, 40));
            track = false;

            Detach();
            Action<int> fooo = (i)=> ExplainMetabolism(2);
            Action foooo = ()=> ExplainIntro();
            model.OnRescued += fooo;
            nodelink.OnUnfocused += foooo;
            Detach = ()=> { model.OnRescued -= fooo; nodelink.OnUnfocused -= foooo; };
        }
        void ExplainMetabolism(float delay)
        {
            help.Show(false);
            inspector.Uninspect();
            nodelink.FullUnfocus();

            StartCoroutine(WaitThenDo(delay, ()=>{ help.SetText("Well done! You can save the animal here by giving it more food. This is achieved by making itself or its food source lighter, as lighter species grow and eat faster. This is exactly what happens in the real world! Grass spreads faster than oak trees. A swarm of locusts devours a field much faster than a herd of cows. Try pressing a species again."); help.SetWidth(.8f); help.Show(true); help.SetDistFromTop(.03f); }));

            targetSize = Vector2.zero;
            shuffle = false;
            // track = true;
            // StartCoroutine(Track(animal.transform));

            Detach();
            Action<int> foo = (i)=> ExplainInterference();
            nodelink.OnNodeFocused += foo;
            Detach = ()=> nodelink.OnNodeFocused -= foo;
        }
        void ExplainInterference()
        {
            help.Show(false);

            help.SetText("The other trait you can change is known as 'interference'. The higher the interference, the more a species competes with others of its own species, and so the lower its maximum population. Try changing it to see the effects.");
            help.Show(true);
            inspector.HideGreedSlider(false);

            // track = false;
            shuffle = true;
            StartCoroutine(ShuffleOnSlider(3, 90));

            Detach();
            Action<int,float,float> foo = (i,x,y)=> ExplainScore();
            inspector.OnUserGreedSet += foo;
            Action fooo = ()=> ExplainMetabolism(0);
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

            StartCoroutine(WaitThenDo(3, ()=> { inspector.Uninspect(); nodelink.FullUnfocus(); help.SetSide(false,false); help.SetDistFromTop(.13f); StartCoroutine(WaitThenDo(2, ()=>{help.SetText("This bar at the top displays your score. It measures the 'complexity' of your ecosystem, and is based on three things: the number of species, the number of links, and the strengths of interaction, indicated by the flow along the links. Getting enough points will earn you more stars – good luck!"); help.Show(true);})); score.HideScore(false); score.DisableFinish(false); }));

            Detach();
            Action foo = ()=> Finish();
            score.OnLevelCompleted += foo;
        }
        void Finish()
        {
            help.SetWidth(.7f);
            help.SetDistFromTop(.25f);
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
            if (seconds > 0)
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