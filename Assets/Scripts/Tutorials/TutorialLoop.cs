using System;
using System.Collections;
using UnityEngine;

namespace EcoBuilder.Tutorials
{
    // this tutorial teaches the idea of loops
    public class TutorialLoop : Tutorial
    {
        protected override void StartLesson()
        {
            targetSize = Vector2.zero;

            ExplainIntro();
        }
        void ExplainIntro()
        {
            // ask the user to make a chain of three first
            help.Message = "This level will teach you the final feature of food webs that you will need to know, known as a loop. Let's construct one! The first step is to build a simple chain of length three. Try doing that now.";

            DetachSmellyListeners();
            AttachSmellyListener(nodelink, "OnConstraints", ()=>CheckChainOfHeight(3, ExplainChainThree));
        }
        void CheckChainOfHeight(int heightGoal, Action Todo)
        {
            if (nodelink.MaxChain == heightGoal) {
                Todo.Invoke();
            }
        }
        void ExplainChainThree()
        {
            help.Message = "Now make the animal with the longest chain eat the animal linked to the plant.";
            help.Showing = true;
            // now make them connect the lowest animal to the highest
            print("TODO: disallow other actions somehow, and add track coroutine");

            DetachSmellyListeners();
            AttachSmellyListener(nodelink, "OnConstraints", ()=>CheckChainOfHeight(2, ExplainWrongLoop));
        }
        void ExplainWrongLoop()
        {
            help.Showing = true;
            help.Message = "Oops! You may think that this is a loop, because there is a ring of species connected to the plant, but it is not. This is because the direction around the ring does not go all the way round. Let's fix that by removing the link you just made.";

            DetachSmellyListeners();
            AttachSmellyListener(nodelink, "OnConstraints", ()=>CheckChainOfHeight(3, ExplainWrongLoop2));
        }
        void ExplainWrongLoop2()
        {
            help.Showing = true;
            help.Message = "And now add the same link back, but going the other direction.";

            DetachSmellyListeners();
            AttachSmellyListener(nodelink, "OnConstraints", ()=>CheckLoopOfLength(3, ExplainLoopThree));
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
            AttachSmellyListener(nodelink, "OnConstraints", ()=>CheckLoopOfLength(4, ExplainDoubleLoop1));
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
            AttachSmellyListener(nodelink, "OnConstraints", ()=>CheckLoopNumber(2, ExplainDoubleLoop2));
        }
        void ExplainDoubleLoop2()
        {
            help.Message = "Good! There are now two loops in the same ecosystem. However, your score will only ";
        }

        // create another loop attached to the same one
        // task with 
        IEnumerator WaitThenDo(float seconds, Action Todo)
        {
            yield return new WaitForSeconds(seconds);
            Todo();
        }
    }
}