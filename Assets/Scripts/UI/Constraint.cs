using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.UI
{
	public class Constraint : MonoBehaviour
	{
		[SerializeField] Text number;
		[SerializeField] Image icon;
		int constraintLimit, currentValue;
		public void Constrain(int limit)
		{
			constraintLimit = limit;
			if (limit < 0)
			{
				gameObject.SetActive(false);
			}
			else
			{
				number.text = "0/" + limit;
			}
		}
		public void Display(int value)
		{
			currentValue = value;
			number.text = currentValue + "/" + constraintLimit;
			IsSatisfied = (currentValue >= constraintLimit);

			if (IsSatisfied)
			{
				icon.color = Color.green;
			}
			else
			{
				icon.color = Color.white;
			}
		}
		public bool IsSatisfied { get; private set; }
	}
}