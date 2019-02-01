using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace EcoBuilder.Inspector
{
    public class Inspector : MonoBehaviour
    {
        [Serializable] class IntEvent : UnityEvent<int> { }
        [Serializable] class IntBoolEvent : UnityEvent<int, bool> { }
        [Serializable] class IntFloatEvent : UnityEvent<int, float> { }
        [Serializable] class IntStringEvent : UnityEvent<int, string> { }

        [SerializeField] IntEvent OnSpawned;
        [SerializeField] IntEvent OnProducerSet;
        [SerializeField] IntEvent OnConsumerSet;
        [SerializeField] IntFloatEvent OnMetabolismSet;
        [SerializeField] IntFloatEvent OnGreedinessSet;

        // [SerializeField] Button spawnButton;
        // [SerializeField] Text nameText;
        // [SerializeField] Button producerButton;
        // [SerializeField] Button consumerButton;
        // [SerializeField] Slider metabolismSlider;
        // [SerializeField] Slider greedinessSlider;

        bool nextIsProducer;
        int idxCounter;
        // HashSet<int> producerSet = new HashSet<int>();
        private class Species {
            public string name;
            public bool isProducer;
            public float metabolism;
            public float greediness;
            public Species(string n, bool iP, float m, float g)
            {
                name = n;
                isProducer = iP;
                metabolism = m;
                greediness = g;
            }
        }

        void Awake()
        {
            idxCounter = 0;
        }
        void Start()
        {
            // producersCounter = GameManager.Instance.MaxProducers;
            // nextIsProducer = consumerButton.interactable;
            // producerButton.onClick.AddListener(() => nextIsProducer=true);
            // producerButton.onClick.AddListener(() => nameText.text = GenerateProducerName());
            // consumerButton.onClick.AddListener(() => nextIsProducer=false);
            // consumerButton.onClick.AddListener(() => nameText.text = GenerateConsumerName());
            // nameText.text = "";

            //////////////////////////////////////////////////
            // producerButton.gameObject.SetActive(false);
            // consumerButton.gameObject.SetActive(false);
            // metabolismSlider.gameObject.SetActive(false);
            // greedinessSlider.gameObject.SetActive(false);
        }
        public void Reload()
        {
            GetComponent<Animator>().SetTrigger("Enter");
            // spawnButton.interactable = true;
            // if (nextIsProducer)
            //     nameText.text = GenerateProducerName();
            // else
            //     nameText.text = GenerateConsumerName();

            // ///////////////////////////////////////////////
            // producerButton.gameObject.SetActive(true);
            // consumerButton.gameObject.SetActive(true);
            // metabolismSlider.gameObject.SetActive(true);
            // greedinessSlider.gameObject.SetActive(true);

            // producerButton.targetGraphic.raycastTarget = true;
            // consumerButton.targetGraphic.raycastTarget = true;
            // metabolismSlider.interactable = true;
            // greedinessSlider.interactable = true;
            // spawnButton.interactable = true; // TODO: only allow after choosing attributes
        }
        public void Spawn()
        {
            // var newSpecies = new Species(
            //     nameText.text, nextIsProducer,
            //     metabolismSlider.normalizedValue, greedinessSlider.normalizedValue
            // );

            // OnSpawned.Invoke(idxCounter);

            // if (newSpecies.isProducer)
            //     OnProducerSet.Invoke(idxCounter);
            // else
            //     OnConsumerSet.Invoke(idxCounter);
            // OnMetabolismSet.Invoke(idxCounter, newSpecies.metabolism);
            // OnGreedinessSet.Invoke(idxCounter, newSpecies.greediness);

            // speciesDict[idxCounter] = newSpecies;
            // InspectSpecies(idxCounter);
            // idxCounter += 1;
        }
        public void Extinguish(int idx)
        {
            speciesDict.Remove(idx);
            Uninspect();
        }
        Dictionary<int, Species> speciesDict = new Dictionary<int, Species>();
        public void InspectSpecies(int idx)
        {
            // nameText.text = speciesDict[idx].name;
            // producerButton.interactable = !speciesDict[idx].isProducer;
            // consumerButton.interactable = speciesDict[idx].isProducer;
            // metabolismSlider.normalizedValue = speciesDict[idx].metabolism;
            // greedinessSlider.normalizedValue = speciesDict[idx].greediness;
            
            // ////////////////////////////////////////////////////////////////////
            // producerButton.gameObject.SetActive(true);
            // consumerButton.gameObject.SetActive(true);
            // metabolismSlider.gameObject.SetActive(true);
            // greedinessSlider.gameObject.SetActive(true);

            // producerButton.targetGraphic.raycastTarget = false; // TODO: move these into an animator
            // consumerButton.targetGraphic.raycastTarget = false;
            // metabolismSlider.interactable = false;
            // greedinessSlider.interactable = false;
            // spawnButton.interactable = false;
        }
        public void Uninspect()
        {
            GetComponent<Animator>().SetTrigger("Exit");
            // nameText.text = "";
            // spawnButton.interactable = false;

            // ///////////////////////////////////////////////////////
            // producerButton.gameObject.SetActive(false);
            // consumerButton.gameObject.SetActive(false);
            // metabolismSlider.gameObject.SetActive(false);
            // greedinessSlider.gameObject.SetActive(false);
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
    }
}