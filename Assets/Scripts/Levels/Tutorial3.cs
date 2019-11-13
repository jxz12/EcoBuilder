using System;
using System.Collections;
using UnityEngine;

namespace EcoBuilder.Levels
{
    public class Tutorial3 : Tutorial
    {
        protected override void StartLesson()
        {
            // inspector.HideSizeSlider(false);
            // inspector.HideGreedSlider(false);
            // inspector.HidePlantPawButton(true);
            // inspector.HideRemoveButton(true);
            // score.HideScore(true);
            // score.HideConstraints(true);
            // score.PauseScoreCalculation(true);
            targetSize = new Vector2(100,100);

            ExplainIntro();
        }
        void ExplainIntro()
        {
            targetSize = new Vector2(100,100);
            targetPos = new Vector2(100,-270);
            targetAnchor = new Vector2(0,1);
            targetZRot = 30;

            help.SetText("Let's put your skills to the test! Try to construct the best ecosystem you can, given the constraints shown in the left. Feel free to try the tutorials again if you get stuck! If you press and hold this panel to the left, you will receive an explanation for why you cannot finish the level. Good luck!");

            Detach?.Invoke();
            
            Action foo = ()=> ExplainFocus();
            Action fooo = ()=> { targetSize = Vector2.zero; help.Show(false); };
            Action foooo = ()=> { targetSize = new Vector2(100,100); };
            score.OnLevelCompletabled += foo;
            constraints.OnErrorShown += fooo;
            help.OnUserShown += foooo;
            Detach = ()=> { score.OnLevelCompletabled -= foo; constraints.OnErrorShown -= fooo; help.OnUserShown -= foooo; };
        }
        void ExplainFocus()
        {
            inspector.Uninspect();
            nodelink.FullUnfocus();

            StartCoroutine(WaitThenDo(2, ()=>{ help.Show(true); help.SetText("Well done! If your ecosystem gets too crowded, you can press a species twice to focus only on the species that it is directly connected to. This is helpful for navigating a complicated system. Enjoy the rest of the game!");}));

            Detach();
        }
        IEnumerator WaitThenDo(float seconds, Action Todo)
        {
            yield return new WaitForSeconds(seconds);
            Todo();
        }
    }
}