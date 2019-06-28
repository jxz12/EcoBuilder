using UnityEngine;
using UnityEngine.UI;
using System;

namespace EcoBuilder.UI
{
	public class Help : MonoBehaviour
	{
		[SerializeField] Text text;
		public void SetText(string toSet)
		{
			text.text = toSet;
		}
		// public void SetText(string text, Action)
		// {

		// }
		public void Show(bool showing)
		{
			GetComponent<Animator>().SetBool("Show", showing);
		}
		public void DelayThenShow()
		{
			GetComponent<Animator>().SetTrigger("Reset");
			GetComponent<Animator>().SetBool("Show", true);
		}
	}
}