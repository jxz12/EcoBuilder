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

        [SerializeField] UnityEvent OnDisplayed;
        [SerializeField] UnityEvent OnHidden;
        [SerializeField] IntEvent OnSpawned;
        [SerializeField] IntEvent OnProducerSet;
        [SerializeField] IntEvent OnConsumerSet;
        [SerializeField] IntFloatEvent OnMetabolismSet;
        [SerializeField] IntFloatEvent OnGreedinessSet;

        enum ChosenType { None, Producer, Consumer };
        ChosenType chosenType = ChosenType.None;
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

        [SerializeField] Token producer;
        [SerializeField] Token consumer;
        [SerializeField] Egg egg;
        [SerializeField] List<GameObject> numbers;

        void Awake()
        {
            idxCounter = 0;
            producer.OnChosen += ()=> chosenType = ChosenType.Producer;
            producer.OnChosen += ()=> producer.Choose();
            producer.OnChosen += ()=> consumer.Exit();
            producer.OnMetabolismChosen += ()=> egg.MakeHatchable();

            consumer.OnChosen += ()=> chosenType = ChosenType.Consumer;
            consumer.OnChosen += ()=> consumer.Choose();
            consumer.OnChosen += ()=> producer.Exit();
            consumer.OnMetabolismChosen += ()=> egg.MakeHatchable();
            // producer.OnMetabolismChosen += ()=> Instantiate(numbers[producer.Number-1], producer.transform);

            egg.OnHatched += ()=> Spawn();
        }
        void Start()
        {
            // producersCounter = GameManager.Instance.MaxProducers;
            // nextIsProducer = consumerButton.interactable;
        }
        bool loaded;
        public void Reload()
        {
            if (!loaded)
            {
                egg.Enter();
                producer.Enter();
                consumer.Enter();
                chosenType = ChosenType.None;
                loaded = true;
                OnDisplayed.Invoke();
            }
        }
        public void Spawn()
        {
            // var newSpecies = new Species(
            //     nameText.text, nextIsProducer,
            //     metabolismSlider.normalizedValue, greedinessSlider.normalizedValue
            // );
            Species newSpecies;
            OnSpawned.Invoke(idxCounter);
            if (chosenType == ChosenType.Producer)
            {
                newSpecies = new Species(
                    "bob", true,
                    producer.Metabolism, 0.5f
                );
                OnProducerSet.Invoke(idxCounter);
                producer.Exit();
            }
            else if (chosenType == ChosenType.Consumer)
            {
                newSpecies = new Species(
                    "susan", false,
                    consumer.Metabolism, 0.5f
                );
                OnConsumerSet.Invoke(idxCounter);
                consumer.Exit();
            }
            else
            {
                throw new Exception("not possible type");
            }
            OnMetabolismSet.Invoke(idxCounter, newSpecies.metabolism);
            OnGreedinessSet.Invoke(idxCounter, newSpecies.greediness);

            speciesDict[idxCounter] = newSpecies;
            idxCounter += 1;
            chosenType = ChosenType.None;

            loaded = false;
            OnHidden.Invoke();
        }
        public void Extinguish(int idx)
        {
            // speciesDict.Remove(idx);
            // Uninspect();
        }
        Dictionary<int, Species> speciesDict = new Dictionary<int, Species>();
        public void InspectSpecies(int idx)
        {
            Uninspect();
        }
        public void Uninspect()
        {
            if (loaded)
            {
                if (chosenType == ChosenType.None)
                {
                    producer.Exit();
                    consumer.Exit();
                }
                else if (chosenType == ChosenType.Producer)
                {
                    producer.Exit();
                }
                else if (chosenType == ChosenType.Consumer)
                {
                    consumer.Exit();
                }
                egg.Exit();
                loaded = false;
                OnHidden.Invoke();
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
    }
}