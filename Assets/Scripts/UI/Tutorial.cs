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
        // [SerializeField] Image pointer;

        Action bar;
        void Start()
        {
            inspector.HideSizeSlider();
            inspector.HideGreedSlider();
            inspector.FixSize(.5f);
            inspector.FixGreed(.5f);
            inspector.HideRemoveButton();
            status.HideScore();
            status.HideConstraints();
            recorder.gameObject.SetActive(false);

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
            Action fooo = ()=> Start();
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

            help.SetText("Your " + speciesName + " is born! Plants grow on their own, and so do not need food. Now try adding an animal by pressing the paw button.");
            help.Show(true);
            firstSpeciesName = speciesName;

            bar();
            Action<int, GameObject> foo = (x,g)=> ExplainInteraction(g.name);
            inspector.OnShaped += foo;
            bar = ()=> inspector.OnShaped -= foo;
            GetComponent<Animator>().SetInteger("Progress", 2);
        }
        void ExplainInteraction(string speciesName)
        {
            inspector.SetConsumerAvailability(false);
            help.SetText("Your " + speciesName + " is hungry! Drag from it to the " + firstSpeciesName + " to give it some food.");
            help.Show(true);

            bar();
            Action<int, int> foo = (i,j)=> ExplainFinishFlag();
            nodelink.OnUserLinked += foo;
            bar = ()=> nodelink.OnUserLinked -= foo;
            GetComponent<Animator>().SetInteger("Progress", 3);
        }
        // TODO: add in size introduction here!!!
        void ExplainFinishFlag()
        {
            help.Show(false);
            help.SetText("Well done! You have built your first ecosystem. Press the red finish flag to complete the level!");
            StartCoroutine(WaitThenShow());

            bar();
            status.OnLevelCompleted += SetFinishMessage;
            GetComponent<Animator>().SetInteger("Progress", 4);
        }
        IEnumerator WaitThenShow()
        {
            yield return new WaitForSeconds(2);
            help.Show(true);
        }
        void SetFinishMessage()
        {
            help.SetText("Congratulations! You have built your first ecosystem. The next few levels will introduce more concepts that you need to know to play the game.");
            gameObject.SetActive(false);
        }
    }
}