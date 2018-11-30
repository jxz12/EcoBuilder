using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace EcoBuilder
{
    public class Spawner : MonoBehaviour
    {
        [Serializable] class IntEvent : UnityEvent<int> { }
        [Serializable] class IntBoolEvent : UnityEvent<int, bool> { }
        [Serializable] class IntFloatEvent : UnityEvent<int, float> { }
        [Serializable] class IntStringEvent : UnityEvent<int, string> { }

        [SerializeField] IntEvent SpawnedEvent;
        [SerializeField] IntEvent ProducerSetEvent;
        [SerializeField] IntEvent ConsumerSetEvent;
        [SerializeField] IntFloatEvent BodyMassSetEvent;
        [SerializeField] IntFloatEvent GreedinessSetEvent;
        [SerializeField] IntStringEvent NamedEvent;

        [SerializeField] Text nameText;
        [SerializeField] Button producerButton;
        [SerializeField] Button consumerButton;
        [SerializeField] Slider bodyMassSlider;
        [SerializeField] Slider greedinessSlider;

        bool nextIsProducer;
        int idxCounter;
        HashSet<int> producerSet = new HashSet<int>();

        void Start()
        {
            // producersCounter = GameManager.Instance.MaxProducers;
            producerButton.onClick.AddListener(() => nextIsProducer=true);
            consumerButton.onClick.AddListener(() => nextIsProducer=false);
            idxCounter = 0;
        }
        public void Spawn()
        {
            SpawnedEvent.Invoke(idxCounter);
            if (nextIsProducer)
            {
                ProducerSetEvent.Invoke(idxCounter);
                producerSet.Add(idxCounter);
                if (producerSet.Count >= GameManager.Instance.MaxProducers)
                    producerButton.interactable = false;
            }
            else
                ConsumerSetEvent.Invoke(idxCounter);

            BodyMassSetEvent.Invoke(idxCounter, bodyMassSlider.normalizedValue);
            GreedinessSetEvent.Invoke(idxCounter, greedinessSlider.normalizedValue);
            NamedEvent.Invoke(idxCounter, nameText.text);

            idxCounter += 1;
            // if (nextIsProducer)
            // {
            //     producersCounter -= 1;
            //     if (producersCounter == 0)
            //         producerButton.interactable = false;
            // }
        }
        public void Despawn(int idx)
        {
            if (producerSet.Contains(idx))
            {
                producerSet.Remove(idx);
                producerButton.interactable = true;
            }

        }
        

        public static string[] adjectives = new string[]
        {
            // "Doc",
            "Grumpy",
            "Happy",
            "Sleepy",
            "Dopey",
            "Bashful",
            "Sneezy",

            "Tiny",
            "Hungry",
            "Flirtatious",
            "Fire-Breathing",
            "Curious",
            "Brave",
            "Wise",
            "Flying",
            "Mega-Ultra",
            "Invisible",
            "Average",
            "Shy",
        };
        public static string[] nounsConsumer = new string[]
        {
            "Rat",
            "Ox",
            "Tiger",
            "Rabbit",
            "Dragon",
            "Snake",
            "Horse",
            "Sheep",
            "Monkey",
            "Rooster",
            "Dog",
            "Pig",

            "Elephant",
            "Polar Bear",
            "Aardvark",
            "Koala",
            "Platypus",
            "Raccoon",
            "Crocodile",
            "Caterpillar",
            "Sloth",
            "Panda",
            "Chameleon",
            "Alpaca",
            "Wolverine",
            "Bear",
            "Elephant",
            "Chihuahua",
            "Koala",
            "Tyrannosaurus Rex",
            "Velociraptor",

            "Ant",
            "Spider",
            "Wasp",
            "Bumblebee",
            "Beetle",
        };
        public static string[] nounsProducer = new string[]
        {
            "Strawberry",
            "Blueberry",
            "Banana",
            "Watermelon",
            "Orange",
            "Apple",
            "Durian",
            "Grape",
            "Peach",
            "Pineapple",
            "Cherry",

            "Cucumber",
            "Lettuce",
            "Carrot",
            "Wheat",
            "Clover",
            "Onion",
            "Celery",
            "Radish",
            "Spinach",
            "Pumpkin",

            "Lily",
            "Rose",
            "Bamboo",
            "Sunflower",
            "Chrysanthemum",
        };

        public static string GenerateProducerName() {
            return adjectives[UnityEngine.Random.Range(0,adjectives.Length)] + " "
                            + nounsProducer[UnityEngine.Random.Range(0, nounsProducer.Length)];
        }
        public static string GenerateConsumerName() {
            return adjectives[UnityEngine.Random.Range(0,adjectives.Length)] + " "
                            + nounsConsumer[UnityEngine.Random.Range(0, nounsConsumer.Length)];
        }
        public void SetNewName()
        {
            if (nextIsProducer)
                nameText.text = GenerateProducerName();
            else
                nameText.text = GenerateConsumerName();
        }
    }
}