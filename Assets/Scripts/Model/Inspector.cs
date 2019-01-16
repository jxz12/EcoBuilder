using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace EcoBuilder.Nichess
{
    public class Inspector : MonoBehaviour
    {
        [Serializable] class IntEvent : UnityEvent<int> { }
        [Serializable] class IntBoolEvent : UnityEvent<int, bool> { }
        [Serializable] class IntFloatEvent : UnityEvent<int, float> { }
        [Serializable] class IntStringEvent : UnityEvent<int, string> { }

        [SerializeField] IntEvent SpawnedEvent;
        // [SerializeField] IntEvent ProducerSetEvent;
        // [SerializeField] IntEvent ConsumerSetEvent;
        [SerializeField] IntFloatEvent MetabolismSetEvent;
        // [SerializeField] IntFloatEvent GreedinessSetEvent;
        // [SerializeField] IntStringEvent NamedEvent;

        [SerializeField] Button spawnButton;
        [SerializeField] Text nameText;
        // [SerializeField] Button producerButton;
        // [SerializeField] Button consumerButton;
        // [SerializeField] Slider bodyMassSlider;
        // [SerializeField] Slider greedinessSlider;

        // bool nextIsProducer;
        int idxCounter;
        // HashSet<int> producerSet = new HashSet<int>();

        void Start()
        {
            // producersCounter = GameManager.Instance.MaxProducers;
            // producerButton.onClick.AddListener(() => nextIsProducer=true);
            // consumerButton.onClick.AddListener(() => nextIsProducer=false);
            idxCounter = 0;
            nameText.text = "None Selected";
        }
        public void Reload()
        {
            spawnButton.interactable = true;
            SetNewName();
        }
        public void Spawn()
        {
            names[idxCounter] = nameText.text;
            nameText.color = Color.grey;
            spawnButton.interactable = false;

            SpawnedEvent.Invoke(idxCounter);
            MetabolismSetEvent.Invoke(idxCounter, .5f);
            idxCounter += 1;
        }
        // public void Extinguish(int idx)
        // {
        //     names.Remove(idx);
        // }
        Dictionary<int, string> names = new Dictionary<int, string>();
        public void InspectSpecies(int idx)
        {
            print(idx);
            nameText.text = names[idx];
            spawnButton.interactable = false;
            // nameText.color = Color.black;
        }
        public void Uninspect()
        {
            nameText.text = "None Selected";
            spawnButton.interactable = false;
            // nameText.color = Color.grey;
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
            // if (nextIsProducer)
            //     nameText.text = GenerateProducerName();
            // else
                nameText.text = GenerateConsumerName();
        }
    }
}