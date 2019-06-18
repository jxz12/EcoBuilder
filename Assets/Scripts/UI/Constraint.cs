using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.UI
{
	public class Constraint : MonoBehaviour
	{
		[SerializeField] Text numberDisplay;
		int constraintLimit, currentValue;
		public void Constrain(int limit)
		{
			constraintLimit = limit;
			numberDisplay.text = limit.ToString();
		}
		public void Display(int value)
		{
			currentValue = value;
			numberDisplay.text = value.ToString();
			IsSatisfied = (currentValue >= constraintLimit);
		}
		public bool IsSatisfied { get; private set; }
		public bool CheckSatisfied()
		{
			return true;
		}

	}
}