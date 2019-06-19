using System;
using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.UI
{
	public class StatusBar : MonoBehaviour
	{
		// [SerializeField] Button undo;
		// [SerializeField] Animator star1, star2, star3;
		[SerializeField] Constraint edge, chain, loop;
		[SerializeField] Transform levelParent;
		[SerializeField] GameObject finishButton;

		public event Action<int> OnLevelEnd; // int is num stars
		public event Action OnLevelReplay;
		public event Action OnLevelNext;
		public event Action OnBackToMenu;
		// TODO: error messages and stuff like below
            // bool passed = true;
            // if (!model.Feasible)
            // {
            //     passed = false;
            //     status.ShowErrorMessage("Not every species can coexist");
            // }
            // if (nodelink.LongestLoop() < GameManager.Instance.MinLoop)
            // {
            //     passed = false;
            //     status.ShowErrorMessage("No loop longer than " + GameManager.Instance.MinLoop + " exists");
            // }
            // if (nodelink.MaxChainLength() < GameManager.Instance.MinChain)
            // {
            //     passed = false;
            //     status.ShowErrorMessage("No chain taller than " + GameManager.Instance.MinChain + " exists");
            // }
            // if (passed)
            // {
            //     status.Finish();
            //     nodelink.Finish();
            //     inspector.Finish();
            // }
            // int score = 0;
            // if (model.Feasible)
            //     score += 1;
            // if (model.Stable)
            //     score += 1;
            // if (model.Nonreactive)
            //     score += 1;
		void Start()
		{
		}

		public void SlotInLevel(Level level)
		{
			level.transform.SetParent(levelParent, false);
			level.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
		}

		public void ConstrainNumEdges(int numEdges)
		{
			edge.Constrain(numEdges);
		}
		public void ConstrainMaxChain(int lenChain)
		{
			chain.Constrain(lenChain);
		}
		public void ConstrainMaxLoop(int lenLoop)
		{
			loop.Constrain(lenLoop);
		}
		public void DisplayNumEdges(int numEdges)
		{
			edge.Display(numEdges);
			CheckIfSatisfied();
		}
		public void DisplayMaxChain(int lenChain)
		{
			chain.Display(lenChain);
			CheckIfSatisfied();
		}
		public void DisplayMaxLoop(int lenLoop)
		{
			loop.Display(lenLoop);
			CheckIfSatisfied();
		}

		void CheckIfSatisfied()
		{
			if (edge.IsSatisfied && chain.IsSatisfied && loop.IsSatisfied)
			{
				finishButton.SetActive(true);
			}
			else
			{
				finishButton.SetActive(false);
			}
		}

		// public void FillStars(bool feasible, bool stable, bool nonreactive)
		// {
        //     if (feasible)
        //         FillStar1();
        //     else
        //         EmptyStar1();
        //     if (stable)
        //         FillStar2();
        //     else
        //         EmptyStar2();
        //     if (nonreactive) // might be too hard
        //         FillStar3();
        //     else
        //         EmptyStar3();
		// }


		// void FillStar1()
		// {
		// 	star1.SetBool("Filled", true);
		// }
		// void FillStar2()
		// {
		// 	star2.SetBool("Filled", true);
		// }
		// void FillStar3()
		// {
		// 	star3.SetBool("Filled", true);
		// }
		// void EmptyStar1()
		// {
		// 	star1.SetBool("Filled", false);
		// }
		// void EmptyStar2()
		// {
		// 	star2.SetBool("Filled", false);
		// }
		// void EmptyStar3()
		// {
		// 	star3.SetBool("Filled", false);
		// }

		// TODO: make this into prefab instead?
		[SerializeField] Text errorText;
		public void ShowErrorMessage(string message)
		{
			errorText.text = message;
			GetComponent<Animator>().SetTrigger("Show Error");
		}
		public void Finish()
		{
			GetComponent<Animator>().SetTrigger("Finish");
		}
	}
}