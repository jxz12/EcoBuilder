using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.Levels
{
    public abstract class Tutorial : MonoBehaviour
    {
        protected UI.Help help;
        protected UI.Inspector inspector;
        protected UI.Score score;
        protected UI.MoveRecorder recorder;
        protected UI.Constraints constraints;
        protected NodeLink.NodeLink nodelink;
        protected Model.Model model;

        protected Image pointer;
        // [SerializeField] protected Sprite point, grab, pan;
        
        protected Vector2 targetPos, targetSize, targetAnchor;
        private Vector2 velocity, sizocity, anchosity;
        protected float targetZRot;
        private float zRotation, rotocity;
        [SerializeField] protected float smoothTime=.2f;

        protected RectTransform rt;
        protected Vector2 canvasRefRes { get; private set; }

        void Start()
        {
            pointer = GetComponent<Image>();
            rt = GetComponent<RectTransform>();

            var rtCanvas  = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            canvasRefRes = new Vector2(rtCanvas.sizeDelta.x, rtCanvas.sizeDelta.y);

            targetPos = rt.anchoredPosition;
            targetSize = rt.sizeDelta;
            targetAnchor = rt.anchorMin;
            targetZRot = zRotation = rt.rotation.eulerAngles.z;

            StartCoroutine(WaitToStart());
        }
        IEnumerator WaitToStart()
        {
            // ugly as HECK
            while ((help = FindObjectOfType<UI.Help>()) == null)
                yield return null;
            while ((inspector = FindObjectOfType<UI.Inspector>()) == null)
                yield return null;
            while ((score = FindObjectOfType<UI.Score>()) == null)
                yield return null;
            while ((constraints = FindObjectOfType<UI.Constraints>()) == null)
                yield return null;
            while ((recorder = FindObjectOfType<UI.MoveRecorder>()) == null)
                yield return null;
            while ((nodelink = FindObjectOfType<NodeLink.NodeLink>()) == null)
                yield return null;
            while ((model = FindObjectOfType<Model.Model>()) == null)
                yield return null;

            StartLesson();
        }
        protected abstract void StartLesson();
        protected Action Detach;
        void OnDestroy()
        {
            Detach?.Invoke();
        }

        void FixedUpdate()
        {
            rt.anchoredPosition = Vector2.SmoothDamp(rt.anchoredPosition, targetPos, ref velocity, smoothTime);
            rt.sizeDelta = Vector2.SmoothDamp(rt.sizeDelta, targetSize, ref sizocity, smoothTime);
            rt.anchorMax = rt.anchorMin = Vector2.SmoothDamp(rt.anchorMin, targetAnchor, ref anchosity, smoothTime);
            zRotation = Mathf.SmoothDamp(zRotation, targetZRot, ref rotocity, smoothTime);
            rt.rotation = Quaternion.Euler(0, 0, zRotation);
        }

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