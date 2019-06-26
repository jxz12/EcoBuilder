using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.UI
{
	public class Constraint : MonoBehaviour
	{
		[SerializeField] Text number;
		[SerializeField] Image icon;
		public int ConstraintLimit { get; private set; }
		public int CurrentValue { get; private set; }
		public void Constrain(int limit)
		{
			ConstraintLimit = limit;
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
			CurrentValue = value;
			number.text = CurrentValue + "/" + ConstraintLimit;
			IsSatisfied = (CurrentValue >= ConstraintLimit);

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