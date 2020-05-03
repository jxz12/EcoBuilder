using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.UI
{
    public class Initiator : MonoBehaviour
    {
        public event Action OnProducerWanted;
        public event Action OnConsumerWanted;

        [SerializeField] Button producerButton;
        [SerializeField] Button consumerButton;
        
        RectTransform rt;
        CanvasGroup cg;
        void Awake()
        {
            rt = GetComponent<RectTransform>();
            cg = GetComponent<CanvasGroup>();
            producerButton.onClick.AddListener(InitiateProducer);
            consumerButton.onClick.AddListener(InitiateConsumer);
        }
        void Start()
        {
            ShowButtons(true, 1f);
        }
        void InitiateProducer()
        {
            OnProducerWanted?.Invoke();
        }
        void InitiateConsumer()
        {
            OnProducerWanted?.Invoke();
        }
        IEnumerator buttonsRoutine;
        public void ShowButtons(bool showing, float duration=.5f)
        {
            cg.interactable = showing;
            if (buttonsRoutine != null) {
                StopCoroutine(buttonsRoutine);
            }
            var start = showing? new Vector2(0,0) : new Vector2(1,0);
            var end = showing? new Vector2(1,0) : new Vector2(0,0);
            StartCoroutine(buttonsRoutine = Tweens.Pivot(rt, start, end, duration));
        }
        // for tutorials
        public void HideTypeButtons(bool hidden=true)
        {
            gameObject.SetActive(!hidden);
        }
        public void EnableProducerButton(bool enabled)
        {
            producerButton.interactable = enabled;
        }
        public void EnableConsumerButton(bool enabled)
        {
            consumerButton.interactable = enabled;
        }

    }
}