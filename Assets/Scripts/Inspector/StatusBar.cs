using System;
using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder
{
	public class StatusBar : MonoBehaviour
	{
		[SerializeField] Button undo, redo, menu;
		[SerializeField] Image star1, star2, star3;

		public event Action OnUndo;
		public event Action OnRedo;
		public event Action OnMenu;

		void Start()
		{
			undo.onClick.AddListener(()=> OnUndo());
			redo.onClick.AddListener(()=> OnRedo());
			menu.onClick.AddListener(()=> OnMenu());
		}

		public void FillStar1()
		{
			star1.color = Color.yellow;
		}
		public void FillStar2()
		{
			star2.color = Color.yellow;
		}
		public void FillStar3()
		{
			star3.color = Color.yellow;
		}
		public void EmptyStar1()
		{
			star1.color = Color.white;
		}
		public void EmptyStar2()
		{
			star2.color = Color.white;
		}
		public void EmptyStar3()
		{
			star3.color = Color.white;
		}
	}
}