using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

[RequireComponent(typeof(Animator))]
public class Inspector : MonoBehaviour
{
    /// <summary>
    /// attach anything you want to listen to these events
    /// </summary>
    [Serializable] class IntEvent : UnityEvent<int> { }
    [SerializeField] IntEvent ProducerSpawnedEvent;
    [SerializeField] IntEvent ConsumerSpawnedEvent;
    [SerializeField] IntEvent SpeciesRemovedEvent;

    class Species
    {
        public Species(bool isProducer, float bodyMass, string name)
        {
            Producer = isProducer;
            BodyMass = bodyMass;
            Name = name;
        }

        public bool Producer { get; private set; }
        public float BodyMass { get; private set; }
        public string Name { get; private set; }
    }

    [SerializeField] Text nameText;
    [SerializeField] Button nameRefresh;
    [SerializeField] Slider massSlider;
    [SerializeField] Button spawnButton;
    [SerializeField] Text spawnText;
    [SerializeField] Toggle producerToggle;

    Animator anim;
    [SerializeField] Button appear;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }
    void Start()
    {
        producersCounter = GameManager.Instance.MaxProducers;
        idxCounter = 0;

        spawnButton.onClick.AddListener(() => Spawn());
        appear.onClick.AddListener(() => { anim.SetTrigger("Toggle"); nameText.text = GenerateName(); });
        nameRefresh.onClick.AddListener(() => { nameText.text = GenerateName(); });

    }

    Dictionary<int, Species> speciesDict = new Dictionary<int, Species>();
    int producersCounter;
    int idxCounter;
    void Spawn()
    {
        if (inspectedIdx != null)
        {
            Species toRemove = speciesDict[(int)inspectedIdx];
            if (toRemove.Producer)
            {
                producersCounter += 1;
                if (producersCounter == 1)
                {
                    producerToggle.interactable = true;
                }
            }
            speciesDict.Remove((int)inspectedIdx);
            SpeciesRemovedEvent.Invoke((int)inspectedIdx);
            Uninspect();
        }
        else
        {
            speciesDict[idxCounter] = new Species(true, massSlider.value, nameText.text);
            idxCounter += 1;

            if (producerToggle.isOn)
            {
                ProducerSpawnedEvent.Invoke(idxCounter);
                producersCounter -= 1;
                if (producersCounter == 0)
                {
                    producerToggle.interactable = false;
                    producerToggle.isOn = false;
                }
            }
            else
            {
                ConsumerSpawnedEvent.Invoke(idxCounter);
            }
        }
        anim.SetTrigger("Toggle");
    }

    int? inspectedIdx;
    public void InspectSpecies(int idx)
    {
        nameText.text = speciesDict[idx].Name;
        spawnText.text = "REMOVE";
        massSlider.value = speciesDict[idx].BodyMass;
        producerToggle.isOn = speciesDict[idx].Producer;

        massSlider.interactable = false;
        producerToggle.interactable = false;
        inspectedIdx = idx;
    }
    public void Uninspect()
    {
        nameText.text = "<new species>";
        spawnText.text = "SPAWN";
        massSlider.interactable = true;
        if (producersCounter > 0)
            producerToggle.interactable = true;
        else
            producerToggle.isOn = false;
        inspectedIdx = null;
    }

    public double GetMass(int idx)
    {
        return speciesDict[idx].BodyMass;
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
    public static string[] nouns = new string[]
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

    public static string GenerateName() {
        return adjectives[UnityEngine.Random.Range(0,adjectives.Length)] + " " + nouns[UnityEngine.Random.Range(0,nouns.Length)];
    }
}
