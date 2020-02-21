using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace EcoBuilder.Tutorials
{
    // I wish I could make this work by inheriting from Level, but
    // Unity prefab variants aren't that good just yet
    public abstract class Tutorial : MonoBehaviour
    {
        // smellllly
        protected UI.Help help;
        protected UI.Inspector inspector;
        protected UI.Score score;
        protected UI.Recorder recorder;
        protected UI.Constraints constraints;
        protected NodeLink.NodeLink nodelink;
        protected Model.Model model;

        protected Camera mainCam;
        protected Image pointerIm;
        protected RectTransform pointerRT;
        protected Vector2 canvasRefRes { get; private set; }
        
        protected Vector2 targetPos, targetSize, targetAnchor;
        private Vector2 velocity, sizocity, anchosity;
        protected float targetZRot;
        private float zRotation, rotocity;
        protected float smoothTime;

        void Start()
        {
            pointerIm = GetComponent<Image>();
            pointerRT = GetComponent<RectTransform>();

            var rtCanvas  = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            canvasRefRes = new Vector2(rtCanvas.sizeDelta.x, rtCanvas.sizeDelta.y);
            mainCam = Camera.main;

            targetPos = pointerRT.anchoredPosition;
            targetSize = pointerRT.sizeDelta;
            targetAnchor = pointerRT.anchorMin;
            targetZRot = zRotation = pointerRT.rotation.eulerAngles.z;

            smoothTime = .2f;

            InitSmellyReferences();
            StartLesson();
        }

        protected abstract void StartLesson();
        void InitSmellyReferences()
        {
            help = FindObjectOfType<UI.Help>();
            inspector = FindObjectOfType<UI.Inspector>();
            score = FindObjectOfType<UI.Score>();
            constraints = FindObjectOfType<UI.Constraints>();
            recorder = FindObjectOfType<UI.Recorder>();
            nodelink = FindObjectOfType<NodeLink.NodeLink>();
            model = FindObjectOfType<Model.Model>();

            Assert.IsNotNull(help);
            Assert.IsNotNull(inspector);
            Assert.IsNotNull(score);
            Assert.IsNotNull(constraints);
            Assert.IsNotNull(recorder);
            Assert.IsNotNull(nodelink);
            Assert.IsNotNull(model);
        }


        void Update()
        {
            pointerRT.anchoredPosition = Vector2.SmoothDamp(pointerRT.anchoredPosition, targetPos, ref velocity, smoothTime);
            pointerRT.sizeDelta = Vector2.SmoothDamp(pointerRT.sizeDelta, targetSize, ref sizocity, smoothTime);
            pointerRT.anchorMax = pointerRT.anchorMin = Vector2.SmoothDamp(pointerRT.anchorMin, targetAnchor, ref anchosity, smoothTime);
            zRotation = Mathf.SmoothDamp(zRotation, targetZRot, ref rotocity, smoothTime);
            pointerRT.rotation = Quaternion.Euler(0, 0, zRotation);
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

        // protected Action Detach;
        List<Action> smellyListeners = new List<Action>(); // eww
        protected void DetachSmellyListeners()
        {
            foreach (Action A in smellyListeners)
            {
                A();
            }
            smellyListeners.Clear();
        }
        void OnDestroy()
        {
            StopAllCoroutines();
            DetachSmellyListeners();
        }
        protected void AttachSmellyListener(object eventSource, string eventName, Action Callback)
        {
            var eventInfo = eventSource.GetType().GetEvent(eventName);
            eventInfo.AddEventHandler(eventSource, Callback);
            Action Detach = ()=> eventInfo.RemoveEventHandler(eventSource, Callback);
            smellyListeners.Add(Detach);
        }
        protected void AttachSmellyListener<T>(object eventSource, string eventName, Action<T> Callback)
        {
            var eventInfo = eventSource.GetType().GetEvent(eventName);
            eventInfo.AddEventHandler(eventSource, Callback);
            Action Detach = ()=> eventInfo.RemoveEventHandler(eventSource, Callback);
            smellyListeners.Add(Detach);
        }
        protected void AttachSmellyListener<T1,T2>(object eventSource, string eventName, Action<T1,T2> Callback)
        {
            var eventInfo = eventSource.GetType().GetEvent(eventName);
            eventInfo.AddEventHandler(eventSource, Callback);
            Action Detach = ()=> eventInfo.RemoveEventHandler(eventSource, Callback);
            smellyListeners.Add(Detach);
        }
        protected void AttachSmellyListener<T1,T2,T3>(object eventSource, string eventName, Action<T1,T2,T3> Callback)
        {
            var eventInfo = eventSource.GetType().GetEvent(eventName);
            eventInfo.AddEventHandler(eventSource, Callback);
            Action Detach = ()=> eventInfo.RemoveEventHandler(eventSource, Callback);
            smellyListeners.Add(Detach);
        }
    }
}