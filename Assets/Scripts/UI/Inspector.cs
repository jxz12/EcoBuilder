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

        public event Action<int, bool, bool> OnUserIsProducerSet;
        public event Action<int, float, float> OnUserSizeSet;
        public event Action<int, float, float> OnUserGreedSet;
        public event Action<int> OnUserSpawned;
        public event Action<int> OnUserDespawned;

        public event Action OnIncubated;
        public event Action OnUnincubated;

        [SerializeField] Button producerButton;
        [SerializeField] Button consumerButton;
        [SerializeField] Button undoButton;
        [SerializeField] Button redoButton;
        [SerializeField] Slider sizeSlider;
        [SerializeField] Slider greedSlider;

        [SerializeField] Text nameText;
        [SerializeField] Button refroveButton;
        [SerializeField] Animator movesAnim, traitsAnim, typesAnim;
        
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

            SizeSliderCallback = x=> SetSizeFromSlider();
            GreedSliderCallback = x=> SetGreedFromSlider();
            sizeSlider.onValueChanged.AddListener(SizeSliderCallback);
            greedSlider.onValueChanged.AddListener(GreedSliderCallback);

            incubator.OnDropped += ()=> SpawnIncubated();
        }
        UnityAction<float> SizeSliderCallback, GreedSliderCallback;

        void Shape(Species toShape)
        {
            spawnedSpecies[toShape.Idx] = toShape;
            OnSpawned.Invoke(toShape.Idx); // must be invoked first

            OnShaped.Invoke(toShape.Idx, toShape.GObject);
            OnIsProducerSet.Invoke(toShape.Idx, toShape.IsProducer);
            OnSizeSet.Invoke(toShape.Idx, toShape.BodySize);
            OnGreedSet.Invoke(toShape.Idx, toShape.Greediness);
            nextIdx += 1;
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
            Species s = new Species(nextIdx, isProducer);
            if (sizeIfFixed >= 0)
                s.BodySize = sizeIfFixed;
            if (greedIfFixed >= 0)
                s.Greediness = greedIfFixed;
            s.Removable = removeEnabled;

            s.GObject = factory.GenerateSpecies(s.IsProducer, s.BodySize, s.Greediness, s.RandomSeed);

            incubator.Incubate(s.GObject);
            nameText.text = s.GObject.name;

            SetSlidersWithoutEventCallbacks(s.BodySize, s.Greediness);
            sizeSlider.interactable = true;
            greedSlider.interactable = true;
            refroveButton.interactable = true;

            incubated = s;
            traitsAnim.SetTrigger("Incubate");
            typesAnim.SetBool("Visible", false);
            OnIncubated.Invoke();
        }
        void RefreshOrRemove() // only called on inspected or incubated
        {
            if (incubated != null)
            {
                incubated.RerollSeed();
                if (sizeIfFixed >= 0)
                    incubated.BodySize = sizeIfFixed;
                if (greedIfFixed >= 0)
                    incubated.Greediness = greedIfFixed;

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
                    Bury(inspected);
                    OnUserDespawned.Invoke(inspected.Idx);
                    inspected = null;
                    traitsAnim.SetTrigger("Uninspect");
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

            Shape(incubated);
            int spawnedIdx = incubated.Idx;
            incubated = null;

            OnUnincubated.Invoke();
            OnUserSpawned.Invoke(spawnedIdx);

            InspectSpecies(spawnedIdx);
            typesAnim.SetBool("Visible", true);
        }

        void SetSizeFromSlider()
        {
            if (incubated != null)
            {
                incubated.BodySize = sizeSlider.normalizedValue;
                factory.RegenerateSpecies(incubated.GObject, incubated.BodySize, incubated.Greediness, incubated.RandomSeed);
            }
            else if (inspected != null)
            {
                float prevSize = inspected.BodySize;
                inspected.BodySize = sizeSlider.normalizedValue;
                OnSizeSet.Invoke(inspected.Idx, inspected.BodySize);
                OnUserSizeSet.Invoke(inspected.Idx, prevSize, inspected.BodySize);
                factory.RegenerateSpecies(inspected.GObject, inspected.BodySize, inspected.Greediness, inspected.RandomSeed);
            }
        }
        void SetGreedFromSlider()
        {
            if (incubated != null)
            {
                incubated.Greediness = greedSlider.normalizedValue;
                factory.RegenerateSpecies(incubated.GObject, incubated.BodySize, incubated.Greediness, incubated.RandomSeed);
            }
            else if (inspected != null)
            {
                float prevGreed = inspected.Greediness;
                inspected.Greediness = greedSlider.normalizedValue;
                OnGreedSet.Invoke(inspected.Idx, inspected.Greediness);
                OnUserGreedSet.Invoke(inspected.Idx, prevGreed, inspected.Greediness);
                factory.RegenerateSpecies(inspected.GObject, inspected.BodySize, inspected.Greediness, inspected.RandomSeed);
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
        // public stuff

        public void InspectSpecies(int idx)
        {
            if (inspected == null)
            {
                traitsAnim.SetTrigger("Inspect");
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
            SetSlidersWithoutEventCallbacks(inspected.BodySize, inspected.Greediness);

            sizeSlider.interactable  = inspected.SizeEditable;
            greedSlider.interactable = inspected.GreedEditable;
            refroveButton.interactable = inspected.Removable;
        }
        public void Unincubate()
        {
            if (incubated != null)
            {
                incubator.Unincubate();
                incubated = null;
                traitsAnim.SetTrigger("Unincubate");
                typesAnim.SetBool("Visible", true);
            }
        }
        public void Uninspect()
        {
            if (inspected != null)
            {
                inspected = null;
                traitsAnim.SetTrigger("Uninspect");
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
        public int SpawnNotIncubated(bool isProducer, float size, float greed, int randomSeed, bool sizeEditable, bool greedEditable)
        {
            if (inspected != null)
                throw new Exception("somehow inspecting??");
            if (size < 0 || size > 1)
                throw new Exception("size not in bounds");
            if (greed < 0 || greed > 1)
                throw new Exception("greed not in bounds");

            var toSpawn = new Species(nextIdx, isProducer, size, greed, randomSeed);
            toSpawn.SizeEditable = sizeEditable;
            toSpawn.GreedEditable = greedEditable;
            toSpawn.GObject = factory.GenerateSpecies(isProducer, size, greed, randomSeed);
            
            Shape(toSpawn);
            return toSpawn.Idx;
        }

        // for un/redo
        public void Despawn(int idx)
        {
            if (!spawnedSpecies.ContainsKey(idx))
            {
                throw new Exception("idx not spawned");
            }
            if (spawnedSpecies[idx] == inspected)
            {
                traitsAnim.SetTrigger("Uninspect");
                inspected = null;
            }
            Bury(spawnedSpecies[idx]);
        }
        public void Respawn(int idx)
        {
            if (!graveyard.ContainsKey(idx))
            {
                throw new Exception("idx not in graveyard");
            }
            graveyard[idx].GObject.SetActive(true);
            Shape(graveyard[idx]);
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
                SetSlidersWithoutEventCallbacks(s.BodySize, s.Greediness);
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
                SetSlidersWithoutEventCallbacks(s.BodySize, s.Greediness);
            }
        }

        public void Hide()
        {
            if (incubated != null)
            {
                incubator.Unincubate();
                traitsAnim.SetTrigger("Unincubate");
            }
            else if (inspected != null)
            {
                traitsAnim.SetTrigger("Uninspect");
                typesAnim.SetBool("Visible", false);
            }
        }
        [SerializeField] GameObject sizeParent, greedParent;
        public void HideSizeSlider()
        {
            sizeParent.SetActive(false);
        }
        public void HideGreedSlider()
        {
            greedParent.SetActive(false);
        }
        bool removeEnabled = true;
        public void HideRemoveButton()
        {
            removeEnabled = false;
        }
        float sizeIfFixed = -1, greedIfFixed = -1;
        public void FixSize(float fixedSize)
        {
            sizeIfFixed = fixedSize;
        }
        public void FixGreed(float fixedGreed)
        {
            greedIfFixed = fixedGreed;
        }
    }
}