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

            nodes = graph.gameObject.GetComponentsInChildren<NodeLink.Node>();
            Assert.IsTrue(nodes.Length == 4);

            graph.SetIfNodeCanBeSource(0, false);
            graph.SetIfNodeCanBeTarget(0, false);
            graph.SetIfNodeCanBeSource(2, false);
            graph.SetIfNodeCanBeTarget(2, false);

            graph.SetIfNodeCanBeTarget(1, false);
            graph.SetIfNodeCanBeSource(3, false);

            ExplainIntro();
        }
        void ExplainIntro()
        {
            DetachSmellyListeners();

            WaitThenDo(2, ()=>{targetSize = new Vector2(100, 100); smoothTime = .5f; DragAndDrop(nodes[1].transform, nodes[3].transform, 2.6f); });

            AttachSmellyListener(graph, "OnLayedOut", ()=>CheckChainOfHeight(2, ()=>ExplainWrongLoop(1.5f)));
        }

        void CheckChainOfHeight(int heightGoal, Action Todo)
        {
            if (graph.MaxChain == heightGoal) {
                Todo.Invoke();
            }
        }
        void ExplainWrongLoop(float delay)
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            targetSize = Vector2.zero;

            help.Showing = false;
            WaitThenDo(delay, ()=>{ help.Message = "Oops! You may think that this is a loop, because there is a ring of animals eating each other, but it is not. This is because the links do not all flow around in one direction. Let's fix that by first removing the link you just made."; help.Showing = true; });

            AttachSmellyListener(graph, "OnLayedOut", ()=>CheckChainOfHeight(3, ()=>ExplainWrongLoop2(1f)));
        }
        void ExplainWrongLoop2(float delay)
        {
            DetachSmellyListeners();

            graph.SetIfNodeCanBeSource(1, false);
            graph.SetIfNodeCanBeTarget(1, true);
            graph.SetIfNodeCanBeSource(3, true);
            graph.SetIfNodeCanBeTarget(3, false);

            help.Showing = false;
            WaitThenDo(delay, ()=>{ help.Message = "And now add the same link back, but going the other direction."; help.Showing = true; targetSize = new Vector2(100,100); DragAndDrop(nodes[3].transform, nodes[1].transform, 3f); });

            AttachSmellyListener(graph, "OnLayedOut", ()=>CheckLoopOfLength(3, ()=>ExplainLoopThree(1.5f)));
        }
        void CheckLoopOfLength(int lengthGoal, Action Todo)
        {
            if (graph.MaxLoop == lengthGoal) {
                Todo.Invoke();
            }
        }
        void ExplainLoopThree(float delay)
        {
            DetachSmellyListeners();
            StopAllCoroutines();
            graph.ForceUnfocus();

            graph.SetIfLinkRemovable(3, 1, false);
            graph.SetIfNodeCanBeSource(1, true);
            graph.SetIfNodeCanBeTarget(1, false);
            graph.SetIfNodeCanBeSource(3, false);

            help.Showing = false;
            targetSize = Vector2.zero;
            WaitThenDo(delay, ()=>{ help.Showing = true; help.SetAnchorHeight(.5f); help.Message = "Great! You have now created an ecosystem loop, and the icon on the left panel should reflect this. You can interact with this icon to highlight the species in your loop. Let's try one more thing. First add one more species."; Point(); targetZRot = 30; targetSize = new Vector2(100,100); targetAnchor = new Vector2(0,1); targetPos = new Vector2(55, -400); constraints.LimitPaw(4); });

            AttachSmellyListener(inspector, "OnIncubated", ()=>{ targetSize=Vector2.zero; help.Showing=false; });
            AttachSmellyListener<int, bool, GameObject>(inspector, "OnSpawned", (i,b,g)=>{ Assert.IsTrue(i == 4); extraTransform=g.transform; });
            AttachSmellyListener(graph, "OnLayedOut", ()=>RequestDoubleLoop1(1f));
        }
        Transform extraTransform;

        void RequestDoubleLoop1(float delay)
        {
            Assert.IsNotNull(extraTransform);

            DetachSmellyListeners();
            StopAllCoroutines();

            help.Showing = false;
            targetZRot = 0;
            WaitThenDo(delay, ()=>{ help.Showing = true; help.SetAnchorHeight(.85f); help.Message = "Let's add another loop by first making it eat this animal."; targetSize = new Vector2(100,100); DragAndDrop(nodes[1].transform, extraTransform, 3f); });

            AttachSmellyListener(graph, "OnLayedOut", ()=>RequestDoubleLoop2(1f));
        }
        void RequestDoubleLoop2(float delay)
        {
            DetachSmellyListeners();
            StopAllCoroutines();

            graph.SetIfLinkRemovable(1, 4, false);
            graph.SetIfNodeCanBeTarget(3, true);

            help.Showing = false;
            targetSize = Vector2.zero;
            targetZRot = 0;
            WaitThenDo(delay, ()=>{ help.Showing = true; help.SetAnchorHeight(.5f); help.Message = "And now make this animal eat the new species."; targetSize = new Vector2(100,100); DragAndDrop(extraTransform, nodes[3].transform, 3f); });

            AttachSmellyListener(graph, "OnLayedOut", ()=>ExplainDoubleLoop(2f));
        }
        void ExplainDoubleLoop(float delay)
        {
            Assert.IsTrue(graph.MaxLoop==3 && graph.NumMaxLoop==2, $"{graph.MaxLoop} {graph.NumMaxLoop}");

            DetachSmellyListeners();
            StopAllCoroutines();

            ////////
            // help should be covered by the completed message
            ///////

            targetSize = Vector2.zero;

            // undo previous locks
            graph.SetIfLinkRemovable(3, 1, true);
            graph.SetIfLinkRemovable(1, 4, true);

            for (int i=0; i<5; i++)
            {
                graph.SetIfNodeCanBeSource(i, true);
                graph.SetIfNodeCanBeTarget(i, true);
                inspector.HideSizeSlider(false);
            }

            constraints.ConstrainLoop(3);
            recorder.Hide(false);
            score.Hide(false);
            score.DisableStarCalculation(false);
        }
    }
}