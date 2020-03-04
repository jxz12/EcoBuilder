// animal generator
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace EcoBuilder.Archie
{
    public class animal_generator : ProceduralMeshGenerator
    {
        [SerializeField] animal_object speciesPrefab;
        [SerializeField] Mesh[] Consumer_Meshs, Producer_Meshs; // meshes should be stored in the order of the size they represent (ascending)

        public override GameObject GenerateSpecies(bool isProducer, float bodySize, float greediness, int randomSeed)
        {
            animal_object created_species = Instantiate(speciesPrefab);
            created_species.IsPlant = isProducer;

            UnityEngine.Random.InitState(randomSeed);
            if (isProducer)
            {
                Form_Plant(created_species, bodySize, greediness, randomSeed);
            }
            else
            {
                Form_Animal(created_species, bodySize, greediness, randomSeed);
            }
            return created_species.gameObject;
        }

        public override void RegenerateSpecies(GameObject species, float size, float greed, int randomSeed)
        {
            var created_species = species.GetComponent<animal_object>();
            Assert.IsNotNull(created_species, "gameobject corrupted since generation to have no animal_object");

            UnityEngine.Random.InitState(randomSeed);
            if (created_species.IsPlant)
            {
                Form_Plant(created_species, size, greed, randomSeed);
            }
            else
            {
                Form_Animal(created_species, size, greed, randomSeed);
            }
        }


        private void Form_Animal(animal_object animal, float bodySize, float greediness, int seed)
        {
            // // give appropriate name
            // int d0 = UnityEngine.Random.Range(0, adjectives.Length);
            // int d1 = (int)(bodySize*.999f * nounsConsumer.Length);
            // int d2 = UnityEngine.Random.Range(0, nounsConsumer[d1].Length);
            // animal.name = adjectives[d0] + " " + nounsConsumer[d1][d2];
            animal.name = name_generator.GetName6(seed);

            // assign mesh
            animal.Renderer.sharedMesh = Consumer_Meshs[(int)(bodySize*.999f * Consumer_Meshs.Length)];

            // generate texture and material
            // var yuv = new Vector3(.8f-.5f*bodySize, .4f, .8f*greediness-.4f);
            // // convert yuv to rgb
            // Color rgb = (Vector4)(AnimalTexture.yuv_to_rgb.MultiplyVector(yuv)) + new Vector4(0,0,0,1);
            // // scale mesh
            animal.transform.localScale = Vector3.one * (1+bodySize*.2f);
            var lab = new LABColor(70-50*bodySize, 60*greediness, -50);
            Color rgb = lab.ToColor();

            Generate_and_Apply(animal, seed, rgb);
        }

        private void Form_Plant(animal_object plant, float bodySize, float greediness, int seed)
        {
            // give appropriate name
            // int d0 = UnityEngine.Random.Range(0, adjectives.Length);
            // int d1 = (int)(bodySize*.999f * nounsProducer.Length);
            // int d2 = UnityEngine.Random.Range(0, nounsProducer[d1].Length);
            // plant.name = adjectives[d0] + " " + nounsProducer[d1][d2];
            plant.name = name_generator.GetName1(seed);

            // assign mesh
            plant.Renderer.sharedMesh = Producer_Meshs[(int)(bodySize*.999f * Producer_Meshs.Length)];

            // generate texture and material
            // var yuv = new Vector3(.7f-.7f*bodySize, -.4f, .8f*greediness-.4f);
            // Color rgb = (Vector4)(AnimalTexture.yuv_to_rgb.MultiplyVector(yuv)) + new Vector4(0,0,0,1);
            // // scale mesh
            plant.transform.localScale = Vector3.one * (1+bodySize*.2f);
            var lab = new LABColor(80-50*bodySize, -80+100*greediness, 50);
            Color rgb = lab.ToColor();

            Generate_and_Apply(plant, seed, rgb);
        }
        // [SerializeField] animal_effect skullPrefab, heartPrefab;
        public override void KillSpecies(GameObject species)
        {
            var created_species = species.GetComponent<animal_object>();
            Assert.IsNotNull(created_species, "gameobject corrupted since generation to have no animal_object");

            // Instantiate(skullPrefab, created_species.transform);
            created_species.Die();
            created_species.Renderer.materials[1].SetTexture("_MainTex", DeadEyeTexture);
        }
        public override void RescueSpecies(GameObject species)
        {
            var created_species = species.GetComponent<animal_object>();
            Assert.IsNotNull(created_species, "gameobject corrupted since generation to have no animal_object");

            // Instantiate(heartPrefab, created_species.transform);
            created_species.Live();
            created_species.Renderer.materials[1].SetTexture("_MainTex", created_species.Eyes);
        }

        [SerializeField] Texture2D[] EyeTextures, MouthTextures, CheeckTextures, NoseTextures; // arranged in ascending order of size they represent
        [SerializeField] Texture2D EmptyTexture, DeadEyeTexture;
        [SerializeField] Material WoodMaterial;
        private void Generate_and_Apply(animal_object obj, int seed, Color col)
        {
            obj.Renderer.materials[0].SetColor("_Color", col);

            if (obj.Renderer.materials.Length > 1)
            {
                var eyes = pick_random(EyeTextures);
                var nose = obj.IsPlant? EmptyTexture : pick_random(NoseTextures);
                var mouth = pick_random(MouthTextures);
                var cheek = pick_random(CheeckTextures);

                obj.Eyes = eyes; // used for dying later
                if (obj.Renderer.materials[1].GetTexture("_MainTex") != DeadEyeTexture)
                {
                    // only change if not dead
                    obj.Renderer.materials[1].SetTexture("_MainTex", eyes);
                }
                obj.Renderer.materials[1].SetTexture("_MainTex2", mouth);
                obj.Renderer.materials[1].SetTexture("_MainTex3", nose);
                obj.Renderer.materials[1].SetTexture("_MainTex4", cheek);
            }

            // for baobab lol
            if (obj.Renderer.sharedMesh.subMeshCount == 3 && obj.Renderer.materials.Length < 3)
            {
                var prevMaterials = new List<Material>(obj.Renderer.materials);
                prevMaterials.Add(WoodMaterial);

                obj.Renderer.materials = prevMaterials.ToArray();
            }
            else if (obj.Renderer.sharedMesh.subMeshCount == 2 && obj.Renderer.materials.Length > 2)
            {
                var prevMaterials = new List<Material>();
                prevMaterials.Add(obj.Renderer.materials[0]);
                prevMaterials.Add(obj.Renderer.materials[1]);

                obj.Renderer.materials = prevMaterials.ToArray();
            }
        }
        private Texture2D pick_random(Texture2D[] A)
        {
            return A[UnityEngine.Random.Range(0, A.Length)];
        }

        // idle animations
        [SerializeField] float idlePoisson = 7f;
        float lastIdle=0;
        void Update()
        {
            float t = Time.time - lastIdle;
            float prob = (1-Mathf.Exp(-t * (1/idlePoisson)));
            if (Random.Range(0,1f) < prob)
            {
                animal_object.RandomIdleAnimation();
                lastIdle = Time.time;
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
