using System;
using System.Collections;
using UnityEngine;

namespace EcoBuilder.Levels
{
    // this tutorial teaches constraints window
    public class Tutorial3 : Tutorial
    {
        protected override void StartLesson()
        {
            // targetSize = new Vector2(100,100);

            ExplainIntro();
        }
        void ExplainIntro()
        {
            targetSize = new Vector2(100,100);
            targetPos = new Vector2(100,-220);
            targetAnchor = new Vector2(0,1);
            targetZRot = 30;

            Detach?.Invoke();
            
            Action foo = ()=> ExplainFocus();
            Action<bool> fooo = (b)=> { targetSize = Vector2.zero; help.Show(false); };
            Action foooo = ()=> { targetSize = new Vector2(100,100); };
            score.OnLevelCompletabled += foo;
            constraints.OnErrorShown += fooo;
            help.OnUserShown += foooo;
            Detach = ()=> { score.OnLevelCompletabled -= foo; constraints.OnErrorShown -= fooo; help.OnUserShown -= foooo; };
        }
        void ExplainFocus()
        {
            // inspector.Uninspect();
            nodelink.ForceUnfocus();
            help.SetDistFromTop(.2f);

            StartCoroutine(WaitThenDo(2, ()=>{ help.Show(true); help.SetText("Well done! If your ecosystem gets too large, you can use two fingers to pinch and zoom out. You can press once on empty space to reset the zoom.");}));

            Detach();
        }
        IEnumerator WaitThenDo(float seconds, Action Todo)
        {
            yield return new WaitForSeconds(seconds);
            Todo();
        }
    }
}