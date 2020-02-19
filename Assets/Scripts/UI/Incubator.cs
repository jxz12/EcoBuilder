using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace EcoBuilder.UI
{
    public class Incubator : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        // this is a little confusing because Incubator handles wanting incubation,
        // but nodelink handles wanting unincubation
        public event Action<bool> OnIncubationWanted;
        public event Action OnDropped;
        // public event Action OnUnincubated;

        [SerializeField] RectTransform incubatedParent;
        [SerializeField] Image pickupZone, dropZone;
        [SerializeField] float planeDistance;

        [SerializeField] Animator buttonsAnim;
        [SerializeField] Button producerButton;
        [SerializeField] Button consumerButton;

        void Start()
        {
            GetComponent<Canvas>().worldCamera = Camera.main;
            GetComponent<Canvas>().planeDistance = planeDistance;

            producerButton.onClick.AddListener(()=> StartIncubation(true));
            consumerButton.onClick.AddListener(()=> StartIncubation(false));
        }
        private void StartIncubation(bool isProducer)
        {
            OnIncubationWanted?.Invoke(isProducer);
            GetComponent<Animator>().SetBool("Droppable", false);
            GetComponent<Animator>().SetTrigger("Incubate");
            buttonsAnim.SetBool("Visible", false);
        }
        public void Unincubate()
        {
            GetComponent<Animator>().SetTrigger("Unincubate");
            buttonsAnim.SetBool("Visible", true);
            Destroy(incubatedObj);
            incubatedObj = null;
            // OnUnincubated?.Invoke();
        }

        GameObject incubatedObj;
        public void SetIncubatedObject(GameObject toIncubate)
        {
            incubatedObj = toIncubate;
            incubatedObj.transform.SetParent(incubatedParent, false);
        }
        public void ReplaceIncubatedObject(GameObject replacement)
        {
            Destroy(incubatedObj);
            incubatedObj = replacement;
            incubatedObj.transform.SetParent(incubatedParent, false);
        }
        public void Finish()
        {
            if (incubatedObj != null) {
                Destroy(incubatedObj);
                GetComponent<Animator>().SetTrigger("Unincubate");
            }
            buttonsAnim.SetBool("Visible", false);
        }

        // for tutorials
        public void HideStartButtons(bool hidden=true)
        {
            buttonsAnim.gameObject.SetActive(!hidden);
        }
        public void SetProducerAvailability(bool available)
        {
            producerButton.interactable = available;
        }
        public void SetConsumerAvailability(bool available)
        {
            consumerButton.interactable = available;
        }


        bool dragging;
        Vector3 originalPos;
        public void OnBeginDrag(PointerEventData ped)
        {
            if (incubatedObj != null && ped.rawPointerPress == pickupZone.gameObject)
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
                if (ped.pointerCurrentRaycast.gameObject == dropZone.gameObject) {
                    GetComponent<Animator>().SetBool("Droppable", true);
                } else {
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
                OnDropped?.Invoke();
                GetComponent<Animator>().SetTrigger("Spawn");
                buttonsAnim.SetBool("Visible", true);
                // OnUnincubated?.Invoke();
            }
        }
    }
}
