using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace EcoBuilder.Archie
{
    public class animal_object : MonoBehaviour
    {
        [SerializeField] SkinnedMeshRenderer smr;
        [SerializeField] Animator anim;
        public SkinnedMeshRenderer Renderer { get { return smr; } }

        public Texture2D Eyes { get; set; }
        public bool IsPlant { get; set; }

        public void Die()
        {
            anim.SetTrigger("Die");
        }
        public void Live()
        {
            anim.SetTrigger("Live");
        }
        public void IdleAnimation()
        {
            anim.SetInteger("Which Cute", UnityEngine.Random.Range(0,2));
            anim.SetTrigger("Be Cute");
        }
        void OnEnable()
        {
            enabledAnimals.Add(this);
        }
        void OnDisable()
        {
            enabledAnimals.Remove(this);
        }
        static HashSet<animal_object> enabledAnimals = new HashSet<animal_object>();
        public static void RandomIdleAnimation()
        {
            int nSpecies = enabledAnimals.Count;
            if (nSpecies == 0) {
                return;
            }
            int choice = UnityEngine.Random.Range(0, nSpecies);
            enabledAnimals.ElementAt(choice).IdleAnimation();
        }
    }
}