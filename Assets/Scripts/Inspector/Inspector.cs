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

        [SerializeField] Token producer;
        [SerializeField] Token consumer;
        [SerializeField] Egg egg;
        [SerializeField] List<GameObject> numbers;

        void Awake()
        {
            idxCounter = 0;
            producer.OnChosen += ()=> nextIsProducer = true;
            producer.OnChosen += ()=> producer.Center();
            producer.OnChosen += ()=> consumer.Exit();
            producer.OnMetabolismChosen += ()=> egg.MakeHatchable();

            consumer.OnChosen += ()=> nextIsProducer = false;
            consumer.OnChosen += ()=> consumer.Center();
            consumer.OnChosen += ()=> producer.Exit();
            consumer.OnMetabolismChosen += ()=> egg.MakeHatchable();
            // producer.OnMetabolismChosen += ()=> Instantiate(numbers[producer.Number-1], producer.transform);

            egg.OnHatched += ()=> Spawn();
            egg.OnHatched += ()=> { if (nextIsProducer) producer.Reset(); else consumer.Reset(); };
        }
        void Start()
        {
            // producersCounter = GameManager.Instance.MaxProducers;
            // nextIsProducer = consumerButton.interactable;
        }
        bool loaded = false;
        public void Reload()
        {
            if (!loaded)
            {
                egg.Enter();
                producer.Enter();
                consumer.Enter();
                loaded = true;
            }
            // spawnButton.interactable = true;
            // if (nextIsProducer)
            //     nameText.text = GenerateProducerName();
            // else
            //     nameText.text = GenerateConsumerName();
        }
        public void Spawn()
        {
            // var newSpecies = new Species(
            //     nameText.text, nextIsProducer,
            //     metabolismSlider.normalizedValue, greedinessSlider.normalizedValue
            // );
            Species newSpecies;
            OnSpawned.Invoke(idxCounter);
            if (nextIsProducer)
            {
                newSpecies = new Species(
                    "bob", true,
                    producer.Metabolism, 0.5f
                );
                OnProducerSet.Invoke(idxCounter);
            }
            else
            {
                newSpecies = new Species(
                    "susan", false,
                    consumer.Metabolism, 0.5f
                );
                OnConsumerSet.Invoke(idxCounter);
            }
            OnMetabolismSet.Invoke(idxCounter, newSpecies.metabolism);
            OnGreedinessSet.Invoke(idxCounter, newSpecies.greediness);

            speciesDict[idxCounter] = newSpecies;
            idxCounter += 1;
            loaded = false;
        }
        public void Extinguish(int idx)
        {
            // speciesDict.Remove(idx);
            // Uninspect();
        }
        Dictionary<int, Species> speciesDict = new Dictionary<int, Species>();
        public void InspectSpecies(int idx)
        {
        }
        public void Uninspect()
        {
            if (loaded)
            {
                egg.Reset();
                producer.Reset();
                consumer.Reset();
                loaded = false;
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