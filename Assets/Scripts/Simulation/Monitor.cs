using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

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

	public void AddSpecies(int idx)
	{
		abundances[idx] = 0;
	}
	public void RemoveSpecies(int idx)
	{
		abundances.Remove(idx);
	}
	public void SetAbundance(int idx, float abundance)
	{
		abundances[idx] = abundance;
	}
	// public void SetHeight(float maxTrophic)
	// {
	// 	height.text = maxTrophic.ToString("0.0");
	// }
	// public void SetLAS(float las)
	// {
	// 	stability.text = las.ToString("0.0");
	// }
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