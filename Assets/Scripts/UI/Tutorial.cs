using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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
            inspector.FixInitialGreed(.5f);
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
            Action<int, GameObject> foo = (x,g)=> ExplainSpawn(g.name);
            Action fooo = ()=> ExplainIntro();
            inspector.OnShaped += foo;
            nodelink.OnEmptyPressed += fooo;

            bar = ()=> { inspector.OnShaped -= foo; nodelink.OnEmptyPressed -= fooo; };
            GetComponent<Animator>().SetInteger("Progress", 1);
        }
        string firstSpeciesName;
        void ExplainSpawn(string speciesName)
        {
            inspector.SetConsumerAvailability(true);
            inspector.SetProducerAvailability(false);

            help.SetText("Your " + speciesName + " is born! Plants grow on their own, and so do not need to eat any other species. Now try adding an animal by pressing the paw button.");
            help.Show(true);
            firstSpeciesName = speciesName;

            bar();
            Action<int, GameObject> foo = (x,g)=> ExplainInteraction(g.name);
            inspector.OnShaped += foo;
            bar = ()=> inspector.OnShaped -= foo;
            GetComponent<Animator>().SetInteger("Progress", 2);
        }
        // TODO: get references to both species, and map the drag cursor to them
        void ExplainInteraction(string speciesName)
        {
            inspector.SetConsumerAvailability(false);
            help.SetText("Your " + speciesName + " is hungry! Drag from it to the " + firstSpeciesName + " to give it some food.");
            help.Show(true);

            bar();
            Action<int, int> foo = (i,j)=> ExplainFirstEcosystem();
            nodelink.OnUserLinked += foo;
            bar = ()=> nodelink.OnUserLinked -= foo;
            GetComponent<Animator>().SetInteger("Progress", 3);
        }
        void ExplainFirstEcosystem()
        {
            help.Show(false);
            help.SetText("Well done! You have built your first ecosystem. Now let's edit your species. Click on your " + firstSpeciesName + " again.");
            StartCoroutine(WaitThenShowHelp(2));

            bar();
            Action<int> foo = (i)=> { if (i==0) ExplainSize(); };
            nodelink.OnNodeFocused += foo;
            bar = ()=> nodelink.OnNodeFocused -= foo;
            GetComponent<Animator>().SetInteger("Progress", 4);
        }
        IEnumerator WaitThenShowHelp(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            help.Show(true);
        }
        void ExplainSize()
        {
            inspector.HideSizeSlider(false);
            help.Show(true);
            help.SetText("You can use this slider to change the size of your species. Try making your plant bigger and smaller.");

            bar();
            Action<int, float, float> foo = (i,x,y)=> ExplainFinish();
            Action fooo = ()=> ExplainFirstEcosystem();
            inspector.OnUserSizeSet += foo;
            nodelink.OnEmptyPressed += fooo;

            bar = ()=> { inspector.OnUserSizeSet -= foo; nodelink.OnEmptyPressed -= fooo; };
            GetComponent<Animator>().SetInteger("Progress", 5);
        }
        void ExplainFinish()
        {
            help.Show(true);
            help.SetText("You should notice that smaller species grow faster. This is exactly what happens in the real world! Press the finish flag in the top right to finish this level when you are ready.");
            Teaching = false;

            bar();
            Action foo = ()=> SetFinishMessage();
            status.OnLevelCompleted += foo;
            bar = ()=> status.OnLevelCompleted -= foo;
            GetComponent<Animator>().SetInteger("Progress", 6);
        }
        void SetFinishMessage()
        {
            help.SetText("Congratulations! You have built your first ecosystem. The next few levels will introduce more concepts that you need to know to play the game.");

            bar();
            GetComponent<Animator>().SetInteger("Progress", 7);
        }
    }
}