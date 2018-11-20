using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace EcoBuilder.Model
{
	public class Monitor : MonoBehaviour
	{
		// public void SetFlux(float flux)
		// {

		// }
		[SerializeField] Text flux;
		[SerializeField] Text height;
		[SerializeField] Text stability;
		[SerializeField] float tweenSpeed;

		Func<float, float> scale = x=>Mathf.Log(x);
		Dictionary<int, float> abundances;

		public void SetFlux(string flu)
		{
			flux.text = flu;
		}
		public void SetHeight(string hei)
		{
			height.text = hei;
		}
		public void SetLAS(string las)
		{
			stability.text = las;
		}
	}
}