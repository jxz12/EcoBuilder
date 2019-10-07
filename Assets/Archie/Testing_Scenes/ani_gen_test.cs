using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EcoBuilder.Archie
{
    public class ani_gen_test : MonoBehaviour
    {
        //Testing Fields
        public bool Producer = false;
        public float Size;
        public float Greed;
        public int Seed;
        // private animal_generator genny;
        private animal_generator anne;
        void Awake()
        {
            anne = GetComponent<animal_generator>();

        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.U))
            {
                anne.GenerateSpecies(Producer, Size, Greed, Seed);
            }
        //     if (Input.GetKeyDown(KeyCode.B))
        //     {
        //         anne.RegenerateSpecies(anne.generated_consumers[anne.generated_consumers.Count - 1], Size, Greed, Seed);
        //     }
        }
    }
}