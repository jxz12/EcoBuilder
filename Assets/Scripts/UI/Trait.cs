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
                // slider.targetGraphic.color = value? Color.white : Color.blue;
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
            currentValue = UnnormalisedValue;
        }

        int currentValue;
        public int UnnormalisedValue {
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
        Func<int, int> FindConflict;
        public void AddExternalConflict(Func<int, int> Rule)
        {
            FindConflict = Rule;
        }
        public IEnumerable<int> PossibleInitialValues {
            get {
                if (!RandomiseInitialValue)
                {
                    yield return UnnormalisedValue;
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
        public bool RandomiseInitialValue { get; set; } = true;
        public void SetValueFromRandomSeed(int randomSeed)
        {
            if (RandomiseInitialValue)
            {
                UnityEngine.Random.InitState(randomSeed);
                SetValueWithoutCallback(UnityEngine.Random.Range((int)slider.minValue, (int)slider.maxValue));
            }
            // otherwise leave slider alone
        }
        public void SetValueWithoutCallback(int unnormalisedValue)
        {
            Assert.IsFalse(dragging, "should not be dragging while setting value externally");

            slider.value = unnormalisedValue;
            currentValue = UnnormalisedValue;

            Assert.IsTrue(slider.value == currentValue, "somehow normalisation has failed");
        }

        int conflict = -1;
        int toSnapBack = -1;
        private void UserChangeValue()
        {
            if (!dragging) {
                return; 
            }
            int newValue = UnnormalisedValue;
            if (conflict >= 0) {
                OnUnconflicted?.Invoke(conflict);
            }
            conflict = FindConflict(newValue);
            if (conflict >= 0)
            {
                OnConflicted?.Invoke(conflict);
                toSnapBack = currentValue;
                slider.targetGraphic.color = Color.red;
            }
            else
            {
                OnUserSlid?.Invoke(currentValue, newValue);
                conflict = -1;
                toSnapBack = -1;
                currentValue = newValue;
                slider.targetGraphic.color = Color.white;
            }
        }
        private void UndoConflict()
        {
            if (toSnapBack >= 0)
            {
                SetValueWithoutCallback(toSnapBack);
                toSnapBack = -1;
            }
            if (conflict >= 0)
            {
                OnUnconflicted?.Invoke(conflict);
                conflict = -1;
                slider.targetGraphic.color = Color.white;
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