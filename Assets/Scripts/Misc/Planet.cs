﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EcoBuilder
{
    public class Planet : MonoBehaviour, IDragHandler
    {
        Animator anim;
        void Awake()
        {
            anim = GetComponent<Animator>();
            transform.localScale = Vector3.zero;
            TweenToRestPositionFromNextFrame(2f);
        }

        // for animation events
        public void Disable()
        {
            this.enabled = false;
        }
        public void Enable()
        {
            this.enabled = true;
        }
        public void TweenToRestPositionFromNextFrame(float duration)
        {
            if (tweenRoutine != null) {
                StopCoroutine(tweenRoutine);
            }
            StartCoroutine(tweenRoutine = TweenToZero(duration));
        }
        IEnumerator tweenRoutine;
        IEnumerator TweenToZero(float duration)
        {
            yield return null; // smelly wait so that localpos is set to shadow
            float startTime = Time.time;
            Vector3 startPos = transform.localPosition;
            Vector3 startScale = transform.localScale;
            while (Time.time < startTime+duration)
            {
                float t = (Time.time-startTime)/duration;

                // cubic ease in-out
                if (t < .5f) {
                    // t = 2*t*t;
                    t = 4*t*t*t;
                } else {
                    // t = -1 + (4-2*t)*t;
                    t -= 1;
                    t = 4*t*t*t + 1;
                }
                transform.localPosition = Vector3.Lerp(startPos, Vector3.zero, t);
                transform.localScale = Vector3.Lerp(startScale, Vector3.one, t);
                yield return null;
            }
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;
        }

        // for user interaction
        [SerializeField] float minRotationVelocity = .01f;
        float rotation, rotationTarget, rotationVelocity;
        void Update()
        {
            rotationTarget += (rotationVelocity + Mathf.Sign(rotationVelocity)*.2f) * Time.deltaTime;
            rotation = Mathf.SmoothDamp(rotation, rotationTarget, ref rotationVelocity, minRotationVelocity);

            transform.localRotation = Quaternion.Euler(0,rotation,0);
        }
        public void OnDrag(PointerEventData ped)
        {
            rotationTarget -= ped.delta.x * .1f;
        }
    }
}