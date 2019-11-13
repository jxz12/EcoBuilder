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
        public bool Interactable { set { slider.interactable = value; } }
        public bool Active { set { gameObject.SetActive(value); } }

        float currentValue = -1;
        UnityAction<float> ValueChangedCallback;
        Slider slider;
        void Start()
        {
            slider = GetComponent<Slider>();
            ValueChangedCallback = x=> SetValueWithCallback();
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
        float toSnapBack = -1;
        public void SetValueWithCallback()
        {
            float newValue = slider.normalizedValue;
            if (Conflict(newValue))
            {
                print("TODO: show user conflict by throwing int event");
                // SnapBackCallback = ()=> SnapBack(currentValue);
                toSnapBack = currentValue;
                slider.targetGraphic.color = Color.red;
            }
            else
            {
                OnUserSlid.Invoke(currentValue, newValue);
                // SnapBackCallback = null;
                toSnapBack = -1;
                currentValue = newValue;
                slider.targetGraphic.color = Color.white;
            }
        }
        // Action SnapBackCallback;
        // void SnapBack(prevValue) // value reference problem?
        // {
        //     SetValueWithoutCallback(prevValue);
        //     currentValue = prevValue;
        //     SnapBackCallback = null;
        // }
        public void OnPointerUp(PointerEventData ped)
        {
            // if (SnapBackCallback != null)
            // {
                // SnapBackCallback.Invoke();
                // SnapBackCallback = null;
            // }
            if (toSnapBack >= 0)
            {
                SetValueWithoutCallback(toSnapBack);
                toSnapBack = -1;
                slider.targetGraphic.color = Color.white;
            }
        }

        // if this function return false, the slider will 'snap' back
        Func<float, bool> Conflict;
        public void AddExternalConflict(Func<float, bool> Rule)
        {
            Conflict = Rule;
        }
    }
}