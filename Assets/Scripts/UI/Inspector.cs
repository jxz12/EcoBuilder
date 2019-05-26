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
    public class Inspector : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
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
        [SerializeField] RectTransform incubatorParent;

        Dictionary<int, Species> spawnedSpecies = new Dictionary<int, Species>();
        int nextIdx = 0;
        Species inspected = null;

        void Start()
        {
            producerButton.onClick.AddListener(()=> IncubateNew(true));
            consumerButton.onClick.AddListener(()=> IncubateNew(false));
            refreshButton.onClick.AddListener(()=> RefreshIncubated());

            sizeSlider.onValueChanged.AddListener(x=> SetInspectedSize(x));
            greedSlider.onValueChanged.AddListener(x=> SetInspectedGreed(x));
        }

        [SerializeField] JonnyGenerator factory;
        
        void IncubateNew(bool isProducer)
        {
            Species s = new Species(nextIdx, isProducer, sizeSlider.value, greedSlider.value);
            s.Object = factory.GenerateSpecies(s.IsProducer, s.BodySize, s.Greediness, s.RandomSeed);
            s.Object.transform.SetParent(incubatorParent, false);
            nameText.text = s.Object.name;

            inspected = s;
        }
        void RefreshIncubated()
        {
            if (inspected == null || inspected.Spawned)
                throw new Exception("nothing incubated");

            Destroy(inspected.Object);
            IncubateNew(inspected.IsProducer);
        }
        void SetInspectedSize(float bodySize)
        {
            if (inspected == null)
                throw new Exception("nothing inspected");

            inspected.BodySize = bodySize;
            factory.RegenerateSpecies(inspected.Object, inspected.IsProducer, inspected.BodySize, inspected.Greediness, inspected.RandomSeed);
            if (inspected.Spawned)
                OnSizeSet.Invoke(inspected.Idx, inspected.BodySize);
        }
        void SetInspectedGreed(float greed)
        {
            if (inspected == null)
                throw new Exception("nothing inspected");

            inspected.Greediness = greed;
            factory.RegenerateSpecies(inspected.Object, inspected.IsProducer, inspected.BodySize, inspected.Greediness, inspected.RandomSeed);
            if (inspected.Spawned)
                OnGreedSet.Invoke(inspected.Idx, inspected.Greediness);
        }
        public void InspectSpecies(int idx)
        {
            if (inspected != null && !inspected.Spawned)
                Destroy(inspected.Object);

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
        public void Spawn()
        {
            if (inspected == null)
                throw new Exception("nothing inspected");
            if (inspected.Spawned)
                throw new Exception("already spawned");
            
            OnSpawned.Invoke(inspected.Idx, inspected.Object); // must be invoked first
            
            OnProducerSet.Invoke(inspected.Idx, inspected.IsProducer);
            OnSizeSet.Invoke(inspected.Idx, inspected.BodySize);
            OnGreedSet.Invoke(inspected.Idx, inspected.Greediness);

            inspected.Spawned = true;
            spawnedSpecies[inspected.Idx] = inspected;
            nextIdx += 1;

            GetComponent<Animator>().SetTrigger("Spawn");
        }
        // public float GetKg(float normalizedBodySize)
        // {
        //     // float min = Mathf.Log10(minKg);
        //     // float max = Mathf.Log10(maxKg);
        //     // float mid = min + input*(max-min);
        //     // return Mathf.Pow(10, mid);

        //     // same as above commented
        //     return Mathf.Pow(minKg, 1-normalizedBodySize) * Mathf.Pow(maxKg, normalizedBodySize);
        // }

        // public int InspectedIdx { get { return inspected.Idx; } }
        public bool Dragging { get; private set; }
        public void OnBeginDrag(PointerEventData ped)
        {
            if (inspected != null && !inspected.Spawned)
            {
                inspected.Object.transform.SetParent(null, true);
                Dragging = true;
            }
        }
        public void OnDrag(PointerEventData ped)
        {
            if (Dragging)
            {
                Vector3 camPos = Camera.main.ScreenToWorldPoint(ped.position);
                camPos.z = 0;
                inspected.Object.transform.position = camPos;
            }
        }
        public void OnEndDrag(PointerEventData ped)
        {
            if (Dragging)
            {
                Dragging = false;
                // incubatorParent.position = Vector3.zero;
                if (!inspected.Spawned)
                {
                    inspected.Object.transform.SetParent(incubatorParent);
                    inspected.Object.transform.localPosition = Vector3.zero;
                    inspected.Object.transform.localScale = Vector3.one * .5f; // TODO: change to save original
                    inspected.Object.transform.localRotation = Quaternion.identity;
                }
            }
        }
    }
}