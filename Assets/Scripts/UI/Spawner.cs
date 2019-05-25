using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace EcoBuilder.UI
{
    public class Spawner : MonoBehaviour, IDragHandler
    {
        public event Action<GameObject> OnIncubated;
        [SerializeField] Transform incubator;
        GameObject incubated = null;
        public GameObject TakeIncubated()
        {
            var temp = incubated;
            incubated = null;
            return temp;
        }
        public void RefreshIncubated()
        {
            if (incubated != null)
                Destroy(incubated);

            incubated = GameManager.Instance.GetSpeciesObject();
            incubated.transform.SetParent(incubator, false);
        }
        public GameObject GenerateObject(bool isProducer, float size, float greed, int randomSeed)
        {
            return generated;
        }
        public void OnDrag(PointerEventData ped)
        {

        }
        [SerializeField] JonnyGenerator factory;
        public GameObject GetSpeciesObject(bool isProducer, float size, float greed, int randomSeed)
        {
            return factory.GenerateSpecies(isProducer, size, greed, randomSeed);
        }

    }
}
