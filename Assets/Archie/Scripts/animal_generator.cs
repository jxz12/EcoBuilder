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
        [SerializeField] int Texture_Size = 64;
        static public List<GameObject> generated_consumers = new List<GameObject>();
        static public List<GameObject> generated_producers = new List<GameObject>();
        // [SerializedField] int seed;

        public Material Base_Animal_Material; // arranged in ascending order of size they represent
        private AnimalTexture Texy;
        [SerializeField] Texture2D[] Face_Textures;

        //Testing Fields
        public bool Producer = false;
        public float Size;
        public float Greed;
        public int Seed;
        // private animal_generator genny;

        void Start()
        {
            Texy = new AnimalTexture();
            Texy.Base_Animal_Material = Base_Animal_Material;
            Texy.Face_Textures = Face_Textures;

        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.U))
            {
                GenerateSpecies(Producer, Size, Greed, Seed);
            }
            if (Input.GetKeyDown(KeyCode.B))
            {
                RegenerateSpecies(generated_consumers[generated_consumers.Count - 1], Size, Greed, Seed);
            }
        }

        public GameObject GenerateSpecies(bool isProducer, float bodySize, float greediness, int randomSeed, float population = -1)
        {
            UnityEngine.Random.InitState(randomSeed);
            var created_species = Instantiate(Animal_Prefab);
            generated_consumers.Add(created_species);
            created_species.name = "Animal_no." + (generated_consumers.Count).ToString();
            if (!isProducer)
            {
                // // assign mesh
                // created_species.GetComponent<MeshFilter>().mesh = Consumer_Meshs[(int)(bodySize/(1.0f/3.0f))];
                // // // generate texture and material
                // var yuv_coordinates = new Vector3(greediness, UnityEngine.Random.Range(0.0F, 1.0F), UnityEngine.Random.Range(0.0F, 1.0F));
                // // created_species.GetComponent<MeshRenderer>().material = Texy.Generate_and_Apply(randomSeed, bodySize, yuv_coordinates, Texture_Size);
                // created_species.GetComponent<MeshRenderer>().material = Texy.Generate_and_Apply(randomSeed, bodySize, yuv_coordinates);
                Form_Animal(created_species, bodySize, greediness, randomSeed);
            }
            else
            {
                // generate tree
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
                //re-generate tree
            }
        }

        private void Form_Animal(GameObject animal, float bodySize, float greediness, int randomSeed)
        {
            // give appropriate name
            animal.name = adjectives[UnityEngine.Random.Range(0, adjectives.Length)] + " " + nounsConsumer[(int)(bodySize / (1.0f / (float)(nounsConsumer.GetLength(0)))), UnityEngine.Random.Range(0, nounsConsumer.GetLength(1))];
            // assign mesh
            animal.GetComponent<MeshFilter>().mesh = Consumer_Meshs[(int)(bodySize / (1.0f / (float)(Consumer_Meshs.Length)))];
            // // generate texture and material
            var yuv_coordinates = new Vector3(greediness, UnityEngine.Random.Range(0.0F, 1.0F), UnityEngine.Random.Range(0.0F, 1.0F));
            // created_species.GetComponent<MeshRenderer>().material = Texy.Generate_and_Apply(randomSeed, bodySize, yuv_coordinates, Texture_Size);
            animal.GetComponent<MeshRenderer>().material = Texy.Generate_and_Apply(randomSeed, bodySize, yuv_coordinates);
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
