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

			// StartCoroutine(OneFrameUpdateCanvas());
		}
		// IEnumerator OneFrameUpdateCanvas()
		// {
		// 	yield return null;
		// 	Canvas.ForceUpdateCanvases();
		// 	// panel.CalculateLayoutInputVertical();
		// 	// panel.SetLayoutVertical();
		// }
		public void SetY(float y)
		{
			panelLayout.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, y);
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