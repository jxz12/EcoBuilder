using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace EcoBuilder.UI
{
    public class Species
    {
        public int Idx { get; private set; }
        public int RandomSeed { get; private set; } 

        public bool IsProducer { get; set; }
        public float BodySize { get; set; }
        public float Greediness { get; set; }

        public GameObject Object { get; set; }
        public bool Spawned { get; set; }

        public Species(int idx, bool isProducer, float size, float greed)
        {
            Idx = idx;
            IsProducer = isProducer;
            BodySize = size;
            Greediness = greed;
            RandomSeed = UnityEngine.Random.Range(0, int.MaxValue);
            Spawned = false;
        }
    }

    // handles keeping track of species gameobjects and stats
    public class Inspector : MonoBehaviour, IDragHandler, IPointerClickHandler
    {
        // give gameobject once, keep a reference here so that it can be changed from here?
        public event Action<int, GameObject> OnSpawned;
        public event Action<int, bool> OnProducerSet;
        public event Action<int, float> OnSizeSet;
        public event Action<int, float> OnGreedSet;

        [SerializeField] Button producerButton;
        [SerializeField] Button consumerButton;
        [SerializeField] Slider sizeSlider;
        [SerializeField] Slider greedSlider;

        [SerializeField] Text nameText;
        [SerializeField] Button refreshButton;
        [SerializeField] Transform incubator;

        Dictionary<int, Species> spawnedSpecies = new Dictionary<int, Species>();
        int nextIdx = 0;
        Species inspected = null;

        void Start()
        {
            producerButton.onClick.AddListener(()=>IncubateNew(true));
            consumerButton.onClick.AddListener(()=>IncubateNew(false));

            sizeSlider.onValueChanged.AddListener(x=> SetInspectedSize(x));
            greedSlider.onValueChanged.AddListener(x=> SetInspectedGreed(x));

            refreshButton.onClick.AddListener(()=>RefreshInspected());
        }

        [SerializeField] JonnyGenerator factory;
        public GameObject GenerateSpeciesObject(Species s)
        {
            return factory.GenerateSpecies(s.IsProducer, s.BodySize, s.Greediness, s.RandomSeed);
        }
        public void RegenerateInspectedObject()
        {
            print("not yet");
        }
        void RefreshInspected()
        {
            print("not yet either");
        }
        void IncubateNew(bool isProducer)
        {
            Species s = new Species(nextIdx, isProducer, sizeSlider.value, greedSlider.value);
            s.Object = GenerateSpeciesObject(s);
            s.Object.transform.SetParent(incubator, false);
            nameText.name = s.Object.name;

            inspected = s;
        }
        void SetInspectedSize(float bodySize)
        {
            if (inspected == null)
                throw new Exception("nothing inspected");

            inspected.BodySize = bodySize;
            RegenerateInspectedObject();
            if (inspected.Spawned)
                OnSizeSet.Invoke(inspected.Idx, inspected.BodySize);
        }
        void SetInspectedGreed(float greed)
        {
            if (inspected == null)
                throw new Exception("nothing inspected");

            inspected.Greediness = greed;
            RegenerateInspectedObject();
            if (inspected.Spawned)
                OnGreedSet.Invoke(inspected.Idx, inspected.Greediness);
        }
        public void InspectSpecies(int idx)
        {
            if (inspected != null && !inspected.Spawned)
            {
                Destroy(inspected.Object);
                spawnedSpecies.Remove(inspected.Idx); // lol memory leak but not really
            }
            inspected = spawnedSpecies[idx];

            nameText.text = inspected.Object.name;
            // find a way to set the sliders without triggering events please

            GetComponent<Animator>().SetTrigger("Inspect");
        }
        public void Uninspect()
        {
            if (inspected != null && !inspected.Spawned)
            {
                Destroy(inspected.Object);
                spawnedSpecies.Remove(inspected.Idx); // lol memory leak but not really
            }
            GetComponent<Animator>().SetTrigger("Uninspect");
        }
        void Spawn()
        {
            if (inspected == null)
                throw new Exception("nothing inspected");
            if (inspected.Spawned)
                throw new Exception("already spawned");
            
            OnSpawned.Invoke(inspected.Idx, inspected.Object);
            OnProducerSet.Invoke(inspected.Idx, inspected.IsProducer);
            OnSizeSet.Invoke(inspected.Idx, inspected.BodySize);
            OnGreedSet.Invoke(inspected.Idx, inspected.Greediness);

            inspected.Spawned = true;
            spawnedSpecies[inspected.Idx] = inspected;
            nextIdx += 1;

            GetComponent<Animator>().SetTrigger("Spawn");
        }
        public void OnDrag(PointerEventData ped)
        {

        }
        public void OnPointerClick(PointerEventData ped)
        {
            if (inspected != null && !inspected.Spawned)
                Spawn();
        }
    }
}