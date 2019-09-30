using System;
using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder
{
    public abstract class Tutorial : MonoBehaviour
    {
        protected UI.Help help;
        protected UI.Inspector inspector;
        protected UI.StatusBar status;
        protected UI.MoveRecorder recorder;
        protected NodeLink.NodeLink nodelink;

        protected Image pointer;
        [SerializeField] protected Sprite point, grab, pan;
        
        protected Vector2 targetPos, targetSize, targetAnchor;
        private Vector2 velocity, sizocity, anchosity;
        protected float targetZRot;
        private float zRotation, rotocity;
        [SerializeField] protected float smoothTime;

        protected RectTransform rt;
        protected Vector2 canvasRefRes { get; private set; }

        void Start()
        {
            // ugly as HECK
            help = FindObjectOfType<UI.Help>();
            inspector = FindObjectOfType<UI.Inspector>();
            status = FindObjectOfType<UI.StatusBar>();
            recorder = FindObjectOfType<UI.MoveRecorder>();
            nodelink = FindObjectOfType<NodeLink.NodeLink>();

            pointer = GetComponent<Image>();
            rt = GetComponent<RectTransform>();

            var rtCanvas  = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            canvasRefRes = new Vector2(rtCanvas.sizeDelta.x, rtCanvas.sizeDelta.y);

            targetPos = rt.anchoredPosition;
            targetSize = rt.sizeDelta;
            targetAnchor = rt.anchorMin;
            targetZRot = rt.rotation.eulerAngles.z;

            StartLesson();
        }
        void FixedUpdate()
        {
            rt.anchoredPosition = Vector2.SmoothDamp(rt.anchoredPosition, targetPos, ref velocity, smoothTime);
            rt.sizeDelta = Vector2.SmoothDamp(rt.sizeDelta, targetSize, ref sizocity, smoothTime);
            rt.anchorMax = rt.anchorMin = Vector2.SmoothDamp(rt.anchorMin, targetAnchor, ref anchosity, smoothTime);
            zRotation = Mathf.SmoothDamp(zRotation, targetZRot, ref rotocity, smoothTime);
            rt.rotation = Quaternion.Euler(0, 0, zRotation);
        }
        protected abstract void StartLesson();

        protected void Point()
        {
            GetComponent<Animator>().SetInteger("State", 0);
        }
        protected void Grab()
        {
            GetComponent<Animator>().SetInteger("State", 1);
        }
        protected void Pan()
        {
            GetComponent<Animator>().SetInteger("State", 2);
        }
        protected Vector2 ScreenPos(Vector2 viewportPos)
        {
            return new Vector2(viewportPos.x*canvasRefRes.x, viewportPos.y*canvasRefRes.y);
        }
    }
}