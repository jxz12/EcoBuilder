using System;
using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.UI
{
	public class StatusBar : MonoBehaviour
	{
		[SerializeField] Button undo, menu;
		[SerializeField] Animator star1, star2, star3;

		public event Action OnUndo;
		public event Action OnMenu;

		void Start()
		{
			undo.onClick.AddListener(()=> OnUndo());
			menu.onClick.AddListener(()=> OnMenu());
		}

		public void FillStars(bool feasible, bool stable, bool nonreactive)
		{
            if (feasible)
                FillStar1();
            else
                EmptyStar1();
            if (stable)
                FillStar2();
            else
                EmptyStar2();
            if (nonreactive) // might be too hard
                FillStar3();
            else
                EmptyStar3();
		}


		// TODO: make stars dance and stuff
		void FillStar1()
		{
			star1.SetBool("Filled", true);
		}
		void FillStar2()
		{
			star2.SetBool("Filled", true);
		}
		void FillStar3()
		{
			star3.SetBool("Filled", true);
		}
		void EmptyStar1()
		{
			star1.SetBool("Filled", false);
		}
		void EmptyStar2()
		{
			star2.SetBool("Filled", false);
		}
		void EmptyStar3()
		{
			star3.SetBool("Filled", false);
		}

		public void ShowErrorMessage(string message)
		{
			print(message);
		}
		public void Finish()
		{
			GetComponent<Animator>().SetTrigger("Finish");
		}
	}
}