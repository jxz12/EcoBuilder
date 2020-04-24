using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EcoBuilder
{
    public class Planet : MonoBehaviour, IDragHandler
    {
        Animator anim;
        Transform defaultParent;
        void Awake()
        {
            anim = GetComponent<Animator>();
            defaultParent = new GameObject().transform;
            defaultParent.position = new Vector3(0,-2.5f,0);
            defaultParent.localScale = new Vector3(2,2,2);
            transform.SetParent(defaultParent);
            transform.localPosition = Vector2.zero;
            transform.localScale = Vector3.zero;
            TweenToRestPositionFromNextFrame(2f);
        }
        public void ResetParent()
        {
            transform.SetParent(defaultParent, true);
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
                float t = UI.Tweens.CubicInOut((Time.time-startTime)/duration);

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