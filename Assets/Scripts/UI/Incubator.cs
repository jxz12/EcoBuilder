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
        public event Action OnDropped;

        [SerializeField] RectTransform incubatedParent;
        [SerializeField] Image pickupZone, dropZone;
        [SerializeField] float planeDistance;

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
        }
        public void StartIncubation()
        {
            GetComponent<Animator>().SetBool("Droppable", false);
            GetComponent<Animator>().SetTrigger("Incubate");
        }
        public void Unincubate()
        {
            GetComponent<Animator>().SetTrigger("Unincubate");
            Destroy(incubatedObj);
            incubatedObj = null;
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
            }
        }
    }
}
