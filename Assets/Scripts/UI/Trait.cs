﻿using System;
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
        public event Action<float, float> OnUserSlid; // from, to
        public event Action<int> OnConflicted;
        public event Action<int> OnUnconflicted;
        public bool Interactable {
            set { 
                slider.interactable = value;
                slider.targetGraphic.color = value? Color.white : Color.blue;
            }
        }

        float currentValue;
        private Slider slider;
        void Awake()
        {
            slider = GetComponent<Slider>();
            slider.onValueChanged.AddListener(x=> UserChangeValue());
            currentValue  = slider.normalizedValue;
        }

        // if this function return false, the slider will 'snap' back
        Func<float, int> FindConflict;
        public void AddExternalConflict(Func<float, int> Rule)
        {
            FindConflict = Rule;
        }
        public IEnumerable<float> PossibleInitialValues {
            get {
                Assert.IsTrue(slider.wholeNumbers, "not whole numbers");

                if (!RandomiseInitialValue)
                {
                    yield return slider.normalizedValue;
                }
                else
                {
                    float range = slider.maxValue - slider.minValue;
                    for (float val=slider.minValue; val<=slider.maxValue; val+=1)
                    {
                        yield return (val-slider.minValue) / range;
                    }
                }
            }
        }

        // does not set randomly if initial value is fixed
        public bool RandomiseInitialValue { get; set; } = true;
        public float SetValueFromRandomSeed(int randomSeed)
        {
            if (!RandomiseInitialValue)
            {
                return slider.normalizedValue;
            }
            else
            {
                UnityEngine.Random.InitState(randomSeed);
                return SetValueWithoutCallback(UnityEngine.Random.Range(0, 1f));
            }
        }
        public float SetValueWithoutCallback(float normalizedValue)
        {
            Assert.IsFalse(dragging, "should not be dragging while setting value externally");

            slider.normalizedValue = normalizedValue;
            currentValue = slider.normalizedValue;
            return currentValue;
        }

        int conflict = -1;
        float toSnapBack = -1;
        private void UserChangeValue()
        {
            if (!dragging) {
                return; 
            }
            float newValue = slider.normalizedValue;
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