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
        public event Action<int, GameObject> OnShaped;
        public event Action<int, bool> OnIsProducerSet;
        public event Action<int, float> OnSizeSet;
        public event Action<int, float> OnGreedSet;
        public event Action<int> OnSpawned;
        public event Action<int> OnDespawned;

        public event Action<int, float, float> OnUserSizeSet;
        public event Action<int, float, float> OnUserGreedSet;
        public event Action<int> OnUserSpawned;
        public event Action<int> OnUserDespawned;

        public event Action OnIncubated;
        public event Action OnUnincubated;

        [SerializeField] Button producerButton;
        [SerializeField] Button consumerButton;
        [SerializeField] Slider sizeSlider;
        [SerializeField] Slider greedSlider;

        [SerializeField] Text nameText;
        [SerializeField] Button refroveButton;
        [SerializeField] Animator typesAnim;
        
        [SerializeField] Incubator incubator;
        [SerializeField] ProceduralMeshGenerator factory;

        public class Species
        {
            public int Idx { get; private set; }
            public int RandomSeed { get; private set; } 

            public bool IsProducer { get; set; }
            public float BodySize { get; set; } = -1;
            public float Greediness { get; set; } = -1;

            public bool SizeEditable { get; set; } = true;
            public bool GreedEditable { get; set; } = true;
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
                RandomSeed = UnityEngine.Random.Range(0, int.MaxValue);
                BodySize = UnityEngine.Random.Range(0, 1f);
                Greediness = UnityEngine.Random.Range(0, 1f);
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

            SizeSlidCallback = x=> SetSizeFromSlider();
            GreedSlidCallback = x=> SetGreedFromSlider();
            sizeSlider.onValueChanged.AddListener(SizeSlidCallback);
            greedSlider.onValueChanged.AddListener(GreedSlidCallback);
            // sizeSlider.OnPointerUp += ()=>print("hi");

            incubator.OnDropped += ()=> SpawnIncubated();
        }

        void SpawnWithEvents(Species toSpawn)
        {
            spawnedSpecies[toSpawn.Idx] = toSpawn;
            OnSpawned.Invoke(toSpawn.Idx); // must be invoked first

            OnShaped.Invoke(toSpawn.Idx, toSpawn.GObject);
            OnIsProducerSet.Invoke(toSpawn.Idx, toSpawn.IsProducer);
            OnSizeSet.Invoke(toSpawn.Idx, toSpawn.BodySize);
            OnGreedSet.Invoke(toSpawn.Idx, toSpawn.Greediness);
        }
        void Bury(Species toBury)
        {
            spawnedSpecies.Remove(toBury.Idx);
            graveyard.Add(toBury.Idx, toBury);

            OnDespawned.Invoke(toBury.Idx); // must be invoked last
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
            while (spawnedSpecies.ContainsKey(nextIdx) || graveyard.ContainsKey(nextIdx))
                nextIdx += 1;

            Species s = new Species(nextIdx, isProducer);
            if (initialSizeIfFixed >= 0)
                s.BodySize = initialSizeIfFixed;
            if (initialGreedIfFixed >= 0)
                s.Greediness = initialGreedIfFixed;

            // this is to match the whole numbers in slider
            SetSlidersWithoutCallbacks(s.BodySize, s.Greediness);
            s.BodySize = sizeSlider.normalizedValue;
            s.Greediness = greedSlider.normalizedValue; 

            // grab gameobject from factory
            s.GObject = factory.GenerateSpecies(s.IsProducer, s.BodySize, s.Greediness, s.RandomSeed);
            incubator.Incubate(s.GObject);
            nameText.text = s.GObject.name;

            sizeSlider.interactable = true;
            greedSlider.interactable = true;
            refroveButton.interactable = true;
            refroveButton.gameObject.SetActive(true);

            incubated = s;
            GetComponent<Animator>().SetTrigger("Incubate");
            typesAnim.SetBool("Visible", false);
            OnIncubated.Invoke();
        }
        void RefreshOrRemove() // only called on inspected or incubated
        {
            if (incubated != null)
            {
                incubated.RerollSeed();
                if (initialSizeIfFixed >= 0)
                    incubated.BodySize = initialSizeIfFixed;
                if (initialGreedIfFixed >= 0)
                    incubated.Greediness = initialGreedIfFixed;

                // this is to match the whole numbers in slider
                SetSlidersWithoutCallbacks(incubated.BodySize, incubated.Greediness);
                incubated.BodySize = sizeSlider.normalizedValue;
                incubated.Greediness = greedSlider.normalizedValue; 

                // incubated.GObject = factory.GenerateSpecies(incubated.IsProducer, incubated.BodySize, incubated.Greediness, incubated.RandomSeed);
                // incubator.Replace(incubated.GObject);
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
                    Bury(inspected);
                    OnUserDespawned.Invoke(inspected.Idx);
                    inspected = null;
                    GetComponent<Animator>().SetTrigger("Uninspect");
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

            SpawnWithEvents(incubated);
            int spawnedIdx = incubated.Idx;
            incubated = null;

            OnUnincubated.Invoke();
            OnUserSpawned.Invoke(spawnedIdx);

            InspectSpecies(spawnedIdx);
            typesAnim.SetBool("Visible", true);
        }

        private bool CheckConflict(Species toCheck)
        {
            foreach (Species s in spawnedSpecies.Values)
            {
                if (s != toCheck &&
                    s.Greediness == toCheck.Greediness &&
                    s.BodySize == toCheck.BodySize)
                {
                    return true;
                }
            }
            return false;
        }
        void SetSizeFromSlider()
        {
            if (incubated != null)
            {
                float prevSize = incubated.BodySize;
                incubated.BodySize = sizeSlider.normalizedValue;
                if (CheckConflict(incubated))
                    print("BOO");

                factory.RegenerateSpecies(incubated.GObject, incubated.BodySize, incubated.Greediness, incubated.RandomSeed);
                nameText.text = incubated.GObject.name;
            }
            else if (inspected != null)
            {
                float prevSize = inspected.BodySize;
                inspected.BodySize = sizeSlider.normalizedValue;
                if (CheckConflict(inspected))
                    print("BOO");

                OnSizeSet.Invoke(inspected.Idx, inspected.BodySize);
                OnUserSizeSet.Invoke(inspected.Idx, prevSize, inspected.BodySize);

                factory.RegenerateSpecies(inspected.GObject, inspected.BodySize, inspected.Greediness, inspected.RandomSeed);
                nameText.text = inspected.GObject.name;
            }
        }
        void SetGreedFromSlider()
        {
            if (incubated != null)
            {
                float prevGreed = incubated.Greediness;
                incubated.Greediness = greedSlider.normalizedValue;
                if (CheckConflict(incubated))
                    print("BOO");

                factory.RegenerateSpecies(incubated.GObject, incubated.BodySize, incubated.Greediness, incubated.RandomSeed);
                nameText.text = incubated.GObject.name;
            }
            else if (inspected != null)
            {
                float prevGreed = inspected.Greediness;
                inspected.Greediness = greedSlider.normalizedValue;
                if (CheckConflict(inspected))
                    print("BOO");

                OnGreedSet.Invoke(inspected.Idx, inspected.Greediness);
                OnUserGreedSet.Invoke(inspected.Idx, prevGreed, inspected.Greediness);

                factory.RegenerateSpecies(inspected.GObject, inspected.BodySize, inspected.Greediness, inspected.RandomSeed);
                nameText.text = inspected.GObject.name;
            }
        }

        UnityAction<float> SizeSlidCallback, GreedSlidCallback;
        void SetSlidersWithoutCallbacks(float size, float greed)
        {
            // this is ugly as heck, but sliders are stupid
            sizeSlider.onValueChanged.RemoveListener(SizeSlidCallback);
            sizeSlider.normalizedValue = size;
            sizeSlider.onValueChanged.AddListener(SizeSlidCallback);
            greedSlider.onValueChanged.RemoveListener(GreedSlidCallback);
            greedSlider.normalizedValue = greed;
            greedSlider.onValueChanged.AddListener(GreedSlidCallback);
        }





        /////////////////////
        // public stuff

        public void InspectSpecies(int idx)
        {
            if (inspected == null)
            {
                GetComponent<Animator>().SetTrigger("Inspect");
            }
            if (incubated != null)
            {
                typesAnim.SetBool("Visible", true);
                incubator.Unincubate();
                incubated = null;
                OnUnincubated.Invoke();
            }

            inspected = spawnedSpecies[idx];
            nameText.text = inspected.GObject.name;
            SetSlidersWithoutCallbacks(inspected.BodySize, inspected.Greediness);

            sizeSlider.interactable  = inspected.SizeEditable;
            greedSlider.interactable = inspected.GreedEditable;
            refroveButton.interactable = inspected.Removable;

            refroveButton.gameObject.SetActive(removeEnabled);
        }
        public void Unincubate()
        {
            if (incubated != null)
            {
                incubator.Unincubate();
                incubated = null;
                GetComponent<Animator>().SetTrigger("Unincubate");
                typesAnim.SetBool("Visible", true);
                OnUnincubated.Invoke();
            }
        }
        public void Uninspect()
        {
            if (inspected != null)
            {
                inspected = null;
                GetComponent<Animator>().SetTrigger("Uninspect");
            }
        }
        public void SetProducerAvailability(bool available)
        {
            producerButton.interactable = available;
        }
        public void SetConsumerAvailability(bool available)
        {
            consumerButton.interactable = available;
        }
        public void SetSpeciesRemovable(int idx, bool removable)
        {
            spawnedSpecies[idx].Removable = removable;
        }

        // for loading from level
        public void SpawnNotIncubated(int idx, bool isProducer, float size, float greed, int randomSeed, bool sizeEditable, bool greedEditable)
        {
            if (spawnedSpecies.ContainsKey(idx) || graveyard.ContainsKey(idx))
                throw new Exception("idx already added");
            if (inspected != null)
                throw new Exception("somehow inspecting??");
            if (size < 0 || size > 1)
                throw new Exception("size not in bounds");
            if (greed < 0 || greed > 1)
                throw new Exception("greed not in bounds");

            var toSpawn = new Species(idx, isProducer, size, greed, randomSeed);
            toSpawn.SizeEditable = sizeEditable;
            toSpawn.GreedEditable = greedEditable;
            toSpawn.GObject = factory.GenerateSpecies(isProducer, size, greed, randomSeed);
            
            SpawnWithEvents(toSpawn);
        }

        // for un/redo
        public void Despawn(int idx)
        {
            if (!spawnedSpecies.ContainsKey(idx))
                throw new Exception("idx not spawned");

            if (spawnedSpecies[idx] == inspected)
            {
                GetComponent<Animator>().SetTrigger("Uninspect");
                inspected = null;
            }
            Bury(spawnedSpecies[idx]);
        }
        public void DespawnCompletely(int idx)
        {
            if (!graveyard.ContainsKey(idx))
                throw new Exception("idx not in graveyard");

            Destroy(graveyard[idx].GObject); // TODO: check if null?
            graveyard.Remove(idx);
        }
        public void Respawn(int idx)
        {
            if (!graveyard.ContainsKey(idx))
            {
                throw new Exception("idx not in graveyard");
            }
            graveyard[idx].GObject.SetActive(true);
            SpawnWithEvents(graveyard[idx]);
            graveyard.Remove(idx);
        }
        public void SetIsProducer(int idx, bool isProducer)
        {
            if (!spawnedSpecies.ContainsKey(idx))
            {
                throw new Exception("idx not spawned");
            }
            Species s = spawnedSpecies[idx];
            s.IsProducer = isProducer;
            OnIsProducerSet.Invoke(idx, isProducer);
            factory.RegenerateSpecies(s.GObject, s.BodySize, s.Greediness, s.RandomSeed);
        }
        public void SetSize(int idx, float size)
        {
            if (!spawnedSpecies.ContainsKey(idx))
            {
                throw new Exception("idx not spawned");
            }
            Species s = spawnedSpecies[idx];
            s.BodySize = size;
            OnSizeSet.Invoke(idx, size);
            factory.RegenerateSpecies(s.GObject, s.BodySize, s.Greediness, s.RandomSeed);

            if (inspected == s)
            {
                SetSlidersWithoutCallbacks(s.BodySize, s.Greediness);
            }
        }
        public void SetGreed(int idx, float greed)
        {
            if (!spawnedSpecies.ContainsKey(idx))
            {
                throw new Exception("idx not spawned");
            }
            Species s = spawnedSpecies[idx];
            s.Greediness = greed;
            OnGreedSet.Invoke(idx, greed);
            factory.RegenerateSpecies(s.GObject, s.BodySize, s.Greediness, s.RandomSeed);

            if (inspected == s)
            {
                SetSlidersWithoutCallbacks(s.BodySize, s.Greediness);
            }
        }

        public void Hide()
        {
            if (incubated != null)
            {
                incubator.Unincubate();
                GetComponent<Animator>().SetTrigger("Unincubate");
            }
            else if (inspected != null)
            {
                GetComponent<Animator>().SetTrigger("Uninspect");
                typesAnim.SetBool("Visible", false);
            }
        }
        public void HideSizeSlider(bool hidden=true)
        {
            sizeSlider.transform.parent.gameObject.SetActive(!hidden);
        }
        public void HideGreedSlider(bool hidden=true)
        {
            greedSlider.transform.parent.gameObject.SetActive(!hidden);
        }
        bool removeEnabled = true;
        public void HideRemoveButton(bool hidden=true)
        {
            removeEnabled = !hidden;
        }
        float initialSizeIfFixed = -1, initialGreedIfFixed = -1;
        public void FixInitialSize(float fixedSize)
        {
            initialSizeIfFixed = fixedSize;
        }
        public void FixInitialGreed(float fixedGreed)
        {
            initialGreedIfFixed = fixedGreed;
        }
        public void HidePlantPawButton(bool hidden=true)
        {
            typesAnim.gameObject.SetActive(!hidden);
        }
    }
}