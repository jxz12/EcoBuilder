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
        public event Action<bool> OnProducersAvailable;
        public event Action<bool> OnConsumersAvailable;
        public event Action<bool> OnErrorShown;
        public event Action<bool> OnChainHovered;
        public event Action<bool> OnLoopHovered;

        class Constraint
        {
            public TMPro.TextMeshProUGUI counter;
            public Image icon;
            public int threshold=-1;
            public int value=0;
        }
        Dictionary<string, Constraint> constraints;
        string prefix = "     ";

        void Awake()
        {
            constraints = new Dictionary<string, Constraint>();
            foreach (TMPro.TextMeshProUGUI t in transform.GetComponentsInChildren<TMPro.TextMeshProUGUI>())
            {
                constraints[t.name] = new Constraint
                {
                    counter=t,
                    icon=t.transform.GetComponentInChildren<Image>(),
                };
            }
            // prevents scene getting dirty, Unity autolayout sucks
            GetComponent<VerticalLayoutGroup>().enabled = true;
            GetComponent<ContentSizeFitter>().enabled = true;
        }

        HashSet<int> producers = new HashSet<int>();
        HashSet<int> consumers = new HashSet<int>();
        public void AddType(int idx, bool isProducer)
        {
            if (isProducer)
            {
                // this will have to be changed if we want to let species switch type
                producers.Add(idx);

                Display("Leaf", producers.Count);
                if (GetThreshold("Leaf") > 0 && IsSatisfied("Leaf"))
                    OnProducersAvailable.Invoke(false);
            }
            else
            {
                consumers.Add(idx);

                Display("Paw", consumers.Count);
                if (GetThreshold("Paw") > 0 && IsSatisfied("Paw"))
                    OnConsumersAvailable.Invoke(false);
            }
        }
        public void RemoveIdx(int idx)
        {
            if (producers.Contains(idx))
            {
                producers.Remove(idx);

                if (GetThreshold("Leaf") > 0 && IsSatisfied("Leaf"))
                    OnProducersAvailable.Invoke(true);
                Display("Leaf", producers.Count);
            }
            else
            {
                consumers.Remove(idx);

                if (GetThreshold("Paw") > 0 && IsSatisfied("Paw"))
                    OnConsumersAvailable.Invoke(true);
                Display("Paw", consumers.Count);

            }
        }
        public void DisplayDisjoint(bool isDisjoint)
        {
            Disjoint = isDisjoint;
        }
        public void DisplayNumEdges(int numEdges)
        {
            Display("Count", numEdges);
        }
        public void DisplayMaxChain(int lenChain)
        {
            Display("Chain", lenChain);
        }
        public void DisplayMaxLoop(int lenLoop)
        {
            Display("Loop", lenLoop);
        }
        public void DisplayFeasibility(bool isFeasible)
        {
            Feasible = isFeasible;
        }
        public void DisplayStability(bool isStable)
        {
			Stable = isStable;
        }

        public bool IsSatisfied(string name)
        {
            return constraints[name].value >= constraints[name].threshold;
        }
        public void Show(bool visible)
        {
            GetComponent<Animator>().SetBool("Visible", visible);
        }
        public void Constrain(string name, int threshold)
        {
            if (threshold < 0) // hide
            {
                constraints[name].counter.gameObject.SetActive(false);
            }
            else if (threshold == 0) // display but do not track
            {
                constraints[name].counter.text = prefix + "0";
            }
            else
            {
                constraints[name].counter.text = prefix + "0/" + threshold;
            }
            constraints[name].threshold = threshold;
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
        }
        public int GetThreshold(string name)
        {
            return constraints[name].threshold;
        }
        public int GetValue(string name)
        {
            return constraints[name].value;
        }
        [SerializeField] Tooltip tooltip;
        public void OnPointerEnter(PointerEventData ped)
        {
            tooltip.Enable();
            tooltip.ShowText(Error());
            followCoroutine = FollowCursor();
            StartCoroutine(followCoroutine);
            OnErrorShown?.Invoke(true);
        }
        
        private IEnumerator followCoroutine;
        bool chainHovered=false, loopHovered=false;
        IEnumerator FollowCursor()
        {
            while (true)
            {
                tooltip.SetPos(Input.mousePosition);

                bool overChain = RectTransformUtility.RectangleContainsScreenPoint(constraints["Chain"].counter.rectTransform, Input.mousePosition);
                bool overLoop = RectTransformUtility.RectangleContainsScreenPoint(constraints["Loop"].counter.rectTransform, Input.mousePosition);
                if (overChain && overLoop)
                    throw new Exception("cannot highlight both chains and loops");

                // always unhighlight first to clear highlighting
                if (chainHovered && !overChain)
                {
                    OnChainHovered?.Invoke(false);
                    chainHovered = false;
                }
                if (loopHovered && !overLoop)
                {
                    OnLoopHovered?.Invoke(false);
                    loopHovered = false;
                }

                if (!chainHovered && overChain)
                {
                    OnChainHovered?.Invoke(true);
                    chainHovered = true;
                }
                if (!loopHovered && overLoop)
                {
                    OnLoopHovered?.Invoke(true);
                    loopHovered = true;
                }

                yield return null;
            }
        }
        public void OnPointerExit(PointerEventData ped)
        {
            tooltip.Disable();
            StopCoroutine(followCoroutine);
            if (chainHovered)
            {
                OnChainHovered?.Invoke(false);
                chainHovered = false;
            }
            if (loopHovered)
            {
                OnLoopHovered?.Invoke(false);
                loopHovered = false;
            }
            OnErrorShown?.Invoke(false);
        }
        public bool Feasible { get; private set; }
        public bool Stable { get; private set; }
        public bool Disjoint { get; private set; }
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