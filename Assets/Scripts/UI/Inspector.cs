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
        public event Action OnIncubated;
        public event Action OnUnincubated;
        public event Action<int, bool, GameObject> OnSpawned;
        public event Action<int> OnDespawned;
        public event Action<int, string> OnConflicted;
        public event Action<int> OnUnconflicted;

                         // idx, normalised
        public event Action<int, float> OnSizeSet;
        public event Action<int, float> OnGreedSet;

                         // idx, from,to
        public event Action<int, int, int> OnUserSizeSet;
        public event Action<int, int, int> OnUserGreedSet;
        public event Action<int> OnUserSpawned;
        public event Action<int> OnUserDespawned;

        [SerializeField] Name nameField;
        [SerializeField] Button refroveButton;
        [SerializeField] Trait sizeTrait;
        [SerializeField] Trait greedTrait;
        
        [SerializeField] Incubator incubator;
        [SerializeField] ProceduralMeshGenerator factory;
        [SerializeField] StatusBar statusPrefab;

        private class Species
        {
            public int Idx { get; private set; }
            public int RandomSeed { get; private set; } 

            public bool IsProducer { get; set; }
            public int BodySize { get; set; } = 0;
            public int Greediness { get; set; } = 0;
            public bool Editable { get; set; } = true;

            public bool Removable { get; set; } = true;

            public GameObject GObject { get; set; } = null;
            public StatusBar Status { get; set; } = null;
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

            incubator.OnIncubationWanted += (b)=> IncubateNew(b);
            incubator.OnDropped += ()=> SpawnIncubated();
        }

        int nextIdx=0;
        private void IncubateNew(bool isProducer)
        {
            Assert.IsNull(incubated, "already incubating");
            refroveButton.onClick.RemoveAllListeners();
            refroveButton.onClick.AddListener(RefreshIncubated);

            if (inspected != null) {
                inspected = null;
            }
            while (spawnedSpecies.ContainsKey(nextIdx) || graveyard.ContainsKey(nextIdx)) {
                nextIdx += 1;
            }
            incubated = new Species(nextIdx, isProducer);
            RandomiseIncubatedTraits();

            // grab gameobject from factory
            incubated.GObject = factory.GenerateSpecies(incubated.IsProducer, sizeTrait.NormaliseValue(incubated.BodySize), greedTrait.NormaliseValue(incubated.Greediness), incubated.RandomSeed);

            incubator.SetIncubatedObject(incubated.GObject);
            nameField.SetNameWithoutCallback(incubated.GObject.name);
            nameField.SetDefaultColour();

            incubated.Status = Instantiate(statusPrefab, transform.parent);
            incubated.Status.FollowSpecies(incubated.GObject.transform, sizeTrait.PositivifyValue(incubated.BodySize), greedTrait.PositivifyValue(incubated.Greediness));

            nameField.Interactable = true;
            sizeTrait.Interactable = true;
            greedTrait.Interactable = true;
            refroveButton.interactable = true;
            refroveButton.gameObject.SetActive(true);
            nameField.ExpandIntoRefrove(false); // ugh

            GetComponent<Animator>().SetTrigger("Incubate");
            OnIncubated.Invoke();
        }
        private void SpawnIncubated()
        {
            Assert.IsNotNull(incubated, "nothing incubated to spawn");
            incubator.ReleaseIncubatedObject();
            OnUnincubated?.Invoke();

            SpawnWithNonUserEvents(incubated);
            int spawnedIdx = incubated.Idx;
            incubated = null;

            OnUserSpawned?.Invoke(spawnedIdx);

            // FIXME: this is handled by a nodelink callback, but this is smelly
            //        would be less smelly to unincubate here first
            // InspectSpecies(spawnedIdx);
        }
        private void RefreshIncubated()
        {
            Assert.IsNotNull(incubated, "nothing incubated");

            if (incubated != null) // refresh
            {
                RandomiseIncubatedTraits();
                factory.RegenerateSpecies(incubated.GObject, sizeTrait.NormaliseValue(incubated.BodySize), greedTrait.NormaliseValue(incubated.Greediness), incubated.RandomSeed);

                nameField.SetNameWithoutCallback(incubated.GObject.name);
                nameField.SetDefaultColour();
                incubated.UserName = null;
            }
        }
        
        private void RemoveInspected()
        {
            Assert.IsNotNull(inspected, "nothing inspected");
            Assert.IsTrue(inspected.Removable, $"trying to remove {inspected.Idx} but not removable");

            Species toRemove = inspected;
            OnUserDespawned?.Invoke(toRemove.Idx);
            BuryWithNonUserEvents(toRemove);

            // FIXME: this is handled by a nodelink callback, but is smelly
            //        would be less smelly to uninspect here first
            // Uninspect();
        }

        private void SpawnWithNonUserEvents(Species toSpawn)
        {
            if (graveyard.ContainsKey(toSpawn.Idx))
            {
                toSpawn.GObject.SetActive(true);
                graveyard.Remove(toSpawn.Idx);
            }
            spawnedSpecies[toSpawn.Idx] = toSpawn;
            toSpawn.Status.ShowTraits(true);
            toSpawn.Status.ShowHealth(true);

            // must be invoked first so that nodelink focuses after adding
            OnSpawned?.Invoke(toSpawn.Idx, toSpawn.IsProducer, toSpawn.GObject);

            OnSizeSet?.Invoke(toSpawn.Idx, sizeTrait.NormaliseValue(toSpawn.BodySize));
            OnGreedSet?.Invoke(toSpawn.Idx, greedTrait.NormaliseValue(toSpawn.Greediness));
        }
        private void BuryWithNonUserEvents(Species toBury)
        {
            spawnedSpecies.Remove(toBury.Idx);

            graveyard[toBury.Idx] = toBury;
            toBury.Status.ShowTraits(false);
            toBury.Status.ShowHealth(false);

            // must be invoked last so that nodelink unfocuses before removing
            OnDespawned?.Invoke(toBury.Idx);
        }

        private void RandomiseIncubatedTraits()
        {
            incubated.RerollSeed();
            if (allowConflicts)
            {
                // these set randomly or from fixed value
                sizeTrait.SetValueFromRandomSeed(incubated.RandomSeed);
                greedTrait.SetValueFromRandomSeed(incubated.RandomSeed);
                incubated.BodySize = sizeTrait.UnnormalisedValue;
                incubated.Greediness = greedTrait.UnnormalisedValue;
            }
            else
            {
                // first set up a set of possible conflicts
                var traitPairs = new HashSet<Tuple<int,int>>();
                foreach (Species spawned in spawnedSpecies.Values)
                {
                    if (spawned.IsProducer == incubated.IsProducer) { // no conflicts with other type
                        traitPairs.Add(Tuple.Create(spawned.BodySize, spawned.Greediness));
                    }
                }
                // choose a random combination not in the set
                var availablePairs = new List<Tuple<int, int>>();
                foreach (int testSize in sizeTrait.PossibleInitialValues)
                {
                    foreach (int testGreed in greedTrait.PossibleInitialValues)
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

        // these return the idx of any conflicted species
        int CheckSizeConflict(float newSize)
        {
            if (allowConflicts) {
                return -1;
            }
            Species toCheck = incubated!=null? incubated : inspected;
            foreach (Species s in spawnedSpecies.Values)
            {
                if (s != toCheck &&
                    s.IsProducer == toCheck.IsProducer &&
                    s.BodySize == newSize &&
                    s.Greediness == toCheck.Greediness)
                {
                    return s.Idx;
                }
            }
            return -1;
        }
        int CheckGreedConflict(float newGreed)
        {
            if (allowConflicts) {
                return -1;
            }
            Species toCheck = incubated!=null? incubated : inspected;
            foreach (Species s in spawnedSpecies.Values)
            {
                if (s != toCheck &&
                    s.IsProducer == toCheck.IsProducer &&
                    s.BodySize == toCheck.BodySize && 
                    s.Greediness == newGreed)
                {
                    return s.Idx;
                }
            }
            return -1;
        }
        void SetNameFromInputField(string newName)
        {
            Species toSet = incubated!=null? incubated : inspected;
            toSet.GObject.name = toSet.UserName = newName;
        }
        void SetNameIfNotUser(Species toSet)
        {
            if (toSet.UserName != null) {
                toSet.GObject.name = toSet.UserName; // revert to user's name
            } else {
                nameField.SetNameWithoutCallback(toSet.GObject.name);
            }
        }
        void SetSizeFromSlider(int prevValue, int newValue)
        {
            Assert.IsTrue(incubated!=null || inspected!=null, "nothing inspected or incubated");

            Species toSet = incubated!=null? incubated : inspected;
            toSet.BodySize = newValue;
            toSet.Status.SetSize(sizeTrait.PositivifyValue(toSet.BodySize));

            factory.RegenerateSpecies(toSet.GObject, sizeTrait.NormaliseValue(toSet.BodySize), greedTrait.NormaliseValue(toSet.Greediness), toSet.RandomSeed);

            SetNameIfNotUser(toSet);

            if (inspected != null) // only call events on spawned species
            {
                OnSizeSet?.Invoke(inspected.Idx, sizeTrait.NormaliseValue(newValue));
                OnUserSizeSet?.Invoke(inspected.Idx, prevValue, newValue);
            }
        }
        void SetGreedFromSlider(int prevValue, int newValue)
        {
            Assert.IsTrue(incubated!=null || inspected!=null, "nothing inspected or incubated");

            Species toSet = incubated!=null? incubated : inspected;
            toSet.Greediness = newValue;
            toSet.Status.SetGreed(greedTrait.PositivifyValue(toSet.Greediness));

            factory.RegenerateSpecies(toSet.GObject, sizeTrait.NormaliseValue(toSet.BodySize), greedTrait.NormaliseValue(toSet.Greediness), toSet.RandomSeed);

            SetNameIfNotUser(toSet);

            if (inspected != null) // only call events on spawned species
            {
                OnGreedSet?.Invoke(inspected.Idx, newValue);
                OnUserGreedSet?.Invoke(inspected.Idx, prevValue, newValue);
            }
        }

        //////////////////
        // public stuff //
        //////////////////
        public void InspectSpecies(int idx)
        {
            refroveButton.onClick.RemoveAllListeners();
            refroveButton.onClick.AddListener(RemoveInspected);

            if (inspected == null) {
                GetComponent<Animator>().SetTrigger("Inspect");
            }
            if (incubated != null)
            {
                incubator.Unincubate();
                
                incubated = null;
                OnUnincubated?.Invoke();
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

            sizeTrait.Interactable  = greedTrait.Interactable = inspected.Editable;
            refroveButton.interactable = inspected.Removable;

            // uuggghhh
            refroveButton.gameObject.SetActive(!removeHidden);
            nameField.ExpandIntoRefrove(removeHidden);
        }
        public void Uninspect()
        {
            // Assert.IsNotNull(inspected, "nothing inspected");
            if (inspected != null)
            {
                inspected = null;

                // don't want sliders to keep changing inspected after uninspect
                sizeTrait.CancelDrag();
                greedTrait.CancelDrag();

                GetComponent<Animator>().SetTrigger("Uninspect");
            }
        }
        public void CancelIncubation()
        {
            // Assert.IsNotNull(incubated, "nothing incubated");
            if (incubated != null)
            {
                incubator.Unincubate();
                incubated = null;

                OnUnincubated?.Invoke();
                GetComponent<Animator>().SetTrigger("Unincubate");
            }
        }
        public void MakeSpeciesObjectExtinct(int idx)
        {
            factory.KillSpecies(spawnedSpecies[idx].GObject);
        }
        public void MakeSpeciesObjectRescued(int idx)
        {
            factory.RescueSpecies(spawnedSpecies[idx].GObject);
        }

        public void SetProducerAvailability(bool available)
        {
            incubator.EnableProducerButton(available);
        }
        public void SetConsumerAvailability(bool available)
        {
            incubator.EnableConsumerButton(available);
        }


        ///////////////////////////
        // loading from level

        public void SpawnNotIncubated(int idx, bool isProducer, int size, int greed, int randomSeed, bool editable)
        {
            Assert.IsFalse(spawnedSpecies.ContainsKey(idx) || graveyard.ContainsKey(idx), "idx already added");
            Assert.IsNull(inspected, "somehow inspecting??");

            float norm = sizeTrait.NormaliseValue(size);
            Assert.IsFalse(norm < 0 || norm > 1, "size not in bounds");
            norm = greedTrait.NormaliseValue(greed);
            Assert.IsFalse(greed < 0 || greed > 1, "greed not in bounds");

            var toSpawn = new Species(idx, isProducer, randomSeed);

            toSpawn.BodySize = size;
            toSpawn.Greediness = greed;
            toSpawn.Editable = editable;
            toSpawn.GObject = factory.GenerateSpecies(isProducer, sizeTrait.NormaliseValue(size), greedTrait.NormaliseValue(greed), randomSeed);

            toSpawn.Status = Instantiate(statusPrefab, transform.parent);
            toSpawn.Status.FollowSpecies(toSpawn.GObject.transform, sizeTrait.PositivifyValue(toSpawn.BodySize), greedTrait.PositivifyValue(toSpawn.Greediness));
            
            SpawnWithNonUserEvents(toSpawn);
        }
        public void SetSpeciesRemovable(int idx, bool removable)
        {
            spawnedSpecies[idx].Removable = removable;
        }

        ////////////////////
        // un/redoing 

        // public void SetIsProducer(int idx, bool isProducer)
        // {
        //     Assert.IsTrue(spawnedSpecies.ContainsKey(idx), "idx not spawned");

        //     Species s = spawnedSpecies[idx];
        //     s.IsProducer = isProducer;
        //     OnIsProducerSet?.Invoke(idx, isProducer);
        //     factory.RegenerateSpecies(s.GObject, s.BodySize, s.Greediness, s.RandomSeed);
        // }
        public void SetSize(int idx, int size)
        {
            Assert.IsTrue(spawnedSpecies.ContainsKey(idx), "idx not spawned");

            Species s = spawnedSpecies[idx];
            s.BodySize = size;
            OnSizeSet?.Invoke(idx, size);
            factory.RegenerateSpecies(s.GObject, sizeTrait.NormaliseValue(s.BodySize), greedTrait.NormaliseValue(s.Greediness), s.RandomSeed);

            if (inspected == s)
            {
                sizeTrait.SetValueWithoutCallback(s.BodySize);
                greedTrait.SetValueWithoutCallback(s.Greediness);
            }
        }
        public void SetGreed(int idx, int greed)
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

        public void Respawn(int idx)
        {
            Assert.IsTrue(graveyard.ContainsKey(idx), "idx not in graveyard");

            SpawnWithNonUserEvents(graveyard[idx]);
        }
        public void Despawn(int idx)
        {
            Assert.IsTrue(spawnedSpecies.ContainsKey(idx), "idx not spawned");

            if (spawnedSpecies[idx] == inspected)
            {
                GetComponent<Animator>().SetTrigger("Uninspect");
                inspected = null;
            }
            BuryWithNonUserEvents(spawnedSpecies[idx]);
        }
        public void DespawnCompletely(int idx)
        {
            Assert.IsTrue(graveyard.ContainsKey(idx), "idx not in graveyard");

            if (graveyard[idx].GObject != null) { // this check is probably not necessary, but why not
                Destroy(graveyard[idx].GObject);
            }
            graveyard.Remove(idx);
        }
        public void DrawHealthBars(Func<int, float> Health)
        {
            print("TODO: health bars");
            // return;
            foreach (var kvp in spawnedSpecies)
            {
                kvp.Value.Status.SetHealth(Health(kvp.Key));
            }
        }
        
        public void Finish()
        {
            if (incubated != null)
            {
                GetComponent<Animator>().SetTrigger("Unincubate");
            }
            else if (inspected != null)
            {
                GetComponent<Animator>().SetTrigger("Uninspect");
            }
            incubator.Finish();
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
        public void HideIncubatorButtons(bool hidden=true)
        {
            incubator.HideTypeButtons(hidden);
        }

        // for saving levels
        public IEnumerable<Tuple<int, int, bool, int, int>> GetSpawnedSpeciesInfo()
        {
            foreach (Species s in spawnedSpecies.Values)
            {
                yield return Tuple.Create(s.Idx, s.RandomSeed, s.IsProducer, s.BodySize, s.Greediness);
            }
        }
        

    }
}