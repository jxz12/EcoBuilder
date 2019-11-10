using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class Trait : MonoBehaviour, IPointerUpHandler
{
    public event Action<float, float> OnUserSlid; // from, to
    [SerializeField] Slider slider;
    public bool Interactable { set { slider.interactable = value; } }
    public bool Active { set { gameObject.SetActive(value); } }
    
    float currentValue = -1;
    UnityAction<float> ValueChangedCallback;
    void Start()
    {
        ValueChangedCallback = x=> SetValueWithCallback();
        slider.onValueChanged.AddListener(ValueChangedCallback);
    }
    public float SetValueFromRandomSeed(int randomSeed)
    {
        SetValueWithoutCallback(UnityEngine.Random.Range(0, 1f));
        // TODO: do Constraint check here too
        return slider.normalizedValue;
    }
    public void SetValueWithoutCallback(float normalizedValue)
    {
        // this is ugly as heck, but sliders are stupid
        slider.onValueChanged.RemoveListener(ValueChangedCallback);
        slider.normalizedValue = normalizedValue;
        slider.onValueChanged.AddListener(ValueChangedCallback);
    }
    public void SetValueWithCallback()
    {
        float newValue = slider.normalizedValue;
        OnUserSlid.Invoke(currentValue, newValue);
        if (Conflict())
        {
            print("CONFLICT");
            SnapBackCallback = ()=> SnapBack(currentValue);
        }
        else
        {
            SnapBackCallback = null;
        }
        currentValue = newValue;
    }
    Action SnapBackCallback;
    void SnapBack(float prevValue) // value reference problem?
    {
        OnUserSlid.Invoke(currentValue, prevValue);
        currentValue = prevValue;
        SnapBackCallback = null;
    }
    public void OnPointerUp(PointerEventData ped)
    {
        print("hi"); // FIXME: this does not get called
        if (SnapBackCallback != null)
        {
            SnapBackCallback.Invoke();
            SnapBackCallback = null;
        }
    }

    // if this function return false, the slider will 'snap' back
    Func<bool> Conflict;
    public void AddExternalConflict(Func<bool> Rule)
    {
        Conflict = Rule;
    }
}
