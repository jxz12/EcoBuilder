using System;
using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.UI
{
	public class StatusBar : MonoBehaviour
	{
		[SerializeField] Button undo, menu;
		[SerializeField] MeshRenderer star1, star2, star3;
		[SerializeField] Material starEmpty, starFilled;

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
			star1.material = starFilled;
		}
		void FillStar2()
		{
			star2.material = starFilled;
		}
		void FillStar3()
		{
			star3.material = starFilled;
		}
		void EmptyStar1()
		{
			star1.material = starEmpty;
		}
		void EmptyStar2()
		{
			star2.material = starEmpty;
		}
		void EmptyStar3()
		{
			star3.material = starEmpty;
		}

		public void ShowErrorMessage(string message)
		{
			print(message);
		}
	}
}