using System;
using UnityEngine;

namespace EcoBuilder.UI
{
    public partial class Tutorial
    {
        public void StartDynamicLesson()
        {
            inspector.HideSizeSlider(false);
            inspector.FixInitialSize(.5f);
            inspector.FixInitialGreed(.3f);
            status.HideScore(true);
            status.HideConstraints(true);
            recorder.gameObject.SetActive(false);
            Teaching = true;

            ExplainIntro();
        }
        void ExplainSize()
        {
            inspector.HideSizeSlider(false);
            help.Show(true);
            help.SetText("You can use this slider to change the size of your species. Try making your plant bigger or smaller.");

            bar();
            Action<int, float, float> foo = (i,x,y)=> { help.Show(false); StartCoroutine(WaitThenDo(3, ()=>ExplainFinish())); };
            Action fooo = ()=> { help.Show(true); ExplainFirstEcosystem(); inspector.HideSizeSlider(true); };
            Action<int> foooo = (i)=>{if (i!=firstIdx) fooo();};
            inspector.OnUserSizeSet += foo;
            nodelink.OnUnfocused += fooo;
            nodelink.OnNodeFocused += foooo;

            bar = ()=> { inspector.OnUserSizeSet -= foo; nodelink.OnUnfocused -= fooo; nodelink.OnNodeFocused -= foooo; };
            GetComponent<Animator>().SetInteger("Progress", 5);
        }

        void ExplainFinish()
        {
            help.SetText("You should notice that the smaller the plant, the larger its population. This is exactly how real world works! It is because smaller species, such as grass, grow much faster than larger ones, like trees. Press the finish flag in the top right to finish this level when you are ready.");
            help.Show(true);
            help.SetWidth(.8f);
            help.SetDistFromTop(.01f);
            Teaching = false;

            bar();
            Action foo = ()=> SetFinishMessage();
            status.OnLevelCompleted += foo;
            bar = ()=> status.OnLevelCompleted -= foo;
            GetComponent<Animator>().SetInteger("Progress", 6);
        }

    }
}