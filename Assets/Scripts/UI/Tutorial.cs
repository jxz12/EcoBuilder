using UnityEngine;

namespace EcoBuilder.UI
{
	public class Tutorial : MonoBehaviour
	{
		[SerializeField] Help help;
		[SerializeField] Inspector inspector;

		void Start()
		{
			inspector.OnIncubated += ()=> help.SetText("good!");
			inspector.OnIncubated += ()=> help.Show(true);
		}
	}
}