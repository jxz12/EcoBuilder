using System;
using System.Collections;
using UnityEngine;

namespace EcoBuilder.Levels
{
    // this tutorial teaches constraints window and disjoint
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
            help.SetText("Let's put your skills to the test! Here you must add 2 plants and two animals, and must have at least 5 interactions between them. Use this level to If you get stuck and do not know why, then you can press and hold this panel to the left to receive an explanation. Good luck!");

            DetachSmellyListeners();
            AttachSmellyListener(score, "OnLevelCompletabled", ExplainFocus);
            AttachSmellyListener(help, "OnUserShown", ()=>targetSize = new Vector2(100,100));
            AttachSmellyListener<bool>(constraints, "OnErrorShown", (b)=>{ targetSize = Vector2.zero; help.Show(false); });
        }
        void ExplainFocus()
        {
            // inspector.Uninspect();
            nodelink.ForceUnfocus();
            help.SetDistFromTop(.2f);

            StartCoroutine(WaitThenDo(2, ()=>{ help.Show(true); help.SetText("Well done! The next few levels will show you something");}));

            DetachSmellyListeners();
        }
        IEnumerator WaitThenDo(float seconds, Action Todo)
        {
            yield return new WaitForSeconds(seconds);
            Todo();
        }
    }
}