using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
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

        [SerializeField] RectTransform incubatedParent;
        [SerializeField] Image pickupZone, dropZone;
        [SerializeField] float planeDistance;

        // [SerializeField] Animator buttonsAnim;
        [SerializeField] RectTransform buttons;
        [SerializeField] Button producerButton;
        [SerializeField] Button consumerButton;

        Canvas rootCanvas;
        Camera mainCam;
        void Awake()
        {
            rootCanvas = GetComponent<Canvas>();
            Assert.IsNotNull(rootCanvas, "no canvas attached to incubator");
            Assert.IsTrue(rootCanvas.isRootCanvas, "incubator must be a root canvas");
        }

        public void SetCamera(Camera cam)
        {
            mainCam = cam;
            rootCanvas.worldCamera = mainCam; // smellly but necessary because maincam is in different scene
            rootCanvas.planeDistance = planeDistance;
        }
        void Start()
        {

            producerButton.onClick.AddListener(()=> StartIncubation(true));
            consumerButton.onClick.AddListener(()=> StartIncubation(false));
            ShowButtons(true, 1f);
        }
        private void StartIncubation(bool isProducer)
        {
            OnIncubationWanted?.Invoke(isProducer);
            GetComponent<Animator>().SetBool("Droppable", false);
            GetComponent<Animator>().SetTrigger("Incubate");
            ShowButtons(false);
        }
        public void Unincubate()
        {
            GetComponent<Animator>().SetTrigger("Unincubate");
            Destroy(incubatedObj);
            incubatedObj = null;
            ShowButtons(true);
        }

        GameObject incubatedObj;
        public void SetIncubatedObject(GameObject toIncubate)
        {
            incubatedObj = toIncubate;
            incubatedObj.transform.SetParent(incubatedParent, false);
        }
        public void ReleaseIncubatedObject()
        {
            incubatedObj = null;
        }
        public void Finish()
        {
            if (incubatedObj != null) {
                Destroy(incubatedObj);
                GetComponent<Animator>().SetTrigger("Unincubate");
            }
            ShowButtons(false);
        }

        // for tutorials
        public void HideTypeButtons(bool hidden=true)
        {
            buttons.gameObject.SetActive(!hidden);
        }
        public void EnableProducerButton(bool enabled)
        {
            producerButton.interactable = enabled;
        }
        public void EnableConsumerButton(bool enabled)
        {
            consumerButton.interactable = enabled;
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
                Vector3 worldPos = mainCam.ScreenToWorldPoint(mousePos);

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

                ShowButtons(true);
            }
        }
        IEnumerator buttonsRoutine;
        private void ShowButtons(bool showing, float duration=.5f)
        {
            buttons.GetComponent<CanvasGroup>().interactable = showing;
            if (buttonsRoutine != null) {
                StopCoroutine(buttonsRoutine);
            }
            var start = showing? new Vector2(0,0) : new Vector2(1,0);
            var end = showing? new Vector2(1,0) : new Vector2(0,0);
            StartCoroutine(buttonsRoutine = Tweens.Pivot(buttons, start, end, duration));
        }
    }
}
