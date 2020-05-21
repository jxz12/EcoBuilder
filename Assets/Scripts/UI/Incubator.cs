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
        [SerializeField] Canvas rootCanvas, thisCanvas;

        void Awake()
        {
            Assert.IsTrue(rootCanvas.isRootCanvas, "incubator must be a root canvas");
            pickupZone.transform.localScale = Vector3.zero; // this prob shouldn't be here but I like seeing things in editor
        }
        float dropZoneAlpha=0;
        GameObject incubatedObj;
        public void StartIncubation(GameObject toIncubate)
        {
            incubatedObj = toIncubate;
            incubatedObj.transform.SetParent(incubatedParent, false);
            dropZoneAlpha = 0;
            StartCoroutine(Grow(1));
            StartCoroutine(ShowDroppable());
        }
        IEnumerator Grow(float scaleTarget, float duration=.5f)
        {
            float tStart = Time.time;
            if (scaleTarget > 0) {
                thisCanvas.enabled = true;
            }
            float scaleStart = pickupZone.transform.localScale.x;
            while (Time.time < tStart+duration)
            {
                float t = Tweens.QuadraticInOut((Time.time-tStart)/duration);
                float scale = Mathf.Lerp(scaleStart, scaleTarget, t);
                pickupZone.transform.localScale = new Vector3(scale,scale,1);
                yield return null;
            }
            pickupZone.transform.localScale = new Vector3(scaleTarget,scaleTarget,1);
            if (scaleTarget == 0) {
                thisCanvas.enabled = false;
            }
        }
        IEnumerator ShowDroppable(float smoothTime=.1f)
        {
            var dropCol = dropZone.color;
            float dropocity = 0;
            while (incubatedObj != null)
            {
                dropCol.a = Mathf.SmoothDamp(dropCol.a, dropZoneAlpha, ref dropocity, smoothTime);
                dropZone.color = dropCol;
                yield return null;
            }
        }
        public void UnincubateAndDestroy()
        {
            StartCoroutine(Grow(0));
            Destroy(incubatedObj);
            incubatedObj = null;
            StartCoroutine(StopShowDroppable(.2f));
            IEnumerator StopShowDroppable(float duration=.2f)
            {
                float tStart = Time.time;
                var dropCol = dropZone.color;
                float aStart = dropCol.a;
                while (Time.time < tStart+duration)
                {
                    float t = Tweens.CubicOut((Time.time-tStart)/duration);
                    dropCol.a = Mathf.Lerp(aStart, 0, t);
                    dropZone.color = dropCol;
                    yield return null;
                }
                dropCol.a = 0;
                dropZone.color = dropCol;
            }
        }
        public void UnincubateAndRelease()
        {
            StartCoroutine(Grow(0));
            incubatedObj = null;
            StartCoroutine(Release());
            IEnumerator Release(float duration=.5f)
            {
                float tStart = Time.time;
                float xEuler = pickupZone.transform.localRotation.eulerAngles.x;
                float yEuler = pickupZone.transform.localRotation.eulerAngles.y;
                Color dropZoneCol = dropZone.color;
                dropZoneCol.a = .5f;
                dropZone.color = dropZoneCol;
                while (Time.time < tStart+duration)
                {
                    float t = Tweens.QuadraticInOut((Time.time-tStart)/duration);
                    pickupZone.transform.localRotation = Quaternion.Euler(xEuler,yEuler,t*360);
                    dropZoneCol.a = Mathf.Lerp(.5f,0,Tweens.CubicInOut(t));
                    dropZone.color = dropZoneCol;
                    yield return null;
                }
                pickupZone.transform.localRotation = Quaternion.Euler(xEuler, yEuler, 0);
                dropZoneCol.a = 0;
                dropZone.color = dropZoneCol;
            }
        }
        public void Finish()
        {
            if (incubatedObj != null) {
                Destroy(incubatedObj);
            }
            Grow(0);
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
                var dropCol = dropZone.color;
                if (ped.pointerCurrentRaycast.gameObject == dropZone.gameObject) {
                    dropZoneAlpha = .15f;
                } else {
                    dropZoneAlpha = 0;
                }
                dropZone.color = dropCol;

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
            }
        }
    }
}
