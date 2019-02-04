using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.Menu
{
	public class StartButton : MonoBehaviour {

		// [SerializeField] Slider boardSize;
		// [SerializeField] LevelSelect levelSelect;

		public void LoadGame()
		{
			GameManager.Instance.UnloadScene("Menu");
			GameManager.Instance.LoadScene("Play");
		}
	}

}