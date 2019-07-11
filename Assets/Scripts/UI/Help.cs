using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace EcoBuilder.UI
{
	public class Help : MonoBehaviour
	{
		[SerializeField] Text text;
		[SerializeField] VerticalLayoutGroup panelLayout;
		[SerializeField] ContentSizeFitter panelFitter;
		public void SetText(string toSet)
		{
			text.text = toSet;
			Canvas.ForceUpdateCanvases();
			panelLayout.CalculateLayoutInputVertical();
			panelLayout.SetLayoutVertical();
			panelFitter.SetLayoutVertical();
		}
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