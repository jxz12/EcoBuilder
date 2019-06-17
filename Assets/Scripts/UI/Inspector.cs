using System;
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
        public event Action<int, bool> OnProducerSet;
        public event Action<int, float> OnSizeSet;
        public event Action<int, float> OnGreedSet;
        public event Action OnGameFinished;
        public event Action OnMainMenu;

        public event Action OnIncubated;
        public event Action OnUnincubated;

        [SerializeField] Button goButton;
        [SerializeField] Button producerButton;
        [SerializeField] Button consumerButton;
        [SerializeField] Text producerCount;
        [SerializeField] Text consumerCount;
        [SerializeField] Slider sizeSlider;
        [SerializeField] Slider greedSlider;
        [SerializeField] Button menuButton;

        [SerializeField] Text nameText;
        [SerializeField] Button refreshButton;
        [SerializeField] RectTransform incubatedParent;

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

        Dictionary<int, Species> spawnedSpecies = new Dictionary<int, Species>();
        int nextIdx = 0;
        Species inspected = null;

        void Start()
        {
            goButton.onClick.AddListener(()=> Go());
            producerButton.onClick.AddListener(()=> IncubateNew(true));
            producerButton.onClick.AddListener(()=> GetComponent<Animator>().SetTrigger("Incubate"));
            consumerButton.onClick.AddListener(()=> IncubateNew(false));
            consumerButton.onClick.AddListener(()=> GetComponent<Animator>().SetTrigger("Incubate"));
            refreshButton.onClick.AddListener(()=> RefreshIncubated());
            menuButton.onClick.AddListener(()=> OnMainMenu.Invoke());

            sizeSlider.onValueChanged.AddListener(x=> SetInspectedSize());
            greedSlider.onValueChanged.AddListener(x=> SetInspectedGreed());
        }

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


        [SerializeField] JonnyGenerator factory;
        
        void Go()
        {
            if (numProducers==maxProducers && numConsumers==maxConsumers)
            {
                OnGameFinished.Invoke();
                // GetComponent<Animator>().SetTrigger("Finish");
            }
            else
            {
                GetComponent<Animator>().SetTrigger("Go");
            }
        }
        void IncubateNew(bool isProducer)
        {
            Species s = new Species(nextIdx, isProducer, sizeSlider.normalizedValue, greedSlider.normalizedValue);
            s.Object = factory.GenerateSpecies(s.IsProducer, s.BodySize, s.Greediness, s.RandomSeed);
            s.Object.transform.SetParent(incubatedParent, false);
            nameText.text = s.Object.name;

            inspected = s;
            OnIncubated.Invoke();
        }
        void RefreshIncubated()
        {
            if (inspected == null || inspected.Spawned)
                throw new Exception("nothing incubated");

            Destroy(inspected.Object);
            inspected = new Species(inspected.Idx, inspected.IsProducer, sizeSlider.normalizedValue, greedSlider.normalizedValue);
            inspected.Object = factory.GenerateSpecies(inspected.IsProducer, inspected.BodySize, inspected.Greediness, inspected.RandomSeed);
            inspected.Object.transform.SetParent(incubatedParent, false);
            nameText.text = inspected.Object.name;
        }
        void SetInspectedSize()
        {
            if (inspected == null)
                throw new Exception("nothing inspected");

            inspected.BodySize = sizeSlider.normalizedValue;
            factory.RegenerateSpecies(inspected.Object, inspected.IsProducer, inspected.BodySize, inspected.Greediness, inspected.RandomSeed);
            if (inspected.Spawned)
                OnSizeSet.Invoke(inspected.Idx, inspected.BodySize);
        }
        void SetInspectedGreed()
        {
            if (inspected == null)
                throw new Exception("nothing inspected");

            inspected.Greediness = greedSlider.normalizedValue;
            factory.RegenerateSpecies(inspected.Object, inspected.IsProducer, inspected.BodySize, inspected.Greediness, inspected.RandomSeed);
            if (inspected.Spawned)
                OnGreedSet.Invoke(inspected.Idx, inspected.Greediness);
        }
        public void InspectSpecies(int idx)
        {
            if (inspected != null && !inspected.Spawned) // if incubating
            {
                Destroy(inspected.Object);
                OnUnincubated.Invoke();
            }

            inspected = spawnedSpecies[idx];

            nameText.text = inspected.Object.name;

            // this is ugly as heck, but sliders are stupid
            sizeSlider.onValueChanged.RemoveListener(x=> SetInspectedSize());
            sizeSlider.normalizedValue = inspected.BodySize;
            sizeSlider.onValueChanged.AddListener(x=> SetInspectedSize());
            greedSlider.onValueChanged.RemoveListener(x=> SetInspectedGreed());
            greedSlider.normalizedValue = inspected.Greediness;
            greedSlider.onValueChanged.AddListener(x=> SetInspectedGreed());

            GetComponent<Animator>().SetTrigger("Inspect");
        }
        public void Uninspect()
        {
            if (inspected != null)
            {
                if (!inspected.Spawned)
                {
                    Destroy(inspected.Object);
                    OnUnincubated.Invoke();
                }
                inspected = null; // lol memory leak but not really
                GetComponent<Animator>().SetTrigger("Uninspect");
            }
        }
        public void TrySpawnNew()
        {
            if (!dragging)
                return;

            if (inspected == null)
                throw new Exception("nothing inspected");
            if (inspected.Spawned)
                throw new Exception("inspected already spawned");
            
            OnSpawned.Invoke(inspected.Idx, inspected.Object); // must be invoked first

            OnProducerSet.Invoke(inspected.Idx, inspected.IsProducer);
            OnSizeSet.Invoke(inspected.Idx, inspected.BodySize);
            OnGreedSet.Invoke(inspected.Idx, inspected.Greediness);

            inspected.Spawned = true;
            spawnedSpecies[inspected.Idx] = inspected;
            nextIdx += 1;

            GetComponent<Animator>().SetTrigger("Spawn");
            // OnUnincubated.Invoke();

            if (inspected.IsProducer)
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
            // TODO: do this better with animation
            if (numProducers==maxProducers && numConsumers==maxConsumers)
            {
                goImage.color = Color.white;
                goImage.sprite = finishFlag;
                // GetComponent<Animator>().SetTrigger("IdleFinish");
            }
        }
        [SerializeField] Image goImage;
        [SerializeField] Sprite finishFlag;

        public void UnspawnSpecies(int idx)
        {
            if (spawnedSpecies.Count > 0)
            {
                if (inspected == spawnedSpecies[idx])
                {
                    GetComponent<Animator>().SetTrigger("Uninspect");
                }
                spawnedSpecies.Remove(nextIdx);
            }
        }

        bool dragging;
        Vector2 originalPos;
        public void OnBeginDrag(PointerEventData ped)
        {
            if (inspected != null && !inspected.Spawned)
            {
                // inspected.Object.transform.SetParent(null, true);
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