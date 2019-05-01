using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EcoBuilder
{
    public class SpawnArea : MonoBehaviour, IDragHandler
    {
        public void Hide()
        {
            // TODO: change to animation
            transform.localScale = Vector3.zero;
        }
        public void Show()
        {
            transform.localScale = new Vector3(87, 87, 87);
        }
        public void PrepareSpawn(GameObject toSpawn)
        {
            toSpawn.transform.SetParent(transform, false);
        }
        public void OnDrag(PointerEventData ped)
        {

        }
    }
}
