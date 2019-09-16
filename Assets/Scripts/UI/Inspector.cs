using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace EcoBuilder.UI
{
    // handles keeping track of species gameobjects and stats
    public class Inspector : MonoBehaviour
    {
        // give gameobject once, keep a reference here so that it can be changed from here?
        public event Action<int, GameObject> OnSpawned;
        public event Action<int, bool> OnIsProducerSet;
        public event Action<int, float> OnSizeSet;
        public event Action<int, float> OnGreedSet;
        public event Action<int> OnDespawned;

        public event Action OnIncubated;
        public event Action OnUnincubated;

        [SerializeField] Button producerButton;
        [SerializeField] Button consumerButton;
        [SerializeField] Slider sizeSlider;
        [SerializeField] Slider greedSlider;

        [SerializeField] Text nameText;
        [SerializeField] Button refroveButton;
        [SerializeField] Animator infoAnimator, typeAnimator;
        
        [SerializeField] JonnyGenerator.JonnyGenerator factory;
        // [SerializeField] Archie.ArchieGenerator factory;
        [SerializeField] Incubator incubator;

        public class Species
        {
            public int Idx { get; private set; }
            public int RandomSeed { get; private set; } 

            public bool IsProducer { get; set; }
            public float BodySize { get; set; }
            public float Greediness { get; set; }

            // TODO: separate this into greed and size?
            public bool Editable { get; set; } = true;
            public bool Removable { get; set; } = true;
            public GameObject GObject { get; set; } = null;

            public Species(int idx, bool isProducer)
            {
                Idx = idx;
                IsProducer = isProducer;
                RerollSeed();
            }
            // to set seed from file
            public Species(int idx, bool isProducer, float size, float greed, int seed)
                : this(idx, isProducer)
            {
                BodySize = size;
                Greediness = greed;
                RandomSeed = seed;
            }

            // for refreshing a species
            public void RerollSeed()
            {
                BodySize = UnityEngine.Random.Range(0, 1f);
                Greediness = UnityEngine.Random.Range(0, 1f);
                RandomSeed = UnityEngine.Random.Range(0, int.MaxValue);
            }
        }

        Dictionary<int, Species> spawnedSpecies = new Dictionary<int, Species>();
        Dictionary<int, Species> graveyard = new Dictionary<int, Species>();
        int nextIdx = 0;
        Species incubated = null, inspected = null;

        void Start()
        {
            producerButton.onClick.AddListener(()=> IncubateNew(true));
            consumerButton.onClick.AddListener(()=> IncubateNew(false));
            refroveButton.onClick.AddListener(()=> RefreshOrRemove());

            SizeSliderCallback = x=> SetSize();
            GreedSliderCallback = x=> SetGreed();
            sizeSlider.onValueChanged.AddListener(SizeSliderCallback);
            greedSlider.onValueChanged.AddListener(GreedSliderCallback);

            incubator.OnSpawned += ()=> SpawnIncubated();
        }
        UnityAction<float> SizeSliderCallback, GreedSliderCallback;


        void Spawn(Species toSpawn)
        {
            spawnedSpecies[toSpawn.Idx] = toSpawn;
            OnSpawned.Invoke(toSpawn.Idx, toSpawn.GObject); // must be invoked first

            OnIsProducerSet.Invoke(toSpawn.Idx, toSpawn.IsProducer);
            OnSizeSet.Invoke(toSpawn.Idx, toSpawn.BodySize);
            OnGreedSet.Invoke(toSpawn.Idx, toSpawn.Greediness);
            nextIdx += 1;
        }
        void Despawn(Species toDespawn)
        {
            if (!spawnedSpecies.ContainsKey(toDespawn.Idx))
            {
                throw new Exception("idx not spawned");
            }

            if (inspected == toDespawn)
            {
                infoAnimator.SetTrigger("Uninspect");
                inspected = null;
            }
            spawnedSpecies.Remove(toDespawn.Idx);
            graveyard.Add(toDespawn.Idx, toDespawn);

            // take back GameObject before nodelink destroys it
            toDespawn.GObject.transform.SetParent(transform);
            toDespawn.GObject.SetActive(false);
        }

        void IncubateNew(bool isProducer)
        {
            if (inspected != null)
            {
                inspected = null;
            }
            if (incubated != null)
            {
                incubator.Unincubate();
                incubated = null;
            }
            Species s = new Species(nextIdx, isProducer);
            s.GObject = factory.GenerateSpecies(s.IsProducer, s.BodySize, s.Greediness, s.RandomSeed);
            s.Editable = true;
            s.Removable = true;
            incubator.Incubate(s.GObject);
            nameText.text = s.GObject.name;

            SetSlidersWithoutEventCallbacks(s.BodySize, s.Greediness);
            sizeSlider.interactable = true;
            greedSlider.interactable = true;
            refroveButton.interactable = true;

            incubated = s;
            infoAnimator.SetTrigger("Incubate");
            typeAnimator.SetBool("Visible", false);
            OnIncubated.Invoke();
        }
        void RefreshOrRemove()
        {
            if (incubated != null)
            {
                incubated.RerollSeed();
                SetSlidersWithoutEventCallbacks(incubated.BodySize, incubated.Greediness);
                factory.RegenerateSpecies(incubated.GObject, incubated.BodySize, incubated.Greediness, incubated.RandomSeed);
                nameText.text = incubated.GObject.name;
            }
            else if (inspected != null)
            {
                if (!inspected.Removable)
                {
                    throw new Exception("inspected not removable");
                }
                else
                {
                    int despawnedIdx = inspected.Idx;
                    Despawn(inspected);
                    OnDespawned.Invoke(despawnedIdx);
                }
            }
            else
            {
                throw new Exception("nothing incubated or inspected");
            }
        }
        void SpawnIncubated()
        {
            if (incubated == null)
                throw new Exception("nothing incubated");

            Spawn(incubated);
            int spawnedIdx = incubated.Idx;
            incubated = null;
            OnUnincubated.Invoke();

            InspectSpecies(spawnedIdx);
            typeAnimator.SetBool("Visible", true);
        }

        void SetSize()
        {
            if (incubated != null)
            {
                incubated.BodySize = sizeSlider.normalizedValue;
                factory.RegenerateSpecies(incubated.GObject, incubated.BodySize, incubated.Greediness, incubated.RandomSeed);
            }
            else if (inspected != null)
            {
                inspected.BodySize = sizeSlider.normalizedValue;
                factory.RegenerateSpecies(inspected.GObject, inspected.BodySize, inspected.Greediness, inspected.RandomSeed);
                OnSizeSet.Invoke(inspected.Idx, inspected.BodySize);
            }
        }
        void SetGreed()
        {
            if (incubated != null)
            {
                incubated.Greediness = greedSlider.normalizedValue;
                factory.RegenerateSpecies(incubated.GObject, incubated.BodySize, incubated.Greediness, incubated.RandomSeed);
            }
            else if (inspected != null)
            {
                inspected.Greediness = greedSlider.normalizedValue;
                factory.RegenerateSpecies(inspected.GObject, inspected.BodySize, inspected.Greediness, inspected.RandomSeed);
                OnGreedSet.Invoke(inspected.Idx, inspected.Greediness);
            }
        }

        void SetSlidersWithoutEventCallbacks(float size, float greed)
        {
            // this is ugly as heck, but sliders are stupid
            sizeSlider.onValueChanged.RemoveListener(SizeSliderCallback);
            sizeSlider.normalizedValue = size;
            sizeSlider.onValueChanged.AddListener(SizeSliderCallback);
            greedSlider.onValueChanged.RemoveListener(GreedSliderCallback);
            greedSlider.normalizedValue = greed;
            greedSlider.onValueChanged.AddListener(GreedSliderCallback);
        }













        /////////////////////
        // external stuff

        public void InspectSpecies(int idx)
        {
            if (inspected == null)
            {
                infoAnimator.SetTrigger("Inspect");
            }
            if (incubated != null)
            {
                typeAnimator.SetBool("Visible", true);
                incubator.Unincubate();
                incubated = null;
                OnUnincubated.Invoke();
            }

            inspected = spawnedSpecies[idx];
            nameText.text = inspected.GObject.name;
            SetSlidersWithoutEventCallbacks(inspected.BodySize, inspected.Greediness);

            sizeSlider.interactable = greedSlider.interactable
                                    = inspected.Editable;
            refroveButton.interactable = inspected.Removable;
        }
        public void Unincubate()
        {
            if (incubated != null)
            {
                incubator.Unincubate();
                incubated = null;
                infoAnimator.SetTrigger("Unincubate");
                typeAnimator.SetBool("Visible", true);
            }
        }
        public void Uninspect()
        {
            if (inspected != null)
            {
                inspected = null;
                infoAnimator.SetTrigger("Uninspect");
            }
        }
        public void SetProducersAvailable(bool available)
        {
            producerButton.interactable = available;
        }
        public void SetConsumersAvailable(bool available)
        {
            consumerButton.interactable = available;
        }

        // for loading from level
        public int SpawnNotIncubated(bool isProducer, float size, float greed, int randomSeed, bool editable, bool removable=false)
        {
            if (inspected != null)
                throw new Exception("somehow inspecting??");
            if (size < 0 || size > 1)
                throw new Exception("size not in bounds");
            if (greed < 0 || greed > 1)
                throw new Exception("greed not in bounds");

            var toSpawn = new Species(nextIdx, isProducer, size, greed, randomSeed);
            toSpawn.Editable = editable;
            toSpawn.Removable = removable;
            toSpawn.GObject = factory.GenerateSpecies(isProducer, size, greed, randomSeed);
            
            Spawn(toSpawn);
            return toSpawn.Idx;
        }
        public void Respawn(int idx)
        {
            Spawn(graveyard[idx]);
            graveyard[idx].GObject.SetActive(true);
            graveyard.Remove(idx);
        }

        public void Hide()
        {
            if (incubated != null)
            {
                incubator.Unincubate();
                infoAnimator.SetTrigger("Unincubate");
            }
            else if (inspected != null)
            {
                infoAnimator.SetTrigger("Uninspect");
                typeAnimator.SetBool("Visible", false);
            }
        }
    }
}