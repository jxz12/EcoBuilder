using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace EcoBuilder.UI
{
    public class Trait : MonoBehaviour, IPointerUpHandler
    {
        public event Action<float, float> OnUserSlid; // from, to
        public event Action<int> OnConflicted;
        public event Action<int> OnUnconflicted;
        public bool Interactable { set { slider.interactable = value; } }
        public bool Active { set { gameObject.SetActive(value); } }

        float currentValue = -1;
        UnityAction<float> ValueChangedCallback;
        Slider slider;
        void Start()
        {
            slider = GetComponent<Slider>();
            ValueChangedCallback = x=> OnValueChanged();
            slider.onValueChanged.AddListener(ValueChangedCallback);
        }
        public float SetValueFromRandomSeed(int randomSeed)
        {
            SetValueWithoutCallback(UnityEngine.Random.Range(0, 1f));
            return slider.normalizedValue;
        }
        public IEnumerable<float> PossibleValues {
            get {
                if (!slider.wholeNumbers)
                    throw new Exception("not whole numbers");

                float range = slider.maxValue - slider.minValue;
                for (float val=slider.minValue; val<=slider.maxValue; val+=1)
                {
                    yield return (val-slider.minValue) / range;
                }
            }
        }
        public void SetValueWithoutCallback(float normalizedValue)
        {
            // this is ugly as heck, but sliders are stupid
            slider.onValueChanged.RemoveListener(ValueChangedCallback);
            slider.normalizedValue = normalizedValue;
            slider.onValueChanged.AddListener(ValueChangedCallback);
            currentValue = normalizedValue;
        }
        int conflict = -1;
        float toSnapBack = -1;
        private void OnValueChanged()
        {
            float newValue = slider.normalizedValue;
            if (conflict >= 0)
            {
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
        public void OnPointerUp(PointerEventData ped)
        {
            if (conflict >= 0)
            {
                OnUnconflicted?.Invoke(conflict);
                SetValueWithoutCallback(toSnapBack);
                conflict = -1;
                toSnapBack = -1;
                slider.targetGraphic.color = Color.white;
            }
        }

        // if this function return false, the slider will 'snap' back
        Func<float, int> FindConflict;
        public void AddExternalConflict(Func<float, int> Rule)
        {
            FindConflict = Rule;
        }
    }
}