using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace EcoBuilder.Tutorials
{
    // this tutorial teaches the idea of loops
    public class TutorialLoop : Tutorial
    {
        NodeLink.Node[] nodes = new NodeLink.Node[4];
        protected override void StartLesson()
        {
            targetSize = Vector2.zero;

            inspector.HideSizeSlider();
            inspector.HideGreedSlider();
            inspector.HideRemoveButton();
            score.Hide();
            score.DisableStarCalculation();

            constraints.ConstrainLeaf(1);
            constraints.ConstrainPaw(3);

            nodes = nodelink.gameObject.GetComponentsInChildren<NodeLink.Node>();
            Assert.IsTrue(nodes.Length == 4);

            nodelink.SetIfNodeInteractable(0, false);
            nodelink.SetIfNodeCanBeTarget(1, false);
            nodelink.SetIfNodeInteractable(2, false);
            nodelink.SetIfNodeCanBeSource(3, false);

            ExplainIntro();
        }
        void ExplainIntro()
        {
            DetachSmellyListeners();

            DetachSmellyListeners();
            help.Showing = true;
            // now make them connect the lowest animal to the highest

            targetSize = new Vector2(100, 100);
            smoothTime = .6f;
            if (GameManager.Instance.ReverseDragDirection) {
                StartCoroutine(Shuffle(nodes[3].transform, nodes[1].transform, 2.5f));
            } else {
                StartCoroutine(Shuffle(nodes[1].transform, nodes[3].transform, 2.5f));
            }

            AttachSmellyListener(nodelink, "OnLayedOut", ()=>CheckChainOfHeight(2, ()=>ExplainWrongLoop(1.5f)));
        }

        void CheckChainOfHeight(int heightGoal, Action Todo)
        {
            if (nodelink.MaxChain == heightGoal) {
                Todo.Invoke();
            }
        }
        void ExplainWrongLoop(float delay)
        {
            StopAllCoroutines();

            targetSize = Vector2.zero;

            help.Showing = false;
            StartCoroutine(WaitThenDo(delay, ()=>{ help.Message = "Oops! You may think that this is a loop, because there is a ring of species connected to the plant, but it is not. This is because the links to not all flow around in one direction. Let's fix that by first removing the link you just made."; help.Showing = true; }));

            DetachSmellyListeners();
            AttachSmellyListener(nodelink, "OnLayedOut", ()=>CheckChainOfHeight(3, ExplainWrongLoop2));
        }
        void ExplainWrongLoop2()
        {
            help.Showing = true;
            help.Message = "And now add the same link back, but going the other direction.";

            DetachSmellyListeners();
            AttachSmellyListener(nodelink, "OnLayedOut", ()=>CheckLoopOfLength(3, ExplainLoopThree));
        }
        void CheckLoopOfLength(int lengthGoal, Action Todo)
        {
            if (nodelink.MaxLoop == lengthGoal) {
                Todo.Invoke();
            }
        }
        void ExplainLoopThree()
        {
            help.Showing = true;
            help.Message = "Great! You have now created an ecosystem loop, and the icon on the left panel should reflect this. You can press this icon to highlight the species in your loop. Let's try one more thing. First add one more species.";

            DetachSmellyListeners();
            AttachSmellyListener(nodelink, "OnLayedOut", ()=>CheckLoopOfLength(4, ExplainDoubleLoop1));
        }

        void CheckLoopNumber(int numGoal, Action Todo)
        {
            if (nodelink.NumMaxLoop == numGoal) {
                Todo.Invoke();
            }
        }
        void ExplainDoubleLoop1()
        {
            help.Showing = true;
            help.Message = "Then make the animal eating the plant eat it.";

            DetachSmellyListeners();
            AttachSmellyListener(nodelink, "OnLayedOut", ()=>CheckLoopNumber(2, ExplainDoubleLoop2));
        }
        void ExplainDoubleLoop2()
        {
            help.Message = "Good! There are now two loops in the same ecosystem. However, your score will only ";
        }
    }
}