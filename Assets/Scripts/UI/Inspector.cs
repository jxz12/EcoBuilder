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
        public event Action<int, bool> OnIsProducerSet;
        public event Action<int, float> OnSizeSet;
        public event Action<int, float> OnGreedSet;

        [SerializeField] Button plusButton;
        [SerializeField] Button producerButton;
        [SerializeField] Button consumerButton;
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

            // TODO: separate this into greed and size?
            public bool Editable { get; set; } = true;
            public GameObject GObject { get; set; } = null;

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
            plusButton.onClick.AddListener(()=> GetComponent<Animator>().SetTrigger("Start"));
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

            sizeSlider.interactable = true;
            greedSlider.interactable = true;

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
                OnGreedSet.Invoke(inspected.Idx, inspected.Greediness);
            }
        }














        /////////////////////
        // external stuff

        // for loading from level
        public int SpawnNotIncubated(bool isProducer, float size, float greed, int randomSeed, bool editable)
        {
            if (inspected != null)
                throw new Exception("somehow inspecting??");
            if (size < 0 || size > 1)
                throw new Exception("size not in bounds");
            if (greed < 0 || greed > 1)
                throw new Exception("greed not in bounds");

            var toSpawn = new Species(nextIdx, isProducer, size, greed, randomSeed);
            toSpawn.Editable = editable;
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
            GetComponent<Animator>().SetBool("All Spawned", false);

            spawnedSpecies.Remove(idx);
        }
        public void SetProducersAvailable(bool available)
        {
            if (available)
            {
                producerButton.interactable = true;
                GetComponent<Animator>().SetBool("All Spawned", false);
            }
            else
            {
                producerButton.interactable = false;
                if (consumerButton.interactable == false)
                    GetComponent<Animator>().SetBool("All Spawned", true);
            }
        }
        public void SetConsumersAvailable(bool available)
        {
            if (available)
            {
                consumerButton.interactable = true;
                GetComponent<Animator>().SetBool("All Spawned", false);
            }
            else
            {
                consumerButton.interactable = false;
                if (producerButton.interactable == false)
                    GetComponent<Animator>().SetBool("All Spawned", true);
            }
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

            if (inspected.Editable)
            {
                sizeSlider.interactable = true;
                greedSlider.interactable = true;
            }
            else
            {
                sizeSlider.interactable = false;
                greedSlider.interactable = false;
            }

            GetComponent<Animator>().SetTrigger("Inspect");
        }
        public void Uninspect()
        {
            GetComponent<Animator>().SetTrigger("Uninspect");
            if (inspected != null)
            {
                inspected = null;
            }
            else if (incubated != null)
            {
                Destroy(incubated.GObject);
                incubated = null; // lol memory leak but not really
            }
        }
        public void Hide()
        {
            GetComponent<Animator>().SetTrigger("Hide");
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

        // for saving to csv
        public Tuple<List<int>, List<int>, List<float>, List<float>> GetSpeciesTraits()
        {
            var idxs = new List<int>();
            var seeds = new List<int>();
            var sizes = new List<float>();
            var greeds = new List<float>();

            foreach (Species s in spawnedSpecies.Values)
            {
                idxs.Add(s.Idx);
                seeds.Add(s.RandomSeed);
                sizes.Add(s.BodySize);
                greeds.Add(s.Greediness);
            }
            return Tuple.Create(idxs, seeds, sizes, greeds);
        }
    }
}