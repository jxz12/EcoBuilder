using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace EcoBuilder.UI
{
    public class Incubator : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        public event Action OnDropped;

        [SerializeField] RectTransform incubatedParent;
        [SerializeField] Image pickupZone, dropZone;
        [SerializeField] float planeDistance;

        void Start()
        {
            GetComponent<Canvas>().worldCamera = Camera.main;
            GetComponent<Canvas>().planeDistance = planeDistance;
        }

        GameObject incubated;
        public void Incubate(GameObject toIncubate)
        {
            // dropZone.SetActive(false);
            incubated = toIncubate;
            incubated.transform.SetParent(incubatedParent, false);
            GetComponent<Animator>().SetBool("Droppable", false);
            GetComponent<Animator>().SetTrigger("Incubate");
        }
        public void Unincubate()
        {
            Destroy(incubated);
            incubated = null;
            GetComponent<Animator>().SetTrigger("Unincubate");
        }
        public void Replace(GameObject replacement)
        {
            Destroy(incubated);
            incubated = replacement;
            incubated.transform.SetParent(incubatedParent, false);
        }


        bool dragging;
        Vector3 originalPos;
        public void OnBeginDrag(PointerEventData ped)
        {
            if (incubated != null && ped.rawPointerPress == pickupZone.gameObject)
            {
                dragging = true;
                dropZone.raycastTarget = true;
                originalPos = incubatedParent.localPosition;
            }
        }
        public void OnDrag(PointerEventData ped)
        {
            if (dragging)
            {
                if (ped.pointerCurrentRaycast.gameObject == dropZone.gameObject)
                {
                    GetComponent<Animator>().SetBool("Droppable", true);
                }
                else 
                {
                    GetComponent<Animator>().SetBool("Droppable", false);
                }
                Vector3 mousePos = ped.position;
                mousePos.z = planeDistance - 1;
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

                incubatedParent.position = worldPos;
            }
        }
        public void OnEndDrag(PointerEventData ped)
        {
            if (dragging)
            {
                dragging = false;
                dropZone.raycastTarget = false;
                incubatedParent.localPosition = originalPos;
            }
        }
        public void OnDrop(PointerEventData ped)
        {
            if (dragging && ped.pointerCurrentRaycast.gameObject == dropZone.gameObject)
            {
                OnDropped.Invoke();
                GetComponent<Animator>().SetTrigger("Spawn");
            }
        }
    }
}
