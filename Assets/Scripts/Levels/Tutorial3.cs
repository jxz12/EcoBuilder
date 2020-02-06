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
            ExplainIntro();
        }
        void ExplainIntro()
        {
            targetSize = new Vector2(100,100);
            targetPos = new Vector2(100,-220);
            targetAnchor = new Vector2(0,1);
            targetZRot = 30;

            DetachSmellyListeners();
            AttachSmellyListener(score, "OnLevelCompletabled", ExplainFocus);
            AttachSmellyListener(help, "OnUserShown", ()=>targetSize = new Vector2(100,100));
            AttachSmellyListener<bool>(constraints, "OnErrorShown", (b)=>{ targetSize = Vector2.zero; help.Show(false); });
        }
        void ExplainFocus()
        {
            nodelink.ForceUnfocus();
            help.SetAnchorHeight(.8f);

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