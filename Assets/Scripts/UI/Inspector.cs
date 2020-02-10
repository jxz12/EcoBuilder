using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace EcoBuilder.UI
{
    // handles keeping track of species gameobjects and stats
    public class Inspector : MonoBehaviour
    {
        public event Action<int, GameObject> OnSpawned;
        public event Action<int> OnDespawned;
        public event Action<int, string> OnConflicted;
        public event Action<int> OnUnconflicted;
        public event Action<int, bool> OnIsProducerSet;
        public event Action<int, float> OnSizeSet;
        public event Action<int, float> OnGreedSet;

        public event Action<int, float, float> OnUserSizeSet;
        public event Action<int, float, float> OnUserGreedSet;
        public event Action<int> OnUserSpawned;
        public event Action<int> OnUserDespawned;

        [SerializeField] Name nameField;
        [SerializeField] Button refroveButton;
        [SerializeField] Trait sizeTrait;
        [SerializeField] Trait greedTrait;
        
        [SerializeField] Incubator incubator;
        [SerializeField] ProceduralMeshGenerator factory;

        public class Species
        {
            public int Idx { get; private set; }
            public int RandomSeed { get; private set; } 

            public bool IsProducer { get; set; }
            public float BodySize { get; set; } = -1;
            public float Greediness { get; set; } = -1;
            public bool Editable { get; set; } = true;
            public bool Removable { get; set; } = true;

            public GameObject GObject { get; set; } = null;
            public HealthBar Health { get; set; } = null;
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
            refroveButton.onClick.AddListener(()=> RefreshOrRemove());

            nameField.OnUserNameChanged += s=> SetNameFromInputField(s);
            sizeTrait.OnUserSlid += (x,y)=> SetSizeFromSlider(x, y);
            greedTrait.OnUserSlid += (x,y)=> SetGreedFromSlider(x, y);

            // x here is value to check, and the function returns the idx of the conflict
            sizeTrait.AddExternalConflict(x=> CheckSizeConflict(x)); 
            greedTrait.AddExternalConflict(x=> CheckGreedConflict(x));
            sizeTrait.OnConflicted += i=> OnConflicted?.Invoke(i, "Identical Size");
            greedTrait.OnConflicted += i=> OnConflicted?.Invoke(i, "Identical Interference");
            sizeTrait.OnUnconflicted += i=> OnUnconflicted?.Invoke(i);
            greedTrait.OnUnconflicted += i=> OnUnconflicted?.Invoke(i);
        }

        void SpawnWithNonUserEvents(Species toSpawn)
        {
            spawnedSpecies[toSpawn.Idx] = toSpawn;
            OnSpawned?.Invoke(toSpawn.Idx, toSpawn.GObject); // must be invoked first

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

        void RandomiseIncubatedTraits()
        {
            incubated.RerollSeed();
            if (allowConflicts)
            {
                                               // this sets randomly or from fixed value
                incubated.BodySize = sizeTrait.SetValueFromRandomSeed(incubated.RandomSeed);
                incubated.Greediness = greedTrait.SetValueFromRandomSeed(incubated.RandomSeed);
            }
            else
            {
                // first set up a set of possible conflicts
                var traitPairs = new HashSet<Tuple<float,float>>();
                foreach (Species spawned in spawnedSpecies.Values)
                {
                    if (spawned.IsProducer == incubated.IsProducer) { // no conflicts with other type
                        traitPairs.Add(Tuple.Create(spawned.BodySize, spawned.Greediness));
                    }
                }
                // choose a random combination not in the set
                var availablePairs = new List<Tuple<float, float>>();
                foreach (float testSize in sizeTrait.PossibleInitialValues)
                {
                    foreach (float testGreed in greedTrait.PossibleInitialValues)
                    {
                        var testPair = Tuple.Create(testSize, testGreed);
                        if (!traitPairs.Contains(testPair)) {
                            availablePairs.Add(testPair);
                        }
                    }
                }
                Assert.IsTrue(availablePairs.Count > 0, "Somehow filled up all niches! Or both traits have fixed initial sizes but conflicts aren't allowed.");

                UnityEngine.Random.InitState(incubated.RandomSeed);
                var chosenPair = availablePairs[UnityEngine.Random.Range(0, availablePairs.Count)];
                incubated.BodySize = chosenPair.Item1;
                incubated.Greediness = chosenPair.Item2;

                sizeTrait.SetValueWithoutCallback(incubated.BodySize);
                greedTrait.SetValueWithoutCallback(incubated.Greediness);
            }
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
                    // OnConflicted?.Invoke(s.Idx, "Identical");
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
                    // OnConflicted?.Invoke(s.Idx);
                    return s.Idx;
                }
            }
            return -1;
        }
        void RefreshOrRemove() // only called on inspected or incubated
        {
            Assert.IsTrue(incubated!=null || inspected!=null, "nothing incubated or inspected");

            if (incubated != null)
            {
                RandomiseIncubatedTraits();
                factory.RegenerateSpecies(incubated.GObject, incubated.BodySize, incubated.Greediness, incubated.RandomSeed);

                nameField.SetNameWithoutCallback(incubated.GObject.name);
                nameField.SetDefaultColour();
                incubated.UserName = null;
            }
            else if (inspected != null)
            {
                Assert.IsTrue(inspected.Removable, $"trying to remove {inspected.Idx} but not removable");

                OnUserDespawned?.Invoke(inspected.Idx);
                Bury(inspected);
                
                // nodelink will unfocus in the process so don't need these
                // inspected = null;
                // GetComponent<Animator>().SetTrigger("Uninspect");
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


        //////////////////
        // public stuff //
        //////////////////

        int nextIdx=0;
        public void IncubateNew(bool isProducer)
        {
            Assert.IsNull(incubated, "already incubating");

            if (inspected != null) {
                inspected = null;
            }
            while (spawnedSpecies.ContainsKey(nextIdx) || graveyard.ContainsKey(nextIdx)) {
                nextIdx += 1;
            }
            incubated = new Species(nextIdx, isProducer);
            RandomiseIncubatedTraits();

            // grab gameobject from factory
            incubated.GObject = factory.GenerateSpecies(incubated.IsProducer, incubated.BodySize, incubated.Greediness, incubated.RandomSeed);
            incubator.SetIncubatedObject(incubated.GObject);
            nameField.SetNameWithoutCallback(incubated.GObject.name);
            nameField.SetDefaultColour();

            nameField.Interactable = true;
            sizeTrait.Interactable = true;
            greedTrait.Interactable = true;
            refroveButton.interactable = true;
            refroveButton.gameObject.SetActive(true);
            nameField.ExpandIntoRefrove(false); // ugh

            GetComponent<Animator>().SetTrigger("Incubate");
        }
        public void SpawnIncubated()
        {
            Assert.IsNotNull(incubated, "nothing incubated to spawn");

            SpawnWithNonUserEvents(incubated);
            int spawnedIdx = incubated.Idx;
            incubated = null;

            OnUserSpawned?.Invoke(spawnedIdx);
            // InspectSpecies(spawnedIdx); // will be done by nodelink
        }
        public void InspectSpecies(int idx)
        {
            if (inspected == null)
            {
                GetComponent<Animator>().SetTrigger("Inspect");
            }
            if (incubated != null)
            {
                incubator.Unincubate();
                incubated = null;
            }

            inspected = spawnedSpecies[idx];
            nameField.SetNameWithoutCallback(inspected.GObject.name);
            if (inspected.UserName != null) {
                nameField.SetUserColour();
            } else {
                nameField.SetDefaultColour();
            }
            sizeTrait.SetValueWithoutCallback(inspected.BodySize);
            greedTrait.SetValueWithoutCallback(inspected.Greediness);

            // nameField.Interactable = false;
            sizeTrait.Interactable  = greedTrait.Interactable = inspected.Editable;
            refroveButton.interactable = inspected.Removable;

            // uuggghhh
            refroveButton.gameObject.SetActive(!removeHidden);
            nameField.ExpandIntoRefrove(removeHidden);
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
        public void Unincubate()
        {
            if (incubated != null)
            {
                incubator.Unincubate();
                incubated = null;
                GetComponent<Animator>().SetTrigger("Unincubate");
            }
        }

        // for loading from level
        public void SpawnNotIncubated(int idx, bool isProducer, float size, float greed, int randomSeed, bool editable)
        {
            Assert.IsFalse(spawnedSpecies.ContainsKey(idx) || graveyard.ContainsKey(idx), "idx already added");
            Assert.IsNull(inspected, "somehow inspecting??");
            Assert.IsFalse(size < 0 || size > 1, "size not in bounds");
            Assert.IsFalse(greed < 0 || greed > 1, "greed not in bounds");

            var toSpawn = new Species(idx, isProducer, randomSeed);
            toSpawn.BodySize = sizeTrait.SetValueWithoutCallback(size);
            toSpawn.Greediness = greedTrait.SetValueWithoutCallback(greed);
            toSpawn.Editable = editable;
            toSpawn.GObject = factory.GenerateSpecies(isProducer, size, greed, randomSeed);
            
            SpawnWithNonUserEvents(toSpawn);
        }
        public void SetSpeciesRemovable(int idx, bool removable)
        {
            spawnedSpecies[idx].Removable = removable;
        }

        // for un/redo
        public void Despawn(int idx)
        {
            Assert.IsTrue(spawnedSpecies.ContainsKey(idx), "idx not spawned");

            if (spawnedSpecies[idx] == inspected)
            {
                GetComponent<Animator>().SetTrigger("Uninspect");
                inspected = null;
            }
            Bury(spawnedSpecies[idx]);
        }
        public void DespawnCompletely(int idx)
        {
            Assert.IsTrue(graveyard.ContainsKey(idx), "idx not in graveyard");

            if (graveyard[idx].GObject != null) { // this check is probably not necessary, but why not
                Destroy(graveyard[idx].GObject);
            }
            graveyard.Remove(idx);
        }
        public void Respawn(int idx)
        {
            Assert.IsTrue(graveyard.ContainsKey(idx), "idx not in graveyard");

            graveyard[idx].GObject.SetActive(true);
            SpawnWithNonUserEvents(graveyard[idx]);
            graveyard.Remove(idx);
        }
        public void DrawHealthBars(Func<int, float> Health)
        {
            foreach (int idx in spawnedSpecies.Keys)
            {
                Health(idx);
            }
            print("TODO: health bars");
        }
        

        ///////////////////////////
        // for spawning from level
        public void SetIsProducer(int idx, bool isProducer)
        {
            Assert.IsTrue(spawnedSpecies.ContainsKey(idx), "idx not spawned");

            Species s = spawnedSpecies[idx];
            s.IsProducer = isProducer;
            OnIsProducerSet?.Invoke(idx, isProducer);
            factory.RegenerateSpecies(s.GObject, s.BodySize, s.Greediness, s.RandomSeed);
        }
        public void SetSize(int idx, float size)
        {
            Assert.IsTrue(spawnedSpecies.ContainsKey(idx), "idx not spawned");

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
            Assert.IsTrue(spawnedSpecies.ContainsKey(idx), "idx not spawned");

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
        public void Finish()
        {
            if (incubated != null)
            {
                incubator.Finish();
                GetComponent<Animator>().SetTrigger("Unincubate");
            }
            else if (inspected != null)
            {
                GetComponent<Animator>().SetTrigger("Uninspect");
            }
        }
        public void HideSizeSlider(bool hidden)
        {
            sizeTrait.gameObject.SetActive(!hidden);
            sizeTrait.RandomiseInitialValue = !hidden;
        }
        public void HideGreedSlider(bool hidden)
        {
            greedTrait.gameObject.SetActive(!hidden);
            greedTrait.RandomiseInitialValue = !hidden;
        }
        private bool allowConflicts = true;
        public void AllowConflicts(bool allowed)
        {
            allowConflicts = allowed;
        }


        //////////////////////////////
        // stuff for tutorials to use
        bool removeHidden = false;
        public void HideRemoveButton(bool hidden=true)
        {
            removeHidden = hidden;
        }

        // for saving levels
        public IEnumerable<KeyValuePair<int, Species>> GetSpeciesInfo()
        {
            return spawnedSpecies;
        }

    }
}