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
        string prefix = "     ";
        public void Unconstrain()
        {
            ConstraintLimit = -1;
            gameObject.SetActive(true);
            IsSatisfied = true;
            number.text = prefix + "0";
        }
        public void Constrain(int limit)
        {
            if (limit <= 0)
                gameObject.SetActive(false);

            ConstraintLimit = limit;
            number.text = prefix + "0/" + limit;
        }
        public bool IsSatisfied { get; private set; }
        public void Display(int value)
        {
            CurrentValue = value;
            if (ConstraintLimit <= 0)
            {
                number.text = prefix + value.ToString();
                IsSatisfied = true;
            }
            else
            {
                number.text = prefix + CurrentValue + "/" + ConstraintLimit;
                IsSatisfied = (CurrentValue >= ConstraintLimit);
                icon.color = IsSatisfied? Color.green : Color.white;
            }
        }
        public void Display(bool satisfied)
        {
			IsSatisfied = satisfied;
            icon.color = IsSatisfied? Color.green : Color.white;
        }
    }
}