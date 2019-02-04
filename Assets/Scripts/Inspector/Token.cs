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
		[SerializeField] MeshFilter numberMesh;

		// [SerializeField] Image background;
		// [SerializeField] Image fill;

		public float Metabolism {
			get { return .1f + .9f*slider.normalizedValue; }
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
			numberMesh.mesh = GameManager.Instance.GetNumberMesh(Number);
			chosen = true;
		}
	}

}