using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

namespace EcoBuilder.UI
{
    public class Trait : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler
    {
                         // from,to
        public event Action<int, int> OnUserSlid;
        public event Action<int> OnConflicted;
        public event Action<int> OnUnconflicted;

        private Slider slider;
        Sprite defaultFillSprite;
        [SerializeField] Sprite blueFillSprite;
        [SerializeField] Image fill;
        public bool Interactable {
            set { 
                slider.interactable = value;
                slider.targetGraphic.enabled = value;
                fill.sprite = value? defaultFillSprite : blueFillSprite;
            }
        }
        void Awake()
        {
            slider = GetComponent<Slider>();
            Assert.IsNotNull(slider, "slider is null");
            Assert.IsTrue(slider.wholeNumbers, "not whole numbers, but this is smelly");

            defaultFillSprite = fill.sprite;
            slider.onValueChanged.AddListener(x=> UserChangeValue());
            currentValue = Value;
        }

        int currentValue;
        public int Value {
            get {
                return (int)(slider.value);
            }
        }
        public float NormaliseValue(int unnormalised)
        {
            return (unnormalised-slider.minValue) / (slider.maxValue-slider.minValue);
        }
        public int PositivifyValue(int unnormalised)
        {
            return unnormalised - (int)slider.minValue + 1;
        }

        // if this function return false, the slider will 'snap' back
        Func<int, int?> FindConflict;
        public void AddExternalConflict(Func<int, int?> Rule)
        {
            FindConflict = Rule;
        }
        public IEnumerable<int> PossibleInitialValues {
            get {
                if (!randomiseInitialValue)
                {
                    yield return Value;
                }
                else
                {
                    for (int val=(int)slider.minValue; val<=slider.maxValue; val++)
                    {
                        yield return val;
                    }
                }
            }
        }

        // does not set randomly if initial value is fixed
        // public bool RandomiseInitialValue { get; set; } = true;
        bool randomiseInitialValue = true;
        public void FixInitialValue(int initialValue)
        {
            randomiseInitialValue = false;
            SetValueWithoutCallback(initialValue);
        }
        public void SetValueFromRandomSeed(int randomSeed)
        {
            if (randomiseInitialValue)
            {
                UnityEngine.Random.InitState(randomSeed);
                SetValueWithoutCallback(UnityEngine.Random.Range((int)slider.minValue, (int)slider.maxValue));
            }
            // otherwise leave slider alone
        }
        public void SetValueWithoutCallback(int value)
        {
            Assert.IsFalse(dragging, "should not be dragging while setting value externally");

            slider.value = value;
            currentValue = Value;

            Assert.IsTrue(slider.value == currentValue, "somehow normalisation has failed");
        }

        int? conflictIdx = null;
        int? snapBackValue = null;
        private void UserChangeValue()
        {
            if (!dragging) {
                return; 
            }
            int newValue = Value;
            if (conflictIdx != null) {
                OnUnconflicted?.Invoke((int)conflictIdx);
            }
            conflictIdx = FindConflict(newValue);
            if (conflictIdx != null)
            {
                OnConflicted?.Invoke((int)conflictIdx);
                snapBackValue = currentValue;
                slider.targetGraphic.color = fill.color = Color.red;
            }
            else
            {
                OnUserSlid?.Invoke(currentValue, newValue);
                conflictIdx = null;
                snapBackValue = null;
                currentValue = newValue;
                slider.targetGraphic.color = fill.color = Color.white;
            }
        }
        private void UndoConflict()
        {
            if (snapBackValue != null)
            {
                SetValueWithoutCallback((int)snapBackValue);
                snapBackValue = null;
            }
            if (conflictIdx != null)
            {
                OnUnconflicted?.Invoke((int)conflictIdx);
                conflictIdx = null;
                slider.targetGraphic.color = fill.color = Color.white;
            }
        }
        bool dragging;
        public void OnPointerDown(PointerEventData ped)
        {
            dragging = true;
        }
        public void OnPointerUp(PointerEventData ped)
        {
            CancelDrag();
        }
        // in case a tutorial wants to cancel, or the user does something weird
        public void CancelDrag()
        {
            if (dragging)
            {
                dragging = false;
                UndoConflict();
            }
        }
    }
}