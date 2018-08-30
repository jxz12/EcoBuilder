using UnityEngine;
using System;
using System.Collections.Generic;

public class ECG : MonoBehaviour
{
	[SerializeField] GameObject linePrefab;

	Func<float, float> yAxisScale = x=>Mathf.Log10(x);
	Dictionary<int, ECGLine> lines = new Dictionary<int, ECGLine>();
	private Transform worldParent, worldGrandParent;
	
	void Awake()
	{
	}

	public void AddLine(int idx)
	{

	}
	// TODO: make this bleeping change transparency
	public void Bleep(int idx, float value)
	{
	
	}
}