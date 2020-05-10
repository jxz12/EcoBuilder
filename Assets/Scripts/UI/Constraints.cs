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
        public event Action<bool> OnLeafFilled;
        public event Action<bool> OnPawFilled;
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
            public bool Active {
                get { return counter.gameObject.activeSelf; }
                set { counter.gameObject.SetActive(value); }
            }
        }

        private enum Type { Leaf, Paw, Edge, Chain, Loop}
        Dictionary<Type, Constraint> constraintMap = new Dictionary<Type, Constraint>();

        [SerializeField] TMPro.TextMeshProUGUI leaf, paw, edge, chain, loop, error;
        [SerializeField] Image divider;

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

            OnChainHovered += ()=> HoverChain(true);
            OnChainUnhovered += ()=> HoverChain(false);
            OnLoopHovered += ()=> HoverLoop(true);
            OnLoopUnhovered += ()=> HoverLoop(false);
        }
        void Start()
        {
            Hide(false);
        }

        private void Constrain(Type type, int threshold)
        {
            var constraint = constraintMap[type];
            
            if (threshold < 0) // hide
            {
                constraint.Active = false;
            }
            else if (threshold == 0) // display but do not track
            {
                constraint.Active = true;
                constraint.counter.text = prefix + constraint.value.ToString();
                constraint.icon.color = Color.white;
            }
            else
            {
                constraint.Active = true;
                constraint.counter.text = prefix + constraint.value.ToString() + "/" + threshold;
            }
            constraintMap[type].threshold = threshold;
            Display(type, constraintMap[type].value); // make sure colour matches

            // only add the divider if there's something beneath it
            divider.gameObject.SetActive(constraintMap[Type.Edge].Active || constraintMap[Type.Chain].Active || constraintMap[Type.Loop].Active);
        }
        private void Display(Type type, int value)
        {
            var constraint = constraintMap[type];

            constraint.value = value;
            if (constraint.threshold <= 0)
            {
                constraint.counter.text = prefix + value;
                constraint.icon.color = Color.white;
            }
            else
            {
                constraint.counter.text = prefix + value + "/" + constraintMap[type].threshold;
                constraint.icon.color = IsSatisfied(type)? (type==Type.Leaf||type==Type.Paw? new Color(1,1,1,.5f) : Color.green) : Color.white;
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

        [SerializeField] GameObject highlighterPrefab;
        void HighlightOnTransform(RectTransform highlighted)
        {
            Assert.IsNotNull(highlighterPrefab.GetComponent<LayoutElement>());
            Assert.IsNotNull(highlighterPrefab.GetComponent<Image>());
            var highlighter = Instantiate(highlighterPrefab, transform);
            highlighter.transform.SetAsFirstSibling(); // always render below constraints

            IEnumerator Flash(float period, float padding)
            {
                float startTime = Time.time;
                var im = highlighter.GetComponent<Image>();
                var rt = highlighter.GetComponent<RectTransform>();
                while (true)
                {
                    float t = (Time.time-startTime) / period;
                    im.color = new Color(1,1,1, .175f+.075f*Mathf.Sin(2*Mathf.PI*t));
                    rt.anchoredPosition = new Vector2(highlighted.anchoredPosition.x-padding/2, highlighted.anchoredPosition.y+padding/2);
                    rt.sizeDelta = new Vector2(highlighted.sizeDelta.x+padding, highlighted.sizeDelta.y+padding);
                    yield return null;
                }
            }
            StartCoroutine(Flash(2f, 6f));
        }
        public void HighlightPaw(bool highlighted=true)
        {
            HighlightOnTransform(constraintMap[Type.Paw].counter.rectTransform);
        }
        public void HighlightChain(bool highlighted=true)
        {
            HighlightOnTransform(constraintMap[Type.Chain].counter.rectTransform);
        }
        public void HighlightLoop(bool highlighted=true)
        {
            HighlightOnTransform(constraintMap[Type.Loop].counter.rectTransform);
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

        // this class really should not throw these events, but such is life
        public void LimitLeaf(int threshold)
        {
            bool prevSatisfied = IsSatisfied(Type.Leaf);
            Constrain(Type.Leaf, threshold);
            bool nowSatisfied = IsSatisfied(Type.Leaf);

            if (!prevSatisfied && nowSatisfied) {
                OnLeafFilled?.Invoke(false);
            } else if (prevSatisfied && !nowSatisfied) {
                OnLeafFilled?.Invoke(true);
            }
        }
        public void LimitPaw(int threshold)
        {
            bool prevSatisfied = IsSatisfied(Type.Paw);
            Constrain(Type.Paw, threshold);
            bool nowSatisfied = IsSatisfied(Type.Paw);

            if (!prevSatisfied && nowSatisfied) {
                OnPawFilled?.Invoke(false);
            } else if (prevSatisfied && !nowSatisfied) {
                OnPawFilled?.Invoke(true);
            }
        }
        // paws and leaves need to be different
        HashSet<int> leaves = new HashSet<int>();
        HashSet<int> paws = new HashSet<int>();
        public void AddLeafIdx(int idx)
        {
            Assert.IsFalse(leaves.Contains(idx), $"leaf idx {idx} already added");
            leaves.Add(idx);

            Display(Type.Leaf, leaves.Count);
            if (GetThreshold(Type.Leaf) > 0 && IsSatisfied(Type.Leaf)) {
                OnLeafFilled?.Invoke(false);
            }
        }
        public void AddPawIdx(int idx)
        {
            Assert.IsFalse(paws.Contains(idx), $"paw idx {idx} already added");
            paws.Add(idx);

            Display(Type.Paw, paws.Count);
            if (GetThreshold(Type.Paw) > 0 && IsSatisfied(Type.Paw)) {
                OnPawFilled?.Invoke(false);
            }
        }
        public void RemoveIdx(int idx)
        {
            if (leaves.Contains(idx))
            {
                leaves.Remove(idx);

                if (GetThreshold(Type.Leaf) > 0 && IsSatisfied(Type.Leaf)) {
                    OnLeafFilled?.Invoke(true);
                }
                Display(Type.Leaf, leaves.Count);
            }
            else
            {
                paws.Remove(idx);

                if (GetThreshold(Type.Paw) > 0 && IsSatisfied(Type.Paw)) {
                    OnPawFilled?.Invoke(true);
                }
                Display(Type.Paw, paws.Count);
            }
        }
        public int PawValue { get { return constraintMap[Type.Paw].value; } }
        public int LeafValue { get { return constraintMap[Type.Leaf].value; } }

        public bool AllSatisfied()
        {
            return !Disjoint &&
                   Feasible &&
                //    IsSatisfied(Type.Leaf) &&
                //    IsSatisfied(Type.Paw) &&
                   IsSatisfied(Type.Edge) &&
                   IsSatisfied(Type.Chain) &&
                   IsSatisfied(Type.Loop);
        }
        public void Hide(bool hidden=true)
        {
            GetComponent<Canvas>().enabled = !hidden;
            if (!hidden) {
                StartCoroutine(Tweens.Pivot(GetComponent<RectTransform>(), new Vector2(1,1), new Vector2(0,1)));
            }
        }
        public void Finish()
        {
            StopAllCoroutines();
            StartCoroutine(Tweens.Pivot(GetComponent<RectTransform>(), new Vector2(0,1), new Vector2(1,1)));
        }

        public void OnPointerEnter(PointerEventData ped)
        {
            ShowError(.5f, true);
            OnErrorShown?.Invoke(true);
        }
        private IEnumerator growCoroutine, followCoroutine;
        void ShowError(float duration, bool showing)
        {
            IEnumerator Follow()
            {
                var errorRT = error.GetComponent<RectTransform>();
                while (true)
                {
                    error.text = GetErrorMessage();
                    errorRT.position = Input.mousePosition;

                    bool overChain = RectTransformUtility.RectangleContainsScreenPoint(constraintMap[Type.Chain].counter.rectTransform, Input.mousePosition);
                    bool overLoop = RectTransformUtility.RectangleContainsScreenPoint(constraintMap[Type.Loop].counter.rectTransform, Input.mousePosition);

                    Assert.IsFalse(overChain && overLoop, "cannot hover both chain and loop");

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
            IEnumerator Grow()
            {
                if (showing)
                {
                    if (followCoroutine != null) {
                        StopCoroutine(followCoroutine);
                    }
                    StartCoroutine(followCoroutine = Follow());
                }
                error.enabled = true;
                float tStart = Time.time;
                float scaleStart = error.transform.localScale.x;
                float targetScale = showing? 1:0;
                while (Time.time < tStart+duration)
                {
                    float t = Tweens.CubicOut((Time.time-tStart) / duration);
                    float scale = Mathf.Lerp(scaleStart, targetScale, t);
                    error.transform.localScale = new Vector3(scale, scale, 1);
                    yield return null;
                }
                error.transform.localScale = new Vector3(targetScale, targetScale, 1);
                if (!showing)
                {
                    error.enabled = false;
                    StopCoroutine(followCoroutine);
                    followCoroutine = null;
                }
            }
            if (growCoroutine != null)
            {
                StopCoroutine(growCoroutine);
                growCoroutine = null;
            }
            StartCoroutine(growCoroutine = Grow());
        }
        
        bool chainHovered=false, loopHovered=false;
        public void OnPointerExit(PointerEventData ped)
        {
            ShowError(.5f, false);
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
        void HoverChain(bool hovered)
        {
            if (hovered) {
                constraintMap[Type.Chain].icon.color = Color.red;
            } else {
                Display(Type.Chain, constraintMap[Type.Chain].value);
            }
        }
        void HoverLoop(bool hovered)
        {
            if (hovered) {
                constraintMap[Type.Loop].icon.color = Color.red;
            } else {
                Display(Type.Loop, constraintMap[Type.Loop].value);
            }
        }
        
        public bool Feasible { get; private set; }
        public bool Stable { get; private set; }
        public bool Disjoint { get; private set; }
        string GetErrorMessage()
        {
            if (constraintMap[Type.Paw].value==0 && constraintMap[Type.Leaf].value==0) {
                return "You have not added any species yet.";
            // } else if (!IsSatisfied(Type.Leaf)) {
            //     return "You have not added enough plants.";
            // } else if (!IsSatisfied(Type.Paw)) {
            //     return "You have not added enough animals.";
            } else if (!Feasible) {
                return "At least one species is going extinct.";
            } else if (Disjoint) {
                return "You have more than one separate ecosystem.";
            // } else if (!stable) {
            //     return "Your ecosystem is not stable.";
            } else if (!IsSatisfied(Type.Edge)) {
                return "You have not added enough links.";
            } else if (!IsSatisfied(Type.Chain)) {
                return "You do not have a long enough chain.";
            } else if (!IsSatisfied(Type.Loop)) {
                return "You do not have a long enough loop.";
            } else {
                Assert.IsTrue(AllSatisfied(), "no error but level not passed");
                return "You have passed the level!";
            }
        }
    }
}