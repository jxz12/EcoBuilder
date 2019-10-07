// This is a script for the Organism Manager
// it provides function for generating (instancing) and regenerating animals and plants

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics; // TIME TESTING

namespace EcoBuilder.Archie{
    public class ArchieGenerator : ProceduralMeshGenerator
    {
        public GameObject producer;
        public GameObject consumer;

        public List<GameObject> set_of_organisms;
        public int seed;
        void Start()
        {
            set_of_organisms = new List<GameObject>();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                int seeder = UnityEngine.Random.Range(0, int.MaxValue);
                // seeder = 486193647;
                set_of_organisms.Add(GenerateSpecies(false, UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f), seeder));
                UnityEngine.Debug.Log(seeder);
                // set_of_organisms.Add(GenerateSpecies(false, UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f), 1511065024));
                // set_of_organisms.Add(GenerateSpecies(false, 1, 1, 0));
            }
            if (Input.GetKeyDown(KeyCode.P))
            {
                set_of_organisms.Add(GenerateSpecies(true, 1, 0.5f, UnityEngine.Random.Range(0, int.MaxValue)));
            }
            if (Input.GetKey(KeyCode.T))
            {
                RegenerateSpecies(set_of_organisms[set_of_organisms.Count - 1], UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0, int.MaxValue));
            }    
        }
        public override GameObject GenerateSpecies(bool isProducer, float bodySize, float greediness, int randomSeed, float population = -1)
        {
            UnityEngine.Random.InitState(randomSeed);

            GameObject species;
            GameObject reference;
            if (isProducer)
            {
                reference = producer;
            }
            else
            {
                reference = consumer;
            }
            species = Instantiate(reference, new Vector3(0, 0, 0), Quaternion.identity);
            
            if (isProducer)
            {
                // species.GetComponent<Plantwork>().Refresh(bodySize);
                // species.GetComponent<generate_texture>().Refresh(bodySize);
                // species.transform.Find("pot").gameObject.GetComponent<generate_texture>().ColorTexture(greediness, true, true);
            }
            else
            {
                var animal_structure = species.transform.Find("bones").gameObject;
                animal_structure.GetComponent<Bonework>().Refresh(bodySize, true);
                animal_structure.GetComponent<generate_texture>().Refresh(bodySize);
                animal_structure.GetComponent<generate_texture>().ColorTexture(greediness);
            }
            return species;
        }

        public override void RegenerateSpecies(GameObject species, float size, float greed, int randomSeed, float population = -1) 
        {
            UnityEngine.Random.InitState(randomSeed);

            if (species.GetComponent<Plantwork>() == null)
            {
                var animal_structure = species.transform.Find("bones").gameObject;

                Stopwatch bone_time = new Stopwatch(); // TIME TESTING
                bone_time.Start();// TIME TESTING
                animal_structure.GetComponent<Bonework>().Refresh(size, false);
                bone_time.Stop(); // TIME TESTING
                UnityEngine.Debug.Log("bone time: " + bone_time.Elapsed); // TIME TESTING

                Stopwatch texture_time = new Stopwatch(); // TIME TESTING
                texture_time.Start();// TIME TESTING
                animal_structure.GetComponent<generate_texture>().Refresh(size, false);
                texture_time.Stop(); // TIME TESTING
                UnityEngine.Debug.Log("texture time: " + texture_time.Elapsed); // TIME TESTING

                animal_structure.GetComponent<generate_texture>().ColorTexture(greed, false);
            }
            else
            {
                species.GetComponent<Plantwork>().Refresh(size, false);
                species.GetComponent<generate_texture>().Refresh(size, false);
                species.transform.Find("pot").gameObject.GetComponent<generate_texture>().ColorTexture(greed, false, true);
            }
        }
    }
}
