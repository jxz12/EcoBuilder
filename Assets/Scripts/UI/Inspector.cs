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
        public event Action<int> OnConflicted;
        public event Action<int> OnUnconflicted;
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
        [SerializeField] Animator typesAnim;

        [SerializeField] Name nameField;
        [SerializeField] Button refroveButton;
        [SerializeField] Trait sizeTrait;
        [SerializeField] Trait greedTrait;
        [SerializeField] bool allowConflicts;
        
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
            public string UserName { get; set; } = null;

            public Species(int idx, bool isProducer)
            {
                Idx = idx;
                IsProducer = isProducer;
                RerollSeed();
            }
            // to set seed from file
            public Species(int idx, bool isProducer, int seed)
                : this(idx, isProducer)
            {
                // BodySize = size;
                // Greediness = greed;
                RandomSeed = seed;
            }

            // for refreshing a species
            public void RerollSeed()
            {
                RandomSeed = UnityEngine.Random.Range(0, int.MaxValue);
            }
        }

        Dictionary<int, Species> spawnedSpecies = new Dictionary<int, Species>();
        Dictionary<int, Species> graveyard = new Dictionary<int, Species>();
        Species incubated = null, inspected = null;

        void Start()
        {
            producerButton.onClick.AddListener(()=> IncubateNew(true));
            consumerButton.onClick.AddListener(()=> IncubateNew(false));
            refroveButton.onClick.AddListener(()=> RefreshOrRemove());

            nameField.OnUserNameChanged += s=> SetNameFromInputField(s);
            sizeTrait.OnUserSlid += (x,y)=> SetSizeFromSlider(x, y);
            greedTrait.OnUserSlid += (x,y)=> SetGreedFromSlider(x, y);
            sizeTrait.AddExternalConflict(x=> CheckSizeConflict(x));
            greedTrait.AddExternalConflict(x=> CheckGreedConflict(x));

            sizeTrait.OnConflicted += x=> OnConflicted?.Invoke(x);
            greedTrait.OnConflicted += x=> OnConflicted?.Invoke(x);
            sizeTrait.OnUnconflicted += x=> OnUnconflicted?.Invoke(x);
            greedTrait.OnUnconflicted += x=> OnUnconflicted?.Invoke(x);

            incubator.OnDropped += ()=> SpawnIncubated();
        }

        void SpawnWithEvents(Species toSpawn)
        {
            spawnedSpecies[toSpawn.Idx] = toSpawn;
            OnSpawned?.Invoke(toSpawn.Idx); // must be invoked first

            OnShaped?.Invoke(toSpawn.Idx, toSpawn.GObject);
            OnIsProducerSet?.Invoke(toSpawn.Idx, toSpawn.IsProducer);
            OnSizeSet?.Invoke(toSpawn.Idx, toSpawn.BodySize);
            OnGreedSet?.Invoke(toSpawn.Idx, toSpawn.Greediness);
        }
        void Bury(Species toBury)
        {
            spawnedSpecies.Remove(toBury.Idx);
            graveyard.Add(toBury.Idx, toBury);

            OnDespawned?.Invoke(toBury.Idx); // must be invoked last
        }

        int nextIdx=0;
        void IncubateNew(bool isProducer)
        {
            if (inspected != null)
            {
                inspected = null;
            }
            if (incubated != null)
            {
                throw new Exception("already incubating");
            }
            while (spawnedSpecies.ContainsKey(nextIdx) || graveyard.ContainsKey(nextIdx))
                nextIdx += 1;

            incubated = new Species(nextIdx, isProducer);
            RandomiseIncubated();

            // grab gameobject from factory
            incubated.GObject = factory.GenerateSpecies(incubated.IsProducer, incubated.BodySize, incubated.Greediness, incubated.RandomSeed);
            incubator.Incubate(incubated.GObject);
            nameField.SetNameWithoutCallback(incubated.GObject.name);
            nameField.SetDefaultColour();

            nameField.Interactable = true;
            sizeTrait.Interactable = true;
            greedTrait.Interactable = true;
            refroveButton.interactable = true;
            refroveButton.gameObject.SetActive(true);

            GetComponent<Animator>().SetTrigger("Incubate");
            typesAnim.SetBool("Visible", false);
            OnIncubated?.Invoke();
        }
        void RandomiseIncubated()
        {
            incubated.RerollSeed();
            if (traitsFixed || allowConflicts)
            {
                incubated.BodySize = sizeTrait.SetValueFromRandomSeed(incubated.RandomSeed);
                incubated.Greediness = greedTrait.SetValueFromRandomSeed(incubated.RandomSeed);
            }
            else
            {
                // first set up a set of possible conflicts
                var traitPairs = new HashSet<Tuple<float,float>>();
                foreach (Species spawned in spawnedSpecies.Values)
                {
                    if (spawned.IsProducer == incubated.IsProducer) // no conflicts with other type
                        traitPairs.Add(Tuple.Create(spawned.BodySize, spawned.Greediness));
                }
                // choose a random combination not in the set
                var availablePairs = new List<Tuple<float, float>>();
                foreach (float testSize in sizeTrait.PossibleValues)
                {
                    foreach (float testGreed in greedTrait.PossibleValues)
                    {
                        var testPair = Tuple.Create(testSize, testGreed);
                        if (!traitPairs.Contains(testPair))
                        {
                            availablePairs.Add(testPair);
                        }
                    }
                }
                if (availablePairs.Count > 0)
                {
                    UnityEngine.Random.InitState(incubated.RandomSeed);
                    var chosenPair = availablePairs[UnityEngine.Random.Range(0, availablePairs.Count)];
                    incubated.BodySize = chosenPair.Item1;
                    incubated.Greediness = chosenPair.Item2;
                }
                else
                {
                    throw new Exception("somehow filled up all niches!");
                }
                sizeTrait.SetValueWithoutCallback(incubated.BodySize);
                greedTrait.SetValueWithoutCallback(incubated.Greediness);
            }
        }

        void SpawnIncubated()
        {
            if (incubated == null)
                throw new Exception("nothing incubated");

            SpawnWithEvents(incubated);
            int spawnedIdx = incubated.Idx;
            incubated = null;

            OnUnincubated?.Invoke();
            OnUserSpawned?.Invoke(spawnedIdx);

            InspectSpecies(spawnedIdx);
            typesAnim.SetBool("Visible", true);
        }
        int CheckSizeConflict(float newSize)
        {
            if (allowConflicts)
                return -1;

            Species toCheck = incubated!=null? incubated : inspected;
            foreach (Species s in spawnedSpecies.Values)
            {
                if (s != toCheck &&
                    s.IsProducer == toCheck.IsProducer &&
                    s.BodySize == newSize &&
                    s.Greediness == toCheck.Greediness)
                {
                    OnConflicted?.Invoke(s.Idx);
                    return s.Idx;
                }
            }
            return -1;
        }
        int CheckGreedConflict(float newGreed)
        {
            if (allowConflicts)
                return -1;

            Species toCheck = incubated!=null? incubated : inspected;
            foreach (Species s in spawnedSpecies.Values)
            {
                if (s != toCheck &&
                    s.IsProducer == toCheck.IsProducer &&
                    s.BodySize == toCheck.BodySize && 
                    s.Greediness == newGreed)
                {
                    OnConflicted?.Invoke(s.Idx);
                    return s.Idx;
                }
            }
            return -1;
        }
        void RefreshOrRemove() // only called on inspected or incubated
        {
            if (incubated != null)
            {
                RandomiseIncubated();
                factory.RegenerateSpecies(incubated.GObject, incubated.BodySize, incubated.Greediness, incubated.RandomSeed);
                nameField.SetNameWithoutCallback(incubated.GObject.name);
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
                    OnUserDespawned?.Invoke(inspected.Idx);
                    inspected = null;
                    GetComponent<Animator>().SetTrigger("Uninspect");
                }
            }
            else
            {
                throw new Exception("nothing incubated or inspected");
            }
        }
        void SetNameFromInputField(string newName)
        {
            Species toSet = incubated!=null? incubated : inspected;
            toSet.GObject.name = toSet.UserName = newName;
        }
        void SetSizeFromSlider(float prevValue, float newValue)
        {
            Species toSet = incubated!=null? incubated : inspected;
            toSet.BodySize = newValue;
            factory.RegenerateSpecies(toSet.GObject, toSet.BodySize, toSet.Greediness, toSet.RandomSeed);
            if (toSet.UserName != null)
                toSet.GObject.name = toSet.UserName; // revert to user's name
            else
                nameField.SetNameWithoutCallback(toSet.GObject.name);

            if (inspected != null)
            {
                OnSizeSet?.Invoke(inspected.Idx, newValue);
                OnUserSizeSet?.Invoke(inspected.Idx, prevValue, newValue);
            }
        }
        void SetGreedFromSlider(float prevValue, float newValue)
        {
            Species toSet = incubated!=null? incubated : inspected;
            toSet.Greediness = newValue;
            factory.RegenerateSpecies(toSet.GObject, toSet.BodySize, toSet.Greediness, toSet.RandomSeed);
            if (toSet.UserName != null)
                toSet.GObject.name = toSet.UserName; // revert to user's name
            else
                nameField.SetNameWithoutCallback(toSet.GObject.name);

            if (inspected != null)
            {
                OnGreedSet?.Invoke(inspected.Idx, newValue);
                OnUserGreedSet?.Invoke(inspected.Idx, prevValue, newValue);
            }
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
                OnUnincubated?.Invoke();
            }

            inspected = spawnedSpecies[idx];
            nameField.SetNameWithoutCallback(inspected.GObject.name);
            if (inspected.UserName != null)
                nameField.SetUserColour();
            else
                nameField.SetDefaultColour();

            sizeTrait.SetValueWithoutCallback(inspected.BodySize);
            greedTrait.SetValueWithoutCallback(inspected.Greediness);

            nameField.Interactable = false;
            sizeTrait.Interactable  = inspected.SizeEditable;
            greedTrait.Interactable = inspected.GreedEditable;
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
                OnUnincubated?.Invoke();
            }
        }
        public void Uninspect()
        {
            if (inspected != null)
            {
                inspected = null;
                sizeTrait.CancelDrag();
                greedTrait.CancelDrag();
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

            var toSpawn = new Species(idx, isProducer, randomSeed);
            toSpawn.BodySize = sizeTrait.SetValueWithoutCallback(size);
            toSpawn.Greediness = greedTrait.SetValueWithoutCallback(greed);
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

            if (graveyard[idx].GObject != null) // check may not be necessary
            {
                Destroy(graveyard[idx].GObject);
            }
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

        ///////////////////////////
        // for spawning from level
        public void SetIsProducer(int idx, bool isProducer)
        {
            if (!spawnedSpecies.ContainsKey(idx))
            {
                throw new Exception("idx not spawned");
            }
            Species s = spawnedSpecies[idx];
            s.IsProducer = isProducer;
            OnIsProducerSet?.Invoke(idx, isProducer);
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
            OnSizeSet?.Invoke(idx, size);
            factory.RegenerateSpecies(s.GObject, s.BodySize, s.Greediness, s.RandomSeed);

            if (inspected == s)
            {
                sizeTrait.SetValueWithoutCallback(s.BodySize);
                greedTrait.SetValueWithoutCallback(s.Greediness);
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
            OnGreedSet?.Invoke(idx, greed);
            factory.RegenerateSpecies(s.GObject, s.BodySize, s.Greediness, s.RandomSeed);

            if (inspected == s)
            {
                sizeTrait.SetValueWithoutCallback(s.BodySize);
                greedTrait.SetValueWithoutCallback(s.Greediness);
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

        //////////////////////////////
        // stuff for tutorials to use

        public void HideSizeSlider(bool hidden=true)
        {
            sizeTrait.gameObject.SetActive(!hidden);
        }
        public void HideGreedSlider(bool hidden=true)
        {
            greedTrait.gameObject.SetActive(!hidden);
        }

        // only for tutorial
        bool traitsFixed = false;
        public void FixIncubatedSize(float fixedSize)
        {
            sizeTrait.FixRandomSeedValue(fixedSize);
            traitsFixed = true;
        }
        public void FixIncubatedGreed(float fixedGreed)
        {
            greedTrait.FixRandomSeedValue(fixedGreed);
            traitsFixed = true;
        }

        bool removeEnabled = true;
        public void HideRemoveButton(bool hidden=true)
        {
            removeEnabled = !hidden;
        }
        public void HidePlantPawButton(bool hidden=true)
        {
            typesAnim.gameObject.SetActive(!hidden);
        }
    }
}