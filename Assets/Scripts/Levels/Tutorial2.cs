using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EcoBuilder.Levels
{
    public class Tutorial2 : Tutorial
    {
        GameObject plant, animal;
        protected override void StartLesson()
        {
            inspector.HideIncubateButton(true);
            inspector.HideRemoveButton(true);
            score.HideScore(true);
            score.HideConstraints(true);
            score.DisableFinish(true);
            targetSize = new Vector2(100,100);
            targetZRot = 45;

            var nodes = nodelink.gameObject.GetComponentsInChildren<NodeLink.Node>();
            animal = nodes[1].gameObject;
            plant = nodes[0].gameObject;

            ExplainIntro();
        }
        IEnumerator trackRoutine;
        IEnumerator shuffleRoutine;
        void ExplainIntro()
        {
            help.SetText("The animal below is going extinct, even though it has food! Let's fix that. Start by focusing on the plant, by pressing it. A blue outline means that a species or link cannot be removed.");
            help.SetSide(false);
            help.SetDistFromTop(.15f);

            Detach?.Invoke();
            
            if (shuffleRoutine != null)
            {
                StopCoroutine(shuffleRoutine);
                shuffleRoutine = null;
            }
            StartCoroutine(trackRoutine = Track(plant.transform));

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

            if (trackRoutine != null)
            {
                StopCoroutine(trackRoutine);
                trackRoutine = null;
            }
            StartCoroutine(shuffleRoutine = ShuffleOnSlider(3, 40));

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

            if (shuffleRoutine != null)
            {
                StopCoroutine(shuffleRoutine);
                shuffleRoutine = null;
            }
            smoothTime = .3f;
            targetSize = Vector2.zero;

            StartCoroutine(WaitThenDo(delay, ()=>{
                help.SetText("Well done! You can save the animal here by giving it more food. This is achieved by making itself or its food source lighter, as lighter species grow and eat faster. This is exactly what happens in the real world! Grass spreads faster than oak trees. A swarm of locusts devours a field much faster than a herd of cows. Try pressing the plant again."); help.SetWidth(.75f); help.Show(true); help.SetDistFromTop(.03f); StartCoroutine(trackRoutine = Track(plant.transform)); targetSize = new Vector2(100,100);
            }));


            Detach();
            Action<int> foo = (i)=> ExplainInterference();
            nodelink.OnNodeFocused += foo;
            Detach = ()=> nodelink.OnNodeFocused -= foo;
        }
        // TODO: remove this entirely
        void ExplainInterference()
        {
            help.Show(false);

            help.SetText("The other trait you can change is known as 'interference'. The higher the interference, the more a species competes with others of its own species, and so the lower its maximum population. Try changing it to see if you can make the animal go extinct!");
            help.Show(true);
            inspector.HideGreedSlider(false);

            if (trackRoutine != null)
            {
                StopCoroutine(trackRoutine);
                trackRoutine = null;
            }
            StartCoroutine(shuffleRoutine = ShuffleOnSlider(3, 90));

            Detach();
            // Action<int,float,float> foo = (i,x,y)=> ExplainScore();
            // inspector.OnUserGreedSet += foo;
            // Action fooo = ()=> ExplainMetabolism(0);
            // nodelink.OnUnfocused += fooo;
            // Detach = ()=> { inspector.OnUserGreedSet -= foo; nodelink.OnUnfocused -= fooo; };

            Action<int> foo = (i)=> ExplainScore();
            model.OnEndangered += foo;
            Action fooo = ()=> ExplainMetabolism(0);
            nodelink.OnUnfocused += fooo;
            Action<int, float, float> foooo = (i,x,y)=> help.Show(false);
            inspector.OnUserGreedSet += foooo;
            Detach = ()=> { model.OnEndangered -= foo; nodelink.OnUnfocused -= fooo; inspector.OnUserGreedSet -= foooo;};
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
            StopCoroutine(shuffleRoutine);
            smoothTime = .2f;

            inspector.Uninspect();
            nodelink.FullUnfocus();
            help.SetSide(false,false);
            help.SetDistFromTop(.13f);

            // StartCoroutine(WaitThenDo(2, ()=>{
            //     help.SetText("Good job! This bar at the top displays your score, and is based on the size and health of your ecosystem. You can press your score to get a detailed report of what is coming from where. Getting enough points will earn you more stars – good luck!"); help.Show(true); score.HideScore(false); score.DisableFinish(false);
            // }));
            StartCoroutine(WaitThenDo(2, ()=>{
                help.SetText("Good job! This bar at the top displays your score, and is based on the size and total health of your ecosystem. Getting enough points will earn you more stars – good luck!"); help.Show(true); score.HideScore(false); score.DisableFinish(false);
            }));

            Detach();
            Action foo = ()=> Finish();
            score.OnLevelCompleted += foo;
        }
        void Finish()
        {
            help.SetWidth(.7f);
            help.SetDistFromTop(.25f);
        }
        IEnumerator WaitThenDo(float seconds, Action Todo)
        {
            if (seconds > 0)
                yield return new WaitForSeconds(seconds);
            Todo();
        }
        IEnumerator Track(Transform tracked)
        {
            targetAnchor = new Vector2(0,0);
            targetSize = new Vector2(100,100);
            smoothTime = .2f;
            Point();
            while (true)
            {
                targetPos = ScreenPos(Camera.main.WorldToViewportPoint(tracked.position)) + new Vector2(0,-20);
                yield return null;
            }
        }
        IEnumerator ShuffleOnSlider(float period, float yPos)
        {
            float start = Time.time - period/4;
            targetAnchor = new Vector2(.5f, 0);
            targetSize = new Vector2(50,50);
            targetZRot = 0;
            smoothTime = .7f;
            Grab();
            while (true)
            {
                if (((Time.time - start) % period) < (period/2f))
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
        }
    }
}