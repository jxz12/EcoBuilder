using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace EcoBuilder.Inspector
{
    public class Spawner : MonoBehaviour, IDragHandler
    {
        [SerializeField] Transform incubator;
        GameObject toSpawn = null;
        public GameObject TakeIncubated()
        {
            var temp = toSpawn;
            toSpawn = null;
            return temp;
        }
        public void IncubateSpecies(GameObject toIncubate)
        {
            if (toSpawn != null)
                Destroy(toSpawn);

            toSpawn = toIncubate;
            toSpawn.transform.SetParent(incubator, false);
        }
        public GameObject GenerateObject(bool isProducer, float size, float greed, int randomSeed)
        {
            var generated = GameManager.Instance.GetSpeciesObject(isProducer, size, greed, randomSeed);
            return generated;
        }
        public void OnDrag(PointerEventData ped)
        {

        }
    }
}
