using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.UI
{
	public class Tutorial : MonoBehaviour
	{
		[SerializeField] Help help;
		[SerializeField] Inspector inspector;
		[SerializeField] StatusBar status;
		[SerializeField] NodeLink.NodeLink nodelink;
		[SerializeField] Image pointer;

		Action bar;
		void Start()
		{
			if (bar != null)
				bar();

			help.SetText("Welcome to EcoBuilder! In this level you will build your first ecosystem. Try spinning the world around by dragging it, or add your first species by pressing the big plus at the bottom of the screen.");
			// help.Show(true);

			transform.localRotation = Quaternion.Euler(0,0,45);
			transform.localPosition = new Vector2(-20, -420);

			inspector.SetConsumerAvailability(false);
			inspector.OnIncubated += ExplainInspector;

			bar = ()=> { inspector.OnIncubated -= ExplainInspector; };
		}
		void ExplainInspector()
		{
			// TODO: don't let the user see mass or greed
			help.SetText("Here you can edit your species or choose a new name. You can then introduce it by dragging it into the world.");
			help.Show(true);
			// help.SetY(-420);

			// TODO: this should probably all be in an animator
			transform.localRotation = Quaternion.Euler(0,0,20);
			transform.localPosition = new Vector2(160, -353);

			bar();
			Action<int, GameObject> foo = (x,g)=> ExplainSpawn(g.name);
			Action fooo = ()=> Start();
			inspector.OnShaped += foo;
			nodelink.OnUnfocused += fooo;
			bar = ()=> { inspector.OnShaped -= foo; nodelink.OnUnfocused -= fooo; };
		}
		string firstSpeciesName;
		void ExplainSpawn(string speciesName)
		{
			// TODO: make these species unremovable
			inspector.SetConsumerAvailability(true);
			inspector.SetProducerAvailability(false);

			help.SetText("Your " + speciesName + " is born! Plants grow on their own, and so do not need food. Press somewhere in the background to reset and add an animal.");
			help.Show(true);
			// help.SetY(-135);
			firstSpeciesName = speciesName;
			pointer.enabled = false;

			bar();
			Action<int, GameObject> foo = (x,g)=> ExplainInteraction(g.name);
			inspector.OnShaped += foo;
			bar = ()=> inspector.OnShaped -= foo;
		}
		void ExplainInteraction(string speciesName)
		{
			inspector.SetConsumerAvailability(false);
			help.SetText("Your " + speciesName + " is hungry! Drag from it to the " + firstSpeciesName + " to give it some food.");
			help.Show(true);

			pointer.enabled = true;
			transform.localRotation = Quaternion.identity;
			transform.localPosition = new Vector2(0, -192);
			GetComponent<Animator>().SetBool("Drag", true);

			bar();
			Action<int, int> foo = (i,j)=> ExplainFinishFlag();
			nodelink.OnUserLinked += foo;
			bar = ()=> nodelink.OnUserLinked -= foo;
		}
		void ExplainFinishFlag()
		{
			help.SetText("Well done! The size of each species indicates its population size, and the flow along the links indicates the speed of eating. Press the red finish flag to complete the level!");// If all of your species can coexist, then you can finish the level by pressing the big red finish flag in the top-right corner.");
			GetComponent<Animator>().SetBool("Drag", false);
			StartCoroutine(WaitThenShow());

			var rt = GetComponent<RectTransform>();
			rt.anchorMax = rt.anchorMin = new Vector2(1,1);
			rt.anchoredPosition = new Vector2(-90,-50);
			transform.localRotation = Quaternion.Euler(0,0,-45);

			bar();
			status.OnLevelCompleted += SetFinishMessage;
		}
		IEnumerator WaitThenShow()
		{
			yield return new WaitForSeconds(2);
			help.Show(true);
		}
		void SetFinishMessage()
		{
			help.SetText("Congratulations! You have built your first ecosystem. Try the remaining levels if you would like a challenge!");
			gameObject.SetActive(false);
		}
	}
}