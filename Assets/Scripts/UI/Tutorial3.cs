using System;
using System.Collections;
using UnityEngine;

namespace EcoBuilder.UI
{
    public class Tutorial3 : Tutorial
    {
        protected override void StartLesson()
        {
            // inspector.HideSizeSlider(false);
            // inspector.HideGreedSlider(false);
            // inspector.HidePlantPawButton(true);
            // inspector.HideRemoveButton(true);
            // status.HideScore(true);
            status.HideConstraints(true);
            // status.PauseScoreCalculation(true);
            targetSize = new Vector2(100,100);

            ExplainIntro();
        }
        Action Detach;
        void ExplainIntro()
        {
            targetSize = new Vector2(100,100);
            targetPos = new Vector2(50,-200);
            targetAnchor = new Vector2(0,1);
            targetZRot = 45;

            help.SetText("Let's put your skills to the test! Try to construct the best ecosystem you can, given the constraints shown in the left. Feel free to try the tutorials again if you get stuck! If you cannot finish the level and do not know why, then you can press here for an explanation.");

            if (Detach != null)
                Detach();
            
            Action foo = ()=> ExplainFocus();
            status.OnLevelCompletable += foo;
            Detach = ()=> status.OnLevelCompletable -= foo;
        }
        void ExplainFocus()
        {
            help.SetText("Well done! If your ecosystem gets too crowded, you can press a species twice to focus only on the species that it is directly connected to.");
        }
        IEnumerator WaitThenDo(float seconds, Action Todo)
        {
            yield return new WaitForSeconds(seconds);
            Todo();
        }
    }
}