using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class Spawner : MonoBehaviour
{
    [Serializable] class IntStringEvent : UnityEvent<int, string> { }
    [SerializeField] IntStringEvent SpawnedEvent;

    // [Serializable] class SpeciesEvent : UnityEvent<Species> { }
    // [SerializeField] SpeciesEvent SpawnedEvent;

    [SerializeField] Text nameText;

    void Start()
    {
        producersCounter = GameManager.Instance.MaxProducers;
        idxCounter = 0;
        nameText.text = GenerateName();
    }

    int producersCounter;
    int idxCounter;

    public void SetNewName()
    {
        nameText.text = GenerateName();
    }
    public void Spawn()
    {
        // var newSpecies = new Species(idxCounter++, newIsProducer, newBodyMass, nameText.text);

        // if (newIsProducer)
        //     ProducerSpawnedEvent.Invoke(idxCounter);
        // else
        //     ConsumerSpawnedEvent.Invoke(idxCounter);

        SpawnedEvent.Invoke(idxCounter++, nameText.text);
        nameText.text = GenerateName();
        // if (newIsProducer)
        // {
        //     producersCounter--;
        //     if (producersCounter <= 0)

        // }

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
