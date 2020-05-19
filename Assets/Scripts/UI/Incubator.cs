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
        public event Action OnDropped;

        [SerializeField] RectTransform incubatedParent;
        [SerializeField] Image pickupZone, dropZone;
        [SerializeField] Animator squareAnim;
        [SerializeField] Canvas rootCanvas;

        void Awake()
        {
            Assert.IsTrue(rootCanvas.isRootCanvas, "incubator must be a root canvas");
        }
        public void StartIncubation()
        {
            squareAnim.SetBool("Droppable", false);
            squareAnim.SetTrigger("Incubate");
        }
        public void Unincubate()
        {
            squareAnim.SetTrigger("Unincubate");
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
            if (incubatedObj != null)
            {
                Destroy(incubatedObj);
                squareAnim.SetTrigger("Unincubate");
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
                    squareAnim.SetBool("Droppable", true);
                } else {
                    squareAnim.SetBool("Droppable", false);
                }
                Vector3 mousePos = ped.position;
                mousePos.z = rootCanvas.planeDistance - 1;
                Vector3 worldPos = rootCanvas.worldCamera.ScreenToWorldPoint(mousePos);

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
                squareAnim.SetTrigger("Spawn");
            }
        }
    }
}
