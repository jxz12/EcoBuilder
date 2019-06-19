﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace EcoBuilder.UI
{
    // handles keeping track of species gameobjects and stats
    public class Inspector : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // give gameobject once, keep a reference here so that it can be changed from here?
        public event Action<int, GameObject> OnSpawned;
        public event Action<int, bool> OnIsProducerSet;
        public event Action<int, float> OnSizeSet;
        public event Action<int, float> OnGreedSet;

        // public event Action OnIncubated;
        // public event Action OnUnincubated;

        [SerializeField] Button goButton;
        [SerializeField] Button producerButton;
        [SerializeField] Button consumerButton;
        [SerializeField] Text producerCount;
        [SerializeField] Text consumerCount;
        [SerializeField] Slider sizeSlider;
        [SerializeField] Slider greedSlider;

        [SerializeField] Text nameText;
        [SerializeField] Button refreshButton;
        [SerializeField] RectTransform incubatedParent;

        [SerializeField] JonnyGenerator factory;

        public class Species
        {
            public int Idx { get; private set; }
            public int RandomSeed { get; private set; } 

            public bool IsProducer { get; set; }
            public float BodySize { get; set; }
            public float Greediness { get; set; }

            public GameObject GObject { get; set; }

            public Species(int idx, bool isProducer, float size, float greed)
            {
                Idx = idx;
                IsProducer = isProducer;
                BodySize = size;
                Greediness = greed;
                RandomSeed = UnityEngine.Random.Range(0, int.MaxValue);
            }
            // to set seed from file
            public Species(int idx, bool isProducer, float size, float greed, int seed)
                : this(idx, isProducer, size, greed)
            {
                RandomSeed = seed;
            }

            // for refreshing a species
            public void RerollSeed()
            {
                RandomSeed = UnityEngine.Random.Range(0, int.MaxValue);
            }
        }

        Dictionary<int, Species> spawnedSpecies = new Dictionary<int, Species>();
        int nextIdx = 0;
        Species incubated = null, inspected = null;

        void Start()
        {
            goButton.onClick.AddListener(()=> GetComponent<Animator>().SetTrigger("Go"));
            producerButton.onClick.AddListener(()=> IncubateNew(true));
            producerButton.onClick.AddListener(()=> GetComponent<Animator>().SetTrigger("Incubate"));
            consumerButton.onClick.AddListener(()=> IncubateNew(false));
            consumerButton.onClick.AddListener(()=> GetComponent<Animator>().SetTrigger("Incubate"));
            refreshButton.onClick.AddListener(()=> RefreshIncubated());

            sizeSlider.onValueChanged.AddListener(x=> SetSize());
            greedSlider.onValueChanged.AddListener(x=> SetGreed());
        }

        void IncubateNew(bool isProducer)
        {
            if (inspected != null)
            {
                inspected = null;
            }
            Species s = new Species(nextIdx, isProducer, sizeSlider.normalizedValue, greedSlider.normalizedValue);
            s.GObject = factory.GenerateSpecies(s.IsProducer, s.BodySize, s.Greediness, s.RandomSeed);
            s.GObject.transform.SetParent(incubatedParent, false);
            nameText.text = s.GObject.name;

            incubated = s;
            // OnIncubated.Invoke();
        }
        void Spawn(Species toSpawn)
        {
            spawnedSpecies[toSpawn.Idx] = toSpawn;
            OnSpawned.Invoke(toSpawn.Idx, toSpawn.GObject); // must be invoked first

            OnIsProducerSet.Invoke(toSpawn.Idx, toSpawn.IsProducer);
            OnSizeSet.Invoke(toSpawn.Idx, toSpawn.BodySize);
            OnGreedSet.Invoke(toSpawn.Idx, toSpawn.Greediness);

            if (toSpawn.IsProducer)
            {
                numProducers += 1;
                producerCount.text = (maxProducers-numProducers).ToString();
                if (numProducers >= maxProducers)
                    producerButton.interactable = false;
            }
            else
            {
                numConsumers += 1;
                consumerCount.text = (maxConsumers-numConsumers).ToString();
                if (numConsumers >= maxConsumers)
                    consumerButton.interactable = false;
            }
            if (numProducers==maxProducers && numConsumers==maxConsumers)
            {
                goButton.interactable = false;
            }
            nextIdx += 1;
        }

        void RefreshIncubated()
        {
            if (incubated == null)
                throw new Exception("nothing incubated");

            incubated.RerollSeed();
            factory.RegenerateSpecies(incubated.GObject, incubated.IsProducer, incubated.BodySize, incubated.Greediness, incubated.RandomSeed);
            nameText.text = incubated.GObject.name;
        }
        void SetSize()
        {
            if (incubated != null)
            {
                incubated.BodySize = sizeSlider.normalizedValue;
                factory.RegenerateSpecies(incubated.GObject, incubated.IsProducer, incubated.BodySize, incubated.Greediness, incubated.RandomSeed);
            }
            else if (inspected != null)
            {
                inspected.BodySize = sizeSlider.normalizedValue;
                factory.RegenerateSpecies(inspected.GObject, inspected.IsProducer, inspected.BodySize, inspected.Greediness, inspected.RandomSeed);
                OnSizeSet.Invoke(inspected.Idx, inspected.BodySize);
            }
        }
        void SetGreed()
        {
            if (incubated != null)
            {
                incubated.Greediness = greedSlider.normalizedValue;
                factory.RegenerateSpecies(incubated.GObject, incubated.IsProducer, incubated.BodySize, incubated.Greediness, incubated.RandomSeed);
            }
            else if (inspected != null)
            {
                inspected.Greediness = greedSlider.normalizedValue;
                factory.RegenerateSpecies(inspected.GObject, inspected.IsProducer, inspected.BodySize, inspected.Greediness, inspected.RandomSeed);
                OnSizeSet.Invoke(inspected.Idx, inspected.BodySize);
            }
        }














        /////////////////////
        // external stuff

        int numProducers=0, maxProducers=int.MaxValue, numConsumers=0, maxConsumers=int.MaxValue;
        public void ConstrainTypes(int producers, int consumers)
        {
            if (numProducers < 0 || numConsumers < 0)
                throw new Exception("Cannot have negative numbers of species");
            
            maxProducers = producers;
            maxConsumers = consumers;
            producerCount.text = maxProducers.ToString();
            consumerCount.text = maxConsumers.ToString();
        }

        // for loading from level
        public int SpawnNotIncubated(bool isProducer, float size, float greed, int randomSeed)
        {
            if (inspected != null)
                throw new Exception("somehow inspecting??");
            if (size < 0 || size > 1)
                throw new Exception("size not in bounds");
            if (greed < 0 || greed > 1)
                throw new Exception("greed not in bounds");

            var toSpawn = new Species(nextIdx, isProducer, size, greed, randomSeed);
            toSpawn.GObject = factory.GenerateSpecies(isProducer, size, greed, randomSeed);
            
            Spawn(toSpawn);
            return toSpawn.Idx;
        }
        public void TrySpawnIncubated()
        {
            // only allow when dragging the incubator
            if (!dragging)
                return;
            if (incubated == null)
                throw new Exception("nothing incubated");
            
            Spawn(incubated);

            GetComponent<Animator>().SetTrigger("Spawn");
            inspected = incubated;
            incubated = null;
            // OnUnincubated.Invoke();
        }
        public void UnspawnSpecies(int idx)
        {
            if (!spawnedSpecies.ContainsKey(idx))
                throw new Exception("idx not spawned");

            if (inspected == spawnedSpecies[idx])
            {
                GetComponent<Animator>().SetTrigger("Uninspect");
                inspected = null;
            }

            if (spawnedSpecies[idx].IsProducer)
            {
                if (numProducers == maxProducers)
                    producerButton.interactable = true;

                numProducers -= 1;
            }
            else
            {
                if (numConsumers == maxConsumers)
                    consumerButton.interactable = true;

                numConsumers -= 1;
            }
            goButton.interactable = true;

            spawnedSpecies.Remove(idx);
        }

        public void InspectSpecies(int idx)
        {
            if (incubated != null)
            {
                Destroy(incubated.GObject);
                incubated = null;
                // OnUnincubated.Invoke();
            }

            inspected = spawnedSpecies[idx];

            nameText.text = inspected.GObject.name;

            // this is ugly as heck, but sliders are stupid
            sizeSlider.onValueChanged.RemoveListener(x=> SetSize());
            sizeSlider.normalizedValue = inspected.BodySize;
            sizeSlider.onValueChanged.AddListener(x=> SetSize());
            greedSlider.onValueChanged.RemoveListener(x=> SetGreed());
            greedSlider.normalizedValue = inspected.Greediness;
            greedSlider.onValueChanged.AddListener(x=> SetGreed());

            GetComponent<Animator>().SetTrigger("Inspect");
        }
        public void Uninspect()
        {
            if (inspected != null)
            {
                inspected = null;
                GetComponent<Animator>().SetTrigger("Uninspect");
            }
            else if (incubated != null)
            {
                Destroy(incubated.GObject);
                incubated = null; // lol memory leak but not really
                GetComponent<Animator>().SetTrigger("Uninspect");
                // OnUnincubated.Invoke();
            }
        }






        //////////////////////
        // dragging incubated

        bool dragging;
        Vector2 originalPos;
        public void OnBeginDrag(PointerEventData ped)
        {
            if (incubated != null)
            {
                dragging = true;
                originalPos = incubatedParent.position;
            }
        }
        public void OnDrag(PointerEventData ped)
        {
            if (dragging)
            {
                Vector3 mousePos = ped.position;
                mousePos.z = 4;
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

                // incubatedParent.position = ped.position;
                incubatedParent.position = worldPos;
            }
        }
        public void OnEndDrag(PointerEventData ped)
        {
            if (dragging)
            {
                dragging = false;
                incubatedParent.position = originalPos;
            }
        }
		public void Finish()
		{
			GetComponent<Animator>().SetTrigger("Finish");
		}
    }
}