using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace EcoBuilder.Menu
{
	public class LevelCard : MonoBehaviour
	{

		// [SerializeField] Transform miniLevelParent;
		// GameObject miniLevel = null;

		// 	int chosen = 0;
		// 	void Start()
		// 	{
		// 		// NewMiniLevel(GameManager.Instance.SelectedLandscape);
		// 	}

		// 	public void ScrollLeft()
		// 	{
		// 		// GameManager.Instance.SwitchLandscape(false);
		// 		// NewMiniLevel(GameManager.Instance.SelectedLandscape);
		// 	}
		// 	public void ScrollRight()
		// 	{
		// 		// GameManager.Instance.SwitchLandscape(false);
		// 		// NewMiniLevel(GameManager.Instance.SelectedLandscape);
		// 	}
		// 	void NewMiniLevel(GameObject newLevel)
		// 	{
		// 		Quaternion previousRotation;
		// 		if (miniLevel == null)
		// 		{
		// 			previousRotation = Quaternion.Euler(0, -180, 0);
		// 		}
		// 		else
		// 		{
		// 			Destroy(miniLevel);
		// 			previousRotation = miniLevel.transform.localRotation;
		// 		}
		// 		miniLevel = Instantiate(newLevel, miniLevelParent);
		// 		miniLevel.transform.localRotation = previousRotation;
		// 		var animators = miniLevel.GetComponentsInChildren<Animator>();
		// 		foreach (Animator anim in animators)
		// 		{
		// 			anim.enabled = false;
		// 		}
		// 	}
		// 	void Update()
		// 	{
		// 		miniLevel.transform.Rotate(Vector3.up * .3f);
		// 	}
	}
}