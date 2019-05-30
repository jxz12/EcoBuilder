using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.Menu
{
    public class Level : MonoBehaviour
    {
        [SerializeField] int number;
        [SerializeField] string title;
        [SerializeField] string description;
        [SerializeField] int numProducers, numConsumers;
        [SerializeField] int minLoop, maxLoop;
        [SerializeField] int minChain, maxChain;
        [SerializeField] float minOmnivory, maxOmnivory;

        [SerializeField] Button chooseButton;
        [SerializeField] Text numberText;
        [SerializeField] Image starsImage;
        [SerializeField] Image lockImage;

        void Start()
        {
            chooseButton.onClick.AddListener(()=> ShowLevelCard());
            numberText.text = number.ToString();
        }
        // TODO: make these animations;
        public void Lock()
        {
            chooseButton.interactable = false;
            lockImage.gameObject.SetActive(true);
            numberText.gameObject.SetActive(false);
            starsImage.gameObject.SetActive(false);
        }
        public void Unlock()
        {
            chooseButton.interactable = true;
            lockImage.gameObject.SetActive(false);
            numberText.gameObject.SetActive(true);
            starsImage.gameObject.SetActive(true);
        }
        public void SetStars(Sprite s)
        {
            starsImage.sprite = s;
        }

        public void ShowLevelCard()
        {
            GameManager.Instance.ShowLevelCard(number, title, description, numProducers, numConsumers);
        }
    }
}