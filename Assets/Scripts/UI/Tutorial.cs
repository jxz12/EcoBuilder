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
        protected float targetZRotation;
        private float zRotation, rotocity;
        [SerializeField] float smoothTime;

        private RectTransform rt;

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

            StartLesson();
        }
        void FixedUpdate()
        {
            rt.anchoredPosition = Vector2.SmoothDamp(rt.anchoredPosition, targetPos, ref velocity, smoothTime);
            rt.sizeDelta = Vector2.SmoothDamp(rt.sizeDelta, targetSize, ref sizocity, smoothTime);
            rt.anchorMin = rt.anchorMax = Vector2.SmoothDamp(rt.anchorMin, targetAnchor, ref anchosity, smoothTime);
            zRotation = Mathf.SmoothDamp(zRotation, targetZRotation, ref rotocity, smoothTime);
            rt.rotation = Quaternion.Euler(0, 0, zRotation);
        }
        protected abstract void StartLesson();
    }
}