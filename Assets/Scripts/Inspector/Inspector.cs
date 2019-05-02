using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace EcoBuilder.Inspector
{
    public class Inspector : MonoBehaviour
    {
        public event Action<float> OnSizeChanged;
        public event Action<float> OnGreedChanged;

        [SerializeField] Text nameText;
        [SerializeField] Slider sizeSlider;
        [SerializeField] Slider greedSlider;

        void Start()
        {
            sizeSlider.onValueChanged.AddListener(x=> OnSizeChanged.Invoke(x));
            greedSlider.onValueChanged.AddListener(x=> OnGreedChanged.Invoke(x));
        }

        public void SetName(string name)
        {
            nameText.text = name;
        }
        public void SetSize(float size)
        {
            sizeSlider.value = size;
        }
        public void SetGreed(float greed)
        {
            greedSlider.value = greed;
        }
    }
}