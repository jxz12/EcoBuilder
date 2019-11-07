using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

namespace EcoBuilder.UI
{
    public class Constraints : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler
    {
        public event Action OnErrorShown;

        class Constraint
        {
            public Text counter;
            public Image icon;
            public int threshold=-1;
            public int value=0;
        }
        Dictionary<string, Constraint> constraints;
        string prefix = "     ";

        void Awake()
        {
            constraints = new Dictionary<string, Constraint>();
            foreach (Text t in transform.GetComponentsInChildren<Text>())
            {
                constraints[t.name] = new Constraint
                {
                    counter=t,
                    icon=t.transform.GetComponentInChildren<Image>(),
                };
            }
        }

        public bool IsSatisfied(string name)
        {
            return constraints[name].value >= constraints[name].threshold;
        }
        public void Show(bool visible)
        {
            GetComponent<Animator>().SetBool("Visible", visible);
        }
        public void Unconstrain(string name)
        {
            constraints[name].threshold = -1;
            constraints[name].counter.gameObject.SetActive(true);
            constraints[name].counter.text = prefix + "0";
        }
        public void Constrain(string name, int threshold)
        {
            if (threshold <= 0)
            {
                constraints[name].counter.gameObject.SetActive(false);
            }
            else
            {
                constraints[name].threshold = threshold;
                constraints[name].counter.text = prefix + "0/" + threshold;
            }
        }
        public int GetThreshold(string name)
        {
            return constraints[name].threshold;
        }
        public void Display(string name, int value)
        {
            constraints[name].value = value;
            if (constraints[name].threshold <= 0)
            {
                constraints[name].counter.text = prefix + value;
            }
            else
            {
                constraints[name].counter.text = 
                constraints[name].counter.text = prefix + value + "/" + constraints[name].threshold;
                constraints[name].icon.color = value >= constraints[name].threshold? Color.green : Color.white;
            }
            // CurrentValue = value;
            // if (ConstraintLimit <= 0)
            // {
            //     number.text = prefix + value.ToString();
            //     IsSatisfied = true;
            // }
            // else
            // {
            //     number.text = prefix + CurrentValue + "/" + ConstraintLimit;
            //     IsSatisfied = (CurrentValue >= ConstraintLimit);
            //     icon.color = IsSatisfied? Color.green : Color.white;
            // }
        }
        [SerializeField] Tooltip tooltip;
        public void OnPointerEnter(PointerEventData ped)
        {
            tooltip.Enable();
            tooltip.ShowText(Error());
            StartCoroutine(FollowCursor());
            OnErrorShown?.Invoke();
        }
        IEnumerator FollowCursor()
        {
            while (true)
            {
                tooltip.SetPos(Input.mousePosition);
                yield return null;
            }
        }
        public void OnPointerExit(PointerEventData ped)
        {
            tooltip.Disable();
            StopCoroutine(FollowCursor());
        }
        public bool Feasible { get; set; }
        public bool Stable { get; set; }
        public bool Disjoint { get; set; }
        string Error()
        {
            if (constraints["Paw"].value==0 && constraints["Leaf"].value==0)
                return "Your ecosystem is empty.";
            if (!IsSatisfied("Leaf"))
                return "You have not added enough plants.";
            if (!IsSatisfied("Paw"))
                return "You have not added enough animals.";
            if (!Feasible)
                return "At least one species is going extinct.";
            if (Disjoint)
                return "Your network is not connected.";
            // if (!stable)
            //     return "Your ecosystem is not stable.";
            if (!IsSatisfied("Count"))
                return "You have not added enough links.";
            if (!IsSatisfied("Chain"))
                return "Your web is not tall enough.";
            if (!IsSatisfied("Loop"))
                return "You do not have a long enough loop.";
            else
                return "Your ecosystem has no errors!";
        }
    }
}