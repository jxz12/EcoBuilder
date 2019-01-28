using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

namespace EcoBuilder.Model
{
	public class Monitor : MonoBehaviour
	{
		// [SerializeField] Text flux;
		// [SerializeField] Text height;
		// [SerializeField] Text stability;
		// [SerializeField] float tweenSpeed;

		// Func<float, float> scale = x=>Mathf.Log(x);
		// Dictionary<int, float> abundances;

		// public void SetFlux(string flu)
		// {
		// 	flux.text = flu;
		// }
		// public void SetHeight(string hei)
		// {
		// 	height.text = hei;
		// }
		// public void SetLAS(string las)
		// {
		// 	stability.text = las;
		// }


		[SerializeField] Text debug;
		public void Debug(string txt)
		{
			debug.text = txt;
			StartCoroutine(Flash(.1f));
		}
		IEnumerator Flash(float seconds)
		{
			debug.color = Color.green;
			yield return new WaitForSeconds(seconds);
			debug.color = Color.white;
		}
	}
}