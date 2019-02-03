using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.Inspector
{
	public class Token : MonoBehaviour
	{
		[SerializeField] Slider slider;
		[SerializeField] Button button;

		// [SerializeField] Image background;
		// [SerializeField] Image fill;

		public float Metabolism {
			get { return slider.normalizedValue; }
		}
		public int Number {
			get { return (int)slider.value; }
		}

		public event Action OnChosen;
		public event Action OnHatchable;
		public event Action OnMetabolismChosen;

		void Awake()
		{
			button.onClick.AddListener(()=> OnChosen());
			slider.onValueChanged.AddListener(x=> OnMetabolismChosen());

			// hatchable = x=> WaitForHatchable();
		}
		public void Enter()
		{
			gameObject.SetActive(true);
			slider.normalizedValue = .5f;
			button.GetComponent<Animator>().SetTrigger("Enter");
		}
		public void Exit()
		{
			slider.GetComponent<Animator>().SetTrigger("Exit");
		}
		public void Reset()
		{
			button.GetComponent<Animator>().SetTrigger("Reset");
			slider.GetComponent<Animator>().SetTrigger("Reset");
			gameObject.SetActive(false);
		}
		public void Center()
		{
			slider.GetComponent<Animator>().SetTrigger("Enter");
			button.GetComponent<Animator>().SetTrigger("Exit");
		}
	}

}