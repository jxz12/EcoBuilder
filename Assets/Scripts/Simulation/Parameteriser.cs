using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class Parameteriser : MonoBehaviour
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
    [SerializeField] Slider massSlider;
    [SerializeField] Button spawnButton;
    [SerializeField] Toggle producerToggle;

    void Start()
    {
        producersCounter = GameManager.Instance.MaxProducers;
        idxCounter = 0;
        inspectedIdx = null;

        spawnButton.onClick.AddListener(() => Spawn());
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
            if (producerToggle.isOn)
            {
                speciesDict[idxCounter] = new Species(true, massSlider.value, GenerateName());
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
                speciesDict[idxCounter] = new Species(false, massSlider.value, GenerateName());
                ConsumerSpawnedEvent.Invoke(idxCounter);
            }
            idxCounter += 1;
        }
    }

    int? inspectedIdx;
    public void InspectSpecies(int idx)
    {
        nameText.text = speciesDict[idx].Name;
        massSlider.value = speciesDict[idx].BodyMass;
        producerToggle.isOn = speciesDict[idx].Producer;

        massSlider.interactable = false;
        producerToggle.interactable = false;
        inspectedIdx = idx;
    }
    public void Uninspect()
    {
        nameText.text = "(new species)";
        massSlider.interactable = true;
        if (producersCounter > 0)
            producerToggle.interactable = true;
        else
            producerToggle.isOn = false;
        inspectedIdx = null;
    }


    //private readonly Func<int, double> r_i = i => i == 0 ? .1 : -.4;
    //private readonly Func<int, double> a_ii = i => .01;
    //private readonly Func<int, int, double> a_ij = (i, j) => .02;
    //private readonly Func<int, int, double> e_ij = (i, j) => 1

    public float GetGrowth(int idx)
    {
        if (idx == 0)
            return 1;
        else
            return -2;
    }
    public float GetIntraspecific(int idx)
    {
        return -.1f;
    }
    public float GetEfficiency(int resource, int consumer)
    {
        return .5f;
    }
    public float GetInteraction(int resource, int consumer)
    {
        return 1;
    }






    public static string[] adjectives = new string[]{
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
    public static string[] nouns = new string[]{
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
    };
    public static string GenerateName() {
        return adjectives[UnityEngine.Random.Range(0,adjectives.Length)] + " " + nouns[UnityEngine.Random.Range(0,nouns.Length)];
    }
}
