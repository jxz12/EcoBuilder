using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EcoBuilder
{
    public interface ISpeciesGenerator
    {
        GameObject GenerateSpecies(bool isProducer, float bodySize, float greediness, int randomSeed);
    }
    public class JonnyGenerator : MonoBehaviour, ISpeciesGenerator
    {
        [SerializeField] JonnySpecies speciesPrefab;
        [SerializeField] Mesh producerMesh, consumerMesh;
        [SerializeField] List<Mesh> eyes, noses, mouths;

        // TODO: have a generator that changes a GameObject, instead of instantiating every time
        public GameObject GenerateSpecies(bool isProducer, float size, float greed, int randomSeed)
        {
            if (size < 0 || size > 1)
                throw new Exception("size not in bounds");
            if (greed < 0 || greed > 1)
                throw new Exception("greed not in bounds");

            JonnySpecies speciesToSpawn = Instantiate(speciesPrefab);

            UnityEngine.Random.InitState(randomSeed);
            if (isProducer)
            {
                speciesToSpawn.name = GenerateProducerName();
                speciesToSpawn.SetBodyMesh(producerMesh);
            }
            else
            {
                speciesToSpawn.name = GenerateConsumerName();
                speciesToSpawn.SetBodyMesh(consumerMesh);
            }

            Color c = GetColor(isProducer, size, greed);
            speciesToSpawn.GetComponent<MeshRenderer>().material.color = c;

            speciesToSpawn.SetEyesMesh(eyes[UnityEngine.Random.Range(0, eyes.Count)]);
            speciesToSpawn.SetNoseMesh(noses[UnityEngine.Random.Range(0, noses.Count)]);
            speciesToSpawn.SetMouthMesh(mouths[UnityEngine.Random.Range(0, mouths.Count)]);

            return speciesToSpawn.gameObject;
        }

        private Color GetColor(bool isProducer, float size, float greed)
        {
            float y = .8f - .5f*size;
            float u = isProducer? -.4f : .4f;
            float v = -.4f + .8f*greed;
            return ColorHelper.YUVtoRGBtruncated(y, u, v);
        }

        readonly float minKg = .001f, maxKg = 1000; // 1 gram to 1 tonne
        public float GetKg(float bodySize)
        {
            // float min = Mathf.Log10(minKg);
            // float max = Mathf.Log10(maxKg);
            // float mid = min + input*(max-min);
            // return Mathf.Pow(10, mid);

            // same as above commented
            return Mathf.Pow(minKg, 1-bodySize) * Mathf.Pow(maxKg, bodySize);
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