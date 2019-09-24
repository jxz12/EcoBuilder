using System;
using System.Collections;
using UnityEngine;

namespace EcoBuilder.UI
{
    public class Tutorial : MonoBehaviour
    {
        [SerializeField] Help help;
        [SerializeField] Inspector inspector;
        [SerializeField] StatusBar status;
        [SerializeField] NodeLink.NodeLink nodelink;
        [SerializeField] MoveRecorder recorder;

        public bool Teaching { get; private set; } = false;
        Action bar;
        void Start()
        {
            inspector.HideSizeSlider(true);
            inspector.HideGreedSlider(true);
            inspector.FixInitialSize(.5f);
            inspector.FixInitialGreed(.3f);
            inspector.HideRemoveButton();
            status.HideScore();
            status.HideConstraints();
            recorder.gameObject.SetActive(false);

            Teaching = true;
            ExplainIntro();
        }
        void ExplainIntro()
        {
            if (bar != null)
                bar();
            help.SetText("Welcome to EcoBuilder! Let's build your first ecosystem. Try spinning the world around by dragging it, or add your first species by pressing the leaf in the bottom right.");
            help.SetSide(true);
            help.SetDistFromTop(.05f);

            inspector.SetConsumerAvailability(false);
            inspector.OnIncubated += ExplainInspector;

            bar = ()=> { inspector.OnIncubated -= ExplainInspector; };
            GetComponent<Animator>().SetInteger("Progress", 0);
        }

        void ExplainInspector()
        {
            help.SetText("Here you can choose a new name for your species. You can then introduce it by dragging it into the world.");
            help.Show(true);

            bar();
            Action<int, GameObject> foo = (x,g)=> ExplainSpawn(g,x);
            Action fooo = ()=> ExplainIntro();
            inspector.OnShaped += foo;
            nodelink.OnEmptyPressed += fooo;

            bar = ()=> { inspector.OnShaped -= foo; nodelink.OnEmptyPressed -= fooo; };
            GetComponent<Animator>().SetInteger("Progress", 1);
        }
        GameObject firstSpecies;
        int firstIdx;
        void ExplainSpawn(GameObject first, int idx) // TODO: add another state to show pointer to paw
        {
            firstSpecies = first;
            firstIdx = idx;
            inspector.SetConsumerAvailability(true);
            inspector.SetProducerAvailability(false);

            help.SetText("Your " + firstSpecies.name + " is born! Plants grow on their own, and so do not need to eat any other species. Now try adding an animal by pressing the paw button.");
            help.Show(true);

            bar();
            Action<int, GameObject> foo = (x,g)=> ExplainInteraction(g,x);
            Action fooo = ()=> help.Show(false);
            Action foooo = ()=> help.Show(true);
            inspector.OnShaped += foo;
            inspector.OnIncubated += fooo;
            inspector.OnUnincubated += foooo;
            bar = ()=> { inspector.OnShaped -= foo; inspector.OnIncubated -= fooo; inspector.OnUnincubated -= foooo; };
            GetComponent<Animator>().SetInteger("Progress", 2);
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
            GetComponent<Animator>().SetInteger("Progress", 3);
        }
        void ExplainFirstEcosystem()
        {
            help.SetText("Well done! You have built your first ecosystem. Now let's edit your species. Click on your " + firstSpecies.name + " to examine it again.");

            bar();
            Action<int> foo = (i)=> { if (i==0) ExplainSize(); };
            nodelink.OnNodeFocused += foo;
            bar = ()=> nodelink.OnNodeFocused -= foo;
            GetComponent<Animator>().SetInteger("Progress", 4);
        }
        IEnumerator WaitThenDo(float seconds, Action Todo)
        {
            yield return new WaitForSeconds(seconds);
            Todo();
        }
        void ExplainSize()
        {
            inspector.HideSizeSlider(false);
            help.Show(true);
            help.SetText("You can use this slider to change the size of your species. Try making your plant bigger and smaller.");

            bar();
            Action<int, float, float> foo = (i,x,y)=> ExplainFinish();
            Action fooo = ()=> { help.Show(true); ExplainFirstEcosystem(); inspector.HideSizeSlider(true); };
            Action<int> foooo = (i)=>{if (i!=firstIdx) fooo();};
            inspector.OnUserSizeSet += foo;
            nodelink.OnUnfocused += fooo;
            nodelink.OnNodeFocused += foooo;

            bar = ()=> { inspector.OnUserSizeSet -= foo; nodelink.OnUnfocused -= fooo; nodelink.OnNodeFocused -= foooo; };
            GetComponent<Animator>().SetInteger("Progress", 5);
        }

        // TODO: make this an overall delay, instead of one inside the function;
        void ExplainFinish()
        {
            help.Show(false);
            StartCoroutine(WaitThenDo(3, ()=>help.Show(true)));
            help.SetText("The bigger the species looks, the larger its population. You should notice that the smaller the species, the larger its population. This is exactly how real world works! Smaller species, such as grass, can grow much faster than larger ones, like trees. Press the finish flag in the top right to finish this level when you are ready.");
            Teaching = false;

            bar();
            Action foo = ()=> SetFinishMessage();
            status.OnLevelCompleted += foo;
            bar = ()=> status.OnLevelCompleted -= foo;
            GetComponent<Animator>().SetInteger("Progress", 6);
        }
        void SetFinishMessage()
        {
            help.SetText("Congratulations on finishing the first tutorial! The next few levels will introduce more concepts that you will need to know to play the game.");

            bar();
            GetComponent<Animator>().SetInteger("Progress", 7);
        }
    }
}