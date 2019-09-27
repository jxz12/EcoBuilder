using System;
using System.Collections;
using UnityEngine;

namespace EcoBuilder
{
    public class Tutorial1 : Tutorial
    {
        Action bar;
        protected override void StartLesson()
        {
            inspector.HideSizeSlider(true);
            inspector.HideGreedSlider(true);
            inspector.FixInitialSize(.5f);
            inspector.FixInitialGreed(.3f);
            inspector.HideRemoveButton();
            status.HideScore(true);
            status.HideConstraints(true);
            recorder.gameObject.SetActive(false);

            targetSize = rt.sizeDelta;
            targetPos = rt.anchoredPosition;
            targetAnchor = rt.anchorMin;
            targetZRotation = rt.rotation.eulerAngles.z;

            ExplainIntro();
        }
        void ExplainIntro()
        {
            if (bar != null)
                bar();

            help.SetText("Welcome to EcoBuilder! Let's build your first ecosystem. Try spinning the world around by dragging it, or add your first species by pressing the leaf in the bottom right.");
            help.SetSide(false);
            help.SetDistFromTop(.2f);

            inspector.SetConsumerAvailability(false);
            inspector.OnIncubated += ExplainInspector;

            bar = ()=> { inspector.OnIncubated -= ExplainInspector; };
        }

        void ExplainInspector()
        {
            help.SetText("Here you can choose a new name for your species. You can then introduce it by dragging it into the world.");
            help.Show(true);
            help.SetSide(true);
            help.SetDistFromTop(.1f);

            bar();
            Action<int, GameObject> foo = (x,g)=> ExplainSpawn(g,x);
            Action fooo = ()=> ExplainIntro();
            inspector.OnShaped += foo;
            nodelink.OnEmptyPressed += fooo;

            bar = ()=> { inspector.OnShaped -= foo; nodelink.OnEmptyPressed -= fooo; };
            targetPos = new Vector2(160, 50);
            targetAnchor = new Vector2(.5f, 0);
            targetZRotation = 90;
        }
        GameObject firstSpecies;
        int firstIdx;
        void ExplainSpawn(GameObject first, int idx)
        {
            firstSpecies = first;
            firstIdx = idx;
            inspector.SetConsumerAvailability(true);
            inspector.SetProducerAvailability(false);

            // help.SetDistFromTop(.05f);
            help.SetText("Your " + firstSpecies.name + " is born! Plants grow on their own, and so do not need to eat any other species. Now try adding an animal by pressing the paw button.");
            help.SetDistFromTop(.02f);
            help.Show(true);

            bar();
            Action<int, GameObject> foo = (x,g)=> ExplainInteraction(g,x);
            Action fooo = ()=> { help.Show(false); };
            Action foooo = ()=> help.Show(true);
            inspector.OnShaped += foo;
            inspector.OnIncubated += fooo;
            inspector.OnUnincubated += foooo;
            bar = ()=> { inspector.OnShaped -= foo; inspector.OnIncubated -= fooo; inspector.OnUnincubated -= foooo; };
        }
        void WaitForAnimal()
        {
            // TODO: make this the state after spawn
        }

        // TODO: get references to both species, and map the drag cursor to them
        GameObject secondSpecies;
        int secondIdx;
        void ExplainInteraction(GameObject second, int idx)
        {
            secondSpecies = second;
            secondIdx = idx;
            inspector.SetConsumerAvailability(false);
            help.SetText("Your " + secondSpecies.name + " is hungry! Drag from it to the " + firstSpecies.name + " to give it some food.");
            help.Show(true);

            bar();
            Action<int, int> foo = (i,j)=> { ExplainFirstEcosystem(); help.Show(false); StartCoroutine(WaitThenDo(2, ()=>help.Show(true))); };
            nodelink.OnUserLinked += foo;
            bar = ()=> nodelink.OnUserLinked -= foo;
        }
        void ExplainFirstEcosystem()
        {
            help.SetText("Well done! You have built your first ecosystem. Now let's edit your species. Click on your " + firstSpecies.name + " to examine it again.");

            bar();
            Action<int> foo = (i)=> { if (i==0) ExplainRemove(); };
            nodelink.OnNodeFocused += foo;
            bar = ()=> nodelink.OnNodeFocused -= foo;
        }
        void ExplainRemove()
        {
            // TODO: remove link (oh no! don't worry if you make mistakes, you can undo)
        }
        void ExplainUndo()
        {
            // TODO: explain undoing and removing species
            recorder.gameObject.SetActive(true);
        }
        IEnumerator WaitThenDo(float seconds, Action Todo)
        {
            yield return new WaitForSeconds(seconds);
            Todo();
        }
        void SetFinishMessage()
        {
            help.SetText("Congratulations on finishing the first tutorial! The next few levels will introduce more concepts that you will need to know to play the game.");

            bar();
            GetComponent<Animator>().SetInteger("Progress", 7);
        }
    }
}