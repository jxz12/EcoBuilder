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
			slider.onValueChanged.AddListener(x=> ChooseMetabolism(x));

			// hatchable = x=> WaitForHatchable();
		}
		public void Enter()
		{
			slider.normalizedValue = .5f;
			button.interactable = true;
			button.GetComponent<Animator>().SetTrigger("Enter");
			slider.GetComponent<Animator>().SetTrigger("Choosing");
		}
		public void Exit()
		{
			slider.GetComponent<Animator>().SetTrigger("Exit");
			button.GetComponent<Animator>().SetTrigger("Exit");
		}
		public void Choose()
		{
			slider.GetComponent<Animator>().SetTrigger("Enter");
			button.interactable = false;
			chosen = false;
		}

		bool chosen = false;
		public void ChooseMetabolism(float metabolism)
		{
			if (chosen == false)
			{
				OnMetabolismChosen();
			}
			chosen = true;
		}
	}

}