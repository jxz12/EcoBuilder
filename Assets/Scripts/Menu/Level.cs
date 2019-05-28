using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EcoBuilder.Menu
{
    public class Level : MonoBehaviour
    {
        [SerializeField] string title;
        [SerializeField] string description;
        [SerializeField] int numProducers, numConsumers;
        [SerializeField] int minLoop, maxLoop;
        [SerializeField] int minChain, maxChain;
        [SerializeField] float minOmnivory, maxOmnivory;

        public void ShowLevelCard()
        {
            // TODO: set things here
            GameManager.Instance.ShowLevelCard();
        }
    }
}