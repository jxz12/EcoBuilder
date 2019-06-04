using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EcoBuilder.Menu
{
	public class LevelCard : MonoBehaviour
	{
		[SerializeField] Text title;
		[SerializeField] Text description;
		[SerializeField] Text producers;
		[SerializeField] Text consumers;
		[SerializeField] Button startEnd;
		[SerializeField] Text startEndText;

        public void Show(string title, string description, int numProducers, int numConsumers)
		{
			this.title.text = title;
			this.description.text = description;
			this.producers.text = numProducers.ToString();
			this.consumers.text = numConsumers.ToString();
			GetComponent<Animator>().SetBool("Showing", true);
            startEndText.text = "Go!";
		}
		public void Show()
		{
			GetComponent<Animator>().SetBool("Showing", true);
            startEndText.text = "Quit?";
		}
		public void Hide()
		{
			GetComponent<Animator>().SetBool("Showing", false);
		}
		public void StartEndGame()
		{
			// TODO: probably store a variable instead of this crap
			if (startEndText.text == "Go!")
			{
				GameManager.Instance.PlayGame();
			}
			else
			{
				// TODO: 'are you sure' option
				GameManager.Instance.ReturnToMenu(0);
			}
			GetComponent<Animator>().SetBool("Showing", false);
		}
	}
}