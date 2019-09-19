using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EcoBuilder.JonnyGenerator
{
    public interface ISpeciesGenerator
    {
        GameObject GenerateSpecies(bool isProducer, float bodySize, float greediness, int randomSeed, float population=-1);
        void RegenerateSpecies(GameObject species, float size, float greed, int randomSeed, float population=-1);
    }
    public class JonnyGenerator : MonoBehaviour, ISpeciesGenerator
    {
        [SerializeField] JonnySpecies speciesPrefab;
        [SerializeField] Mesh producerMesh, consumerMesh;
        [SerializeField] Material editableMat, uneditableMat;
        [SerializeField] List<Mesh> eyes, noses, mouths;

        void ShapeSpecies(JonnySpecies js, bool isProducer, float size, float greed, int randomSeed)
        {
            if (size < 0 || size > 1)
                throw new Exception("size not in bounds");
            if (greed < 0 || greed > 1)
                throw new Exception("greed not in bounds");

            js.isProducer = isProducer;
            js.seed = randomSeed;
            UnityEngine.Random.InitState(randomSeed);
            if (isProducer)
            {
                js.name = GenerateProducerName();
                js.SetBodyMesh(producerMesh);
            }
            else
            {
                js.name = GenerateConsumerName();
                js.SetBodyMesh(consumerMesh);
            }

            Color c = GetColor(isProducer, size, greed);
            js.GetComponent<MeshRenderer>().material.color = c;

        //     js.SetEyesMesh(eyes[UnityEngine.Random.Range(0, eyes.Count)]);
        //     js.SetNoseMesh(noses[UnityEngine.Random.Range(0, noses.Count)]);
        //     js.SetMouthMesh(mouths[UnityEngine.Random.Range(0, mouths.Count)]);
        }
        private Color GetColor(bool isProducer, float size, float greed)
        {
            float y = .8f - .5f*size;
            float u = isProducer? -.4f : .4f;
            float v = -.4f + .8f*greed;
            return ColorHelper.YUVtoRGBtruncated(y, u, v);
        }


        public GameObject GenerateSpecies(bool isProducer, float size, float greed, int randomSeed, float population=-1)
        {
            JonnySpecies toSpawn = Instantiate(speciesPrefab);
            ShapeSpecies(toSpawn, isProducer, size, greed, randomSeed);
            return toSpawn.gameObject;
        }
        public void RegenerateSpecies(GameObject species, float size, float greed, int randomSeed, float population=-1)
        {
            JonnySpecies toRegen = species.GetComponent<JonnySpecies>();
            if (toRegen == null)
                throw new Exception("not JonnySpecies");

            ShapeSpecies(toRegen, toRegen.isProducer, size, greed, randomSeed);
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