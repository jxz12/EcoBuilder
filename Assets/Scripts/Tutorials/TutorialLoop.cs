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
            inspector.FixSizeInitialValue(-3);
            inspector.HideRemoveButton();
            score.Hide();
            score.DisableStarCalculation();

            nodes = nodelink.gameObject.GetComponentsInChildren<NodeLink.Node>();
            Assert.IsTrue(nodes.Length == 4);

            nodelink.SetIfNodeCanBeSource(0, false);
            nodelink.SetIfNodeCanBeTarget(0, false);
            nodelink.SetIfNodeCanBeSource(2, false);
            nodelink.SetIfNodeCanBeTarget(2, false);

            nodelink.SetIfNodeCanBeTarget(1, false);
            nodelink.SetIfNodeCanBeSource(3, false);

            ExplainIntro();
        }
        void ExplainIntro()
        {
            DetachSmellyListeners();

            StartCoroutine(WaitThenDo(2, ()=>{targetSize = new Vector2(100, 100); smoothTime = .5f; StartCoroutine(DragAndDrop(nodes[1].transform, nodes[3].transform, 2.6f)); }));

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
            DetachSmellyListeners();
            StopAllCoroutines();

            targetSize = Vector2.zero;

            help.Showing = false;
            StartCoroutine(WaitThenDo(delay, ()=>{ help.Message = "Oops! You may think that this is a loop, because there is a ring of animals eating each other, but it is not. This is because the links do not all flow around in one direction. Let's fix that by first removing the link you just made."; help.Showing = true; }));

            AttachSmellyListener(nodelink, "OnLayedOut", ()=>CheckChainOfHeight(3, ()=>ExplainWrongLoop2(1f)));
        }
        void ExplainWrongLoop2(float delay)
        {
            DetachSmellyListeners();

            nodelink.SetIfNodeCanBeSource(1, false);
            nodelink.SetIfNodeCanBeTarget(1, true);
            nodelink.SetIfNodeCanBeSource(3, true);
            nodelink.SetIfNodeCanBeTarget(3, false);

            help.Showing = false;
            StartCoroutine(WaitThenDo(delay, ()=>{ help.Message = "And now add the same link back, but going the other direction."; help.Showing = true; targetSize = new Vector2(100,100); StartCoroutine(DragAndDrop(nodes[3].transform, nodes[1].transform, 3f)); }));

            AttachSmellyListener(nodelink, "OnLayedOut", ()=>CheckLoopOfLength(3, ()=>ExplainLoopThree(1.5f)));
        }
        void CheckLoopOfLength(int lengthGoal, Action Todo)
        {
            if (nodelink.MaxLoop == lengthGoal) {
                Todo.Invoke();
            }
        }
        void ExplainLoopThree(float delay)
        {
            DetachSmellyListeners();
            StopAllCoroutines();
            nodelink.ForceUnfocus();

            nodelink.SetIfLinkRemovable(3, 1, false);
            nodelink.SetIfNodeCanBeSource(1, true);
            nodelink.SetIfNodeCanBeTarget(1, false);
            nodelink.SetIfNodeCanBeSource(3, false);

            help.Showing = false;
            targetSize = Vector2.zero;
            StartCoroutine(WaitThenDo(delay, ()=>{ help.Showing = true; help.SetAnchorHeight(.5f); help.Message = "Great! You have now created an ecosystem loop, and the icon on the left panel should reflect this. You can interact with this icon to highlight the species in your loop. Let's try one more thing. First add one more species."; Point(); targetZRot = 30; targetSize = new Vector2(100,100); targetAnchor = new Vector2(0,1); targetPos = new Vector2(55, -400); constraints.LimitPaw(4); }));

            AttachSmellyListener(inspector, "OnIncubated", ()=>{ targetSize=Vector2.zero; help.Showing=false; });
            AttachSmellyListener<int, bool, GameObject>(inspector, "OnSpawned", (i,b,g)=>{ Assert.IsTrue(i == 4); extraTransform=g.transform; });
            AttachSmellyListener(nodelink, "OnLayedOut", ()=>RequestDoubleLoop1(1f));
        }
        Transform extraTransform;

        void RequestDoubleLoop1(float delay)
        {
            Assert.IsNotNull(extraTransform);

            DetachSmellyListeners();
            StopAllCoroutines();

            help.Showing = false;
            targetZRot = 0;
            StartCoroutine(WaitThenDo(delay, ()=>{ help.Showing = true; help.SetAnchorHeight(.85f); help.Message = "Let's add another loop by first making it eat this animal."; targetSize = new Vector2(100,100); StartCoroutine(DragAndDrop(nodes[1].transform, extraTransform, 3f)); }));

            AttachSmellyListener(nodelink, "OnLayedOut", ()=>RequestDoubleLoop2(1f));
        }
        void RequestDoubleLoop2(float delay)
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            nodelink.SetIfLinkRemovable(1, 4, false);
            nodelink.SetIfNodeCanBeTarget(3, true);

            help.Showing = false;
            targetSize = Vector2.zero;
            targetZRot = 0;
            StartCoroutine(WaitThenDo(delay, ()=>{ help.Showing = true; help.SetAnchorHeight(.5f); help.Message = "And now make this animal eat the new species."; targetSize = new Vector2(100,100); StartCoroutine(DragAndDrop(extraTransform, nodes[3].transform, 3f));}));

            AttachSmellyListener(nodelink, "OnLayedOut", ()=>ExplainDoubleLoop(2f));
        }
        void ExplainDoubleLoop(float delay)
        {
            Assert.IsTrue(nodelink.MaxLoop==3 && nodelink.NumMaxLoop==2, $"{nodelink.MaxLoop} {nodelink.NumMaxLoop}");

            DetachSmellyListeners();
            StopAllCoroutines();

            ////////
            // help should be covered by the completed message
            ///////

            targetSize = Vector2.zero;

            // undo previous locks
            nodelink.SetIfLinkRemovable(3, 1, true);
            nodelink.SetIfLinkRemovable(1, 4, true);

            for (int i=0; i<5; i++)
            {
                nodelink.SetIfNodeCanBeSource(i, true);
                nodelink.SetIfNodeCanBeTarget(i, true);
                inspector.HideSizeSlider(false);
            }

            constraints.ConstrainLoop(3);
            recorder.Hide(false);
            score.Hide(false);
            score.DisableStarCalculation(false);
        }
    }
}