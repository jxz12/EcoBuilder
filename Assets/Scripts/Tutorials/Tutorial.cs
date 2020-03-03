using System;
using System.Collections;
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

            var rootRT = GetComponentInParent<Canvas>().rootCanvas.GetComponent<RectTransform>();
            canvasRefRes = new Vector2(rootRT.sizeDelta.x, rootRT.sizeDelta.y);
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
            pointerRT.anchorMax = pointerRT.anchorMin = Vector2.SmoothDamp(pointerRT.anchorMin, targetAnchor, ref anchosity, smoothTime);
            zRotation = Mathf.SmoothDamp(zRotation, targetZRot, ref rotocity, smoothTime);
            pointerRT.rotation = Quaternion.Euler(0, 0, zRotation);

            float mag = (pointerRT.sizeDelta-targetSize).sqrMagnitude;
            if (mag > 1) {
                pointerRT.sizeDelta = Vector2.SmoothDamp(pointerRT.sizeDelta, targetSize, ref sizocity, smoothTime);
            } else if (mag > 0) {
                pointerRT.sizeDelta = targetSize;
            }
        }

        protected void Point()
        {
            GetComponent<Animator>().SetInteger("State", 0);
        }
        private Vector2 ToAnchoredPos(Vector3 worldPos)
        {
            Vector2 viewportPos = mainCam.WorldToViewportPoint(worldPos);
            return new Vector2(viewportPos.x*canvasRefRes.x, viewportPos.y*canvasRefRes.y);
        }
        protected IEnumerator DragAndDrop(Transform grab, Transform drop, float period)
        {
            float start = Time.time;

            if (GameManager.Instance.ReverseDragDirection)
            {
                Transform temp = grab;
                grab = drop;
                drop = temp;
            }
            transform.position = ToAnchoredPos(grab.position);

            targetAnchor = new Vector3(0f,0f);
            while (true)
            {
                if (((Time.time - start) % period) < (period/2f)) {
                    targetPos = ToAnchoredPos(drop.position) + new Vector2(0,-20);
                } else {
                    targetPos = ToAnchoredPos(grab.position) + new Vector2(0,-20);
                }

                if (((Time.time - start + 3*period/5) % period) < (period/2f)) {
                    GetComponent<Animator>().SetInteger("State", 2); // pan
                } else {
                    GetComponent<Animator>().SetInteger("State", 1); // grab
                }
                yield return null;
            }
        }
        protected IEnumerator Track(Transform tracked)
        {
            while (true)
            {
                targetPos = ToAnchoredPos(tracked.position) + new Vector2(0,-20);
                yield return null;
            }
        }
        protected IEnumerator ShuffleOnSlider(float period, float yPos)
        {
            float start = Time.time - period/4;
            targetAnchor = new Vector2(.5f, 0);
            targetSize = new Vector2(50,50);
            targetZRot = 0;
            smoothTime = .7f;
            GetComponent<Animator>().SetInteger("State", 1); // grab
            while (true)
            {
                if (((Time.time - start) % period) < (period/2f))
                {
                    targetPos = new Vector2(-60,yPos);
                }
                else
                {
                    targetPos = new Vector2(130,yPos);
                    smoothTime = 1f;
                }
                yield return null;
            }
        }
        protected IEnumerator WaitThenDo(float seconds, Action Todo)
        {
            if (seconds > 0) {
                yield return new WaitForSeconds(seconds);
            }
            Todo();
        }


        ////////////////////////
        // attaching to events

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