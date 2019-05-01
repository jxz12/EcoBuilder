using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace EcoBuilder.Inspector
{
    public class Inspector : MonoBehaviour
    {
        public event Action OnProducerPressed;
        public event Action OnConsumerPressed;
        public event Action<float> OnSizeChanged;
        public event Action<float> OnGreedChanged;

        [SerializeField] Slider sizeSlider;
        [SerializeField] Slider greedSlider;
        [SerializeField] Button producerButton;
        [SerializeField] Button consumerButton;

        void Start()
        {
            sizeSlider.onValueChanged.AddListener(x=> OnSizeChanged.Invoke(x));
            greedSlider.onValueChanged.AddListener(x=> OnGreedChanged.Invoke(x));
            producerButton.onClick.AddListener(()=> OnProducerPressed.Invoke());
            consumerButton.onClick.AddListener(()=> OnConsumerPressed.Invoke());
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