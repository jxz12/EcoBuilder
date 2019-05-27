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

		// TODO: make stars dance and stuff
		public void FillStar1()
		{
			star1.material = starFilled;
		}
		public void FillStar2()
		{
			star2.material = starFilled;
		}
		public void FillStar3()
		{
			star3.material = starFilled;
		}
		public void EmptyStar1()
		{
			star1.material = starEmpty;
		}
		public void EmptyStar2()
		{
			star2.material = starEmpty;
		}
		public void EmptyStar3()
		{
			star3.material = starEmpty;
		}
	}
}