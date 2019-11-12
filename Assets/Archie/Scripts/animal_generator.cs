// animal generator
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics; // TIME TESTING

namespace EcoBuilder.Archie{
    public class animal_generator : ProceduralMeshGenerator
    {
        [SerializeField] GameObject Animal_Prefab;
        [SerializeField] Mesh[] Consumer_Meshs, Producer_Meshs; // meshes should be stored in the order of the size they represent (ascending)
        static public List<GameObject> generated_consumers = new List<GameObject>();
        static public List<GameObject> generated_producers = new List<GameObject>();

        private AnimalTexture Texy;


        void Awake()
        {
            Texy = GetComponent<AnimalTexture>();
        }

        public override GameObject GenerateSpecies(bool isProducer, float bodySize, float greediness, int randomSeed, float population = -1)
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

        public override void RegenerateSpecies(GameObject species, float size, float greed, int randomSeed, float population = -1) 
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
            int d0 = UnityEngine.Random.Range(0, adjectives.Length);
            int d1 = (int)(bodySize*.999f * nounsConsumer.Length);
            int d2 = UnityEngine.Random.Range(0, nounsConsumer[d1].Length);
            animal.name = adjectives[d0] + " " + nounsConsumer[d1][d2];
            // assign mesh
            animal.GetComponent<MeshFilter>().mesh = Consumer_Meshs[(int)(bodySize*.999f * Consumer_Meshs.Length)];

            // generate texture and material
            // var yuv = new Vector3(.8f-.5f*bodySize, .4f, .8f*greediness-.4f);
            // // convert yuv to rgb
            // Color rgb = (Vector4)(AnimalTexture.yuv_to_rgb.MultiplyVector(yuv)) + new Vector4(0,0,0,1);
            // // scale mesh
            // animal.transform.localScale = Vector3.one * (1+bodySize*.2f);
            var lab = new LABColor(70-50*bodySize, 80*greediness, -50);
            Color rgb = lab.ToColor();

            Texy.Generate_and_Apply(randomSeed, animal.GetComponent<MeshRenderer>(), rgb);
        }


        private void Form_Plant(GameObject plant, float bodySize, float greediness, int randomSeed)
        {
            // give appropriate name
            int d0 = UnityEngine.Random.Range(0, adjectives.Length);
            int d1 = (int)(bodySize*.999f * nounsProducer.Length);
            int d2 = UnityEngine.Random.Range(0, nounsProducer[d1].Length);
            plant.name = adjectives[d0] + " " + nounsProducer[d1][d2];
            // assign mesh
            plant.GetComponent<MeshFilter>().mesh = Producer_Meshs[(int)(bodySize*.999f * Producer_Meshs.Length)];

            // generate texture and material
            // var yuv = new Vector3(.7f-.7f*bodySize, -.4f, .8f*greediness-.4f);
            // Color rgb = (Vector4)(AnimalTexture.yuv_to_rgb.MultiplyVector(yuv)) + new Vector4(0,0,0,1);
            // // scale mesh
            // plant.transform.localScale = Vector3.one * (1+bodySize*.2f);
            var lab = new LABColor(80-50*bodySize, -50+110*greediness, 50);
            Color rgb = lab.ToColor();

            Texy.Generate_and_Apply(randomSeed, plant.GetComponent<MeshRenderer>(), rgb);
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
        public static string[][] nounsConsumer = new string[][]
        {
            new string[]{
            "Rat",
            "Aardvark",
            "Caterpillar",
            "Ant",
            "Spider",
            "Wasp",
            "Bumblebee",
            "Beetle"},

            new string[]{
            "Koala",
            "Chihuahua",
            "Chameleon",
            "Platypus",
            "Raccoon",
            "Rabbit",
            "Snake",
            "Snake",
            "Rooster"},

            new string[]{
            "Sheep",
            "Monkey",
            "Wolverine",
            "Dog",
            "Pig",
            "Dog",
            "Pig",
            "Alpaca"},

            new string[]{
            "Tiger",
            "Horse",
            "Ox",
            "Tiger",
            "Horse",
            "Ox",
            "Velociraptor"},

            new string[]{
            "Dragon",
            "Elephant",
            "Polar Bear",
            "Crocodile",
            "Panda",
            "Bear",
            "Sloth",
            "Tyrannosaurus Rex"}
        };
        public static string[][] nounsProducer = new string[][]
        {
            new string[]{
            "Grass",
            "Weed",
            "Mushroom",
            "Fern"},

            new string[]{
            "Bush",
            "Shrub",
            "Berries",
            "Ivy"},

            new string[]{
            "Willow",
            "Sycamore",
            "Oak",
            "Beech"}
        };
    }
}
