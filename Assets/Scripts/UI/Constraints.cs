using UnityEngine;
using UnityEngine.Assertions;
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

        public event Action OnChainHovered, OnChainUnhovered;
        public event Action OnLoopHovered, OnLoopUnhovered;

        private class Constraint
        {
            public TMPro.TextMeshProUGUI counter;
            public Image icon;
            public int threshold=-1;
            public int value=0;
            public Constraint(TMPro.TextMeshProUGUI text)
            {
                counter = text;
                icon = text.transform.GetComponentInChildren<Image>();
            }
        }

        private enum Type { Leaf, Paw, Edge, Chain, Loop}
        Dictionary<Type, Constraint> constraintMap = new Dictionary<Type, Constraint>();

        [SerializeField] TMPro.TextMeshProUGUI leaf, paw, edge, chain, loop;

        string prefix = "     ";
        void Awake()
        {
            constraintMap[Type.Leaf] = new Constraint(leaf);
            constraintMap[Type.Paw] = new Constraint(paw);
            constraintMap[Type.Edge] = new Constraint(edge);
            constraintMap[Type.Chain] = new Constraint(chain);
            constraintMap[Type.Loop] = new Constraint(loop);

            // prevents scene getting dirty, Unity autolayout sucks
            GetComponent<VerticalLayoutGroup>().enabled = true;
            GetComponent<ContentSizeFitter>().enabled = true;
        }

        private void Constrain(Type type, int threshold)
        {
            if (threshold < 0) // hide
            {
                constraintMap[type].counter.gameObject.SetActive(false);
            }
            else if (threshold == 0) // display but do not track
            {
                constraintMap[type].counter.text = prefix + constraintMap[type].value.ToString();
            }
            else
            {
                constraintMap[type].counter.text = prefix + constraintMap[type].value.ToString() + "/" + threshold;
            }
            constraintMap[type].threshold = threshold;
        }
        private void Display(Type type, int value)
        {
            constraintMap[type].value = value;
            if (constraintMap[type].threshold <= 0)
            {
                constraintMap[type].counter.text = prefix + value;
            }
            else
            {
                constraintMap[type].counter.text = 
                constraintMap[type].counter.text = prefix + value + "/" + constraintMap[type].threshold;
                constraintMap[type].icon.color = value >= constraintMap[type].threshold? Color.green : Color.white;
            }
        }
        private bool IsSatisfied(Type type)
        {
            return constraintMap[type].value >= constraintMap[type].threshold;
        }
        private int GetThreshold(Type type)
        {
            return constraintMap[type].threshold;
        }

        public void ConstrainLeaf(int threshold) {
            Constrain(Type.Leaf, threshold);
        }
        public void ConstrainPaw(int threshold) {
            Constrain(Type.Paw, threshold);
        }
        public void ConstrainEdge(int threshold) {
            Constrain(Type.Edge, threshold);
        }
        public void ConstrainChain(int threshold) {
            Constrain(Type.Chain, threshold);
        }
        public void ConstrainLoop(int threshold) {
            Constrain(Type.Loop, threshold);
        }
        public void DisplayEdge(int numEdges) {
            Display(Type.Edge, numEdges);
        }
        public void DisplayChain(int lenChain) {
            Display(Type.Chain, lenChain);
        }
        public void DisplayLoop(int lenLoop) {
            Display(Type.Loop, lenLoop);
        }

        public void HighlightPaw(bool highlighted=true)
        {
            print("TODO: hightlight paw");
        }
        public void HighlightChain(bool highlighted=true)
        {
            print("TODO: highlight chain");
        }
        public void HighlightLoop(bool highlighted=true)
        {
            print("TODO: highlight loop");
        }

        public void UpdateDisjoint(bool disjoint)
        {
            Disjoint = disjoint;
        }
        public void UpdateFeasibility(bool isFeasible)
        {
            Feasible = isFeasible;
        }
        public void UpdateStability(bool isStable)
        {
            Stable = isStable;
        }

        // paws and leaves need to be different
        HashSet<int> producers = new HashSet<int>();
        HashSet<int> consumers = new HashSet<int>();
        public void AddIdx(int idx, bool isProducer)
        {
            Assert.IsFalse(producers.Contains(idx) || consumers.Contains(idx), $"idx {idx} already added");
            if (isProducer)
            {
                producers.Add(idx);

                Display(Type.Leaf, producers.Count);
                if (GetThreshold(Type.Leaf) > 0 && IsSatisfied(Type.Leaf)) {
                    OnProducersAvailable.Invoke(false);
                }
            }
            else
            {
                consumers.Add(idx);

                Display(Type.Paw, consumers.Count);
                if (GetThreshold(Type.Paw) > 0 && IsSatisfied(Type.Paw)) {
                    OnConsumersAvailable.Invoke(false);
                }
            }
        }
        public void RemoveIdx(int idx)
        {
            if (producers.Contains(idx))
            {
                producers.Remove(idx);

                if (GetThreshold(Type.Leaf) > 0 && IsSatisfied(Type.Leaf)) {
                    OnProducersAvailable.Invoke(true);
                }
                Display(Type.Leaf, producers.Count);
            }
            else
            {
                consumers.Remove(idx);

                if (GetThreshold(Type.Paw) > 0 && IsSatisfied(Type.Paw)) {
                    OnConsumersAvailable.Invoke(true);
                }
                Display(Type.Paw, consumers.Count);
            }
        }
        public int PawValue { get { return constraintMap[Type.Paw].value; } }
        public int LeafValue { get { return constraintMap[Type.Leaf].value; } }

        public bool AllSatisfied()
        {
            return !Disjoint &&
                   Feasible &&
                   IsSatisfied(Type.Leaf) &&
                   IsSatisfied(Type.Paw) &&
                   IsSatisfied(Type.Edge) &&
                   IsSatisfied(Type.Chain) &&
                   IsSatisfied(Type.Loop);
        }
        public void Hide(bool hidden=true)
        {
            gameObject.SetActive(!hidden);
        }
        public void Finish()
        {
            GetComponent<Animator>().SetBool("Visible", false);
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

                bool overChain = RectTransformUtility.RectangleContainsScreenPoint(constraintMap[Type.Chain].counter.rectTransform, Input.mousePosition);
                bool overLoop = RectTransformUtility.RectangleContainsScreenPoint(constraintMap[Type.Loop].counter.rectTransform, Input.mousePosition);

                Assert.IsFalse(overChain && overLoop, "cannot highlight both chain and loop");

                // always unhighlight first to clear highlighting
                if (chainHovered && !overChain)
                {
                    OnChainUnhovered?.Invoke();
                    chainHovered = false;
                }
                if (loopHovered && !overLoop)
                {
                    OnLoopUnhovered?.Invoke();
                    loopHovered = false;
                }

                if (!chainHovered && overChain)
                {
                    OnChainHovered?.Invoke();
                    chainHovered = true;
                }
                if (!loopHovered && overLoop)
                {
                    OnLoopHovered?.Invoke();
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
                OnChainUnhovered?.Invoke();
                chainHovered = false;
            }
            if (loopHovered)
            {
                OnLoopUnhovered?.Invoke();
                loopHovered = false;
            }
            OnErrorShown?.Invoke(false);
        }
        public bool Feasible { get; private set; }
        public bool Stable { get; private set; }
        public bool Disjoint { get; private set; }
        string Error()
        {
            if (constraintMap[Type.Paw].value==0 && constraintMap[Type.Leaf].value==0) {
                return "Your ecosystem is empty.";
            } else if (!IsSatisfied(Type.Leaf)) {
                return "You have not added enough plants.";
            } else if (!IsSatisfied(Type.Paw)) {
                return "You have not added enough animals.";
            } else if (!Feasible) {
                return "At least one species is going extinct.";
            } else if (Disjoint) {
                return "Your network is not connected.";
            // } else if (!stable) {
            //     return "Your ecosystem is not stable.";
            } else if (!IsSatisfied(Type.Edge)) {
                return "You have not added enough links.";
            } else if (!IsSatisfied(Type.Chain)) {
                return "Your web is not tall enough.";
            } else if (!IsSatisfied(Type.Loop)) {
                return "You do not have a long enough loop.";
            } else {
                return "";//"Your ecosystem has no errors!";
            }
        }
    }
}