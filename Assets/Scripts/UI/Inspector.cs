using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace EcoBuilder.UI
{
    class Species
    {
        public int Idx { get; private set; }
        public bool IsProducer { get; private set; }
        public Species(int idx, bool isProducer)
        {
            Idx = idx;
            IsProducer = isProducer;
            RandomSeed = UnityEngine.Random.Range(0, int.MaxValue);
        }
        public string Name { get; set; }
        public HashSet<int> resources { get; private set; } = new HashSet<int>();
        public float BodySize { get; set; } = .5f;
        public float Greediness { get; set; } = .5f;
        public int RandomSeed { get; set; } = 0;
    }

    // handles keeping track of species gameobjects and stats
    public class Inspector : MonoBehaviour
    {
        public event Action<GameObject> OnSpawned;
        public event Action<int, bool> OnProducerSet;
        public event Action<int, float> OnSizeChanged;
        public event Action<int, float> OnGreedChanged;

        [SerializeField] Text nameText;
        [SerializeField] Slider sizeSlider;
        [SerializeField] Slider greedSlider;
        [SerializeField] Button producerButton;
        [SerializeField] Button consumerButton;
        [SerializeField] Button refreshButton;

        [SerializeField] Spawner spawner;

        Dictionary<int, Species> allSpecies;

        void Start()
        {
            // sizeSlider.onValueChanged.AddListener(x=> OnSizeChanged.Invoke(x));
            // greedSlider.onValueChanged.AddListener(x=> OnGreedChanged.Invoke(x));
        }

        public void SetName(string name)
        {
            nameText.text = name;
        }
        public void SetSize(float size)
        {
            sizeSlider.value = size;
        }
        public void SetGreed(float greed)
        {
            greedSlider.value = greed;
        }
    }
}