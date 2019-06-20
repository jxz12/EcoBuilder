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
			number.text = limit.ToString();
		}
		public void Display(int value)
		{
			currentValue = value;
			number.text = value.ToString();
			IsSatisfied = (currentValue >= constraintLimit);
		}
		public bool IsSatisfied { get; private set; }
	}
}