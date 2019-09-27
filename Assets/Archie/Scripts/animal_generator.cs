// animal generator
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics; // TIME TESTING

namespace EcoBuilder.Archie{
    using JohnnysInterface;
    public class animal_generator : MonoBehaviour, ISpeciesGenerator
    {
        [SerializeField] GameObject Animal_Prefab;
        [SerializeField] Mesh[] Consumer_Meshs; // meshes should be stored in the order of the size they represent (ascending)
        static public List<GameObject> generated_consumers = new List<GameObject>();
        static public List<GameObject> generated_producers = new List<GameObject>();

        private AnimalTexture Texy;


        void Awake()
        {
            Texy = GetComponent<AnimalTexture>();

        }

        public GameObject GenerateSpecies(bool isProducer, float bodySize, float greediness, int randomSeed, float population = -1)
        {
            UnityEngine.Random.InitState(randomSeed);
            var created_species = Instantiate(Animal_Prefab);
            if (!isProducer)
            {
                Form_Animal(created_species, bodySize, greediness, randomSeed);
                generated_consumers.Add(created_species);
            }
            else
            {
                Form_Plant(created_species, bodySize, greediness, randomSeed);
                generated_producers.Add(created_species);
            }
            return created_species;
        }

        public void RegenerateSpecies(GameObject species, float size, float greed, int randomSeed, float population = -1) 
        {
            UnityEngine.Random.InitState(randomSeed);
            if (generated_consumers.Contains(species))
            {
                Form_Animal(species, size, greed, randomSeed);
            }
            else
            {
                Form_Plant(species, size, greed, randomSeed);
                //re-generate tree
            }
        }

        private void Form_Animal(GameObject animal, float bodySize, float greediness, int randomSeed)
        {
            // give appropriate name
            animal.name = adjectives[UnityEngine.Random.Range(0, adjectives.Length)] + " " + nounsConsumer[(int)(bodySize * (float)(nounsConsumer.GetLength(0))), UnityEngine.Random.Range(0, nounsConsumer.GetLength(1))];
            // assign mesh
            animal.GetComponent<MeshFilter>().mesh = Consumer_Meshs[(int)(bodySize * (float)(Consumer_Meshs.Length))];
            // generate texture and material
            var yuv_coordinates = new Vector3(bodySize, .4f, greediness);
            animal.GetComponent<MeshRenderer>().material = Texy.Generate_and_Apply(randomSeed, bodySize, yuv_coordinates);
        }


        private void Form_Plant(GameObject plant, float bodySize, float greediness, int randomSeed)
        {
            // give appropriate name
            plant.name = adjectives[UnityEngine.Random.Range(0, adjectives.Length)] + " " + nounsConsumer[(int)(bodySize * (float)(nounsConsumer.GetLength(0))), UnityEngine.Random.Range(0, nounsConsumer.GetLength(1))];
            // assign mesh
            plant.GetComponent<MeshFilter>().mesh = Consumer_Meshs[(int)(bodySize * (float)(Consumer_Meshs.Length))];
            // generate texture and material
            var yuv_coordinates = new Vector3(bodySize, -.4f, greediness);
            plant.GetComponent<MeshRenderer>().material = Texy.Generate_and_Apply(randomSeed, bodySize, yuv_coordinates);
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
        public static string[,] nounsConsumer = new string[,]
        {
            {"Rat",
            "Aardvark",
            "Caterpillar",
            "Chameleon",
            "Ant",
            "Spider",
            "Wasp",
            "Bumblebee",
            "Beetle"},

            {"Koala",
            "Chihuahua",
            "Platypus",
            "Raccoon",
            "Rabbit",
            "Snake",
            "Rooster",
            "Snake",
            "Rooster"},

            {"Sheep",
            "Monkey",
            "Wolverine",
            "Dog",
            "Pig",
            "Alpaca",
            "Dog",
            "Pig",
            "Alpaca"},

            {"Tiger",
            "Horse",
            "Ox",
            "Velociraptor",
            "Tiger",
            "Horse",
            "Ox",
            "Velociraptor",
            "Velociraptor"},

            {"Dragon",
            "Elephant",
            "Polar Bear",
            "Crocodile",
            "Panda",
            "Bear",
            "Sloth",
            "Tyrannosaurus Rex",
            "Tyrannosaurus Rex"}
        };
    }
}
