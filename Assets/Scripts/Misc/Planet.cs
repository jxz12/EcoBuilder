using System.Collections;
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
        public void TweenToRestPosition(float duration)
        {
            StopCoroutine(tweenRoutine);
            StartCoroutine(TweenToZero(duration));
        }
        IEnumerator tweenRoutine;
        IEnumerator TweenToZero(float duration)
        {
            Vector3 startPos = transform.localPosition;
            Vector3 startScale = transform.localScale;
            float startTime = Time.time;
            while (Time.time < startTime+duration)
            {
                float t = (Time.time-startTime)/duration;
                // quadratic ease in-out
                if (t < .5f) {
                    t = 2*t*t;
                } else {
                    t = -1 + (4-2*t)*t;
                }
                transform.localPosition = Vector3.Lerp(startPos, Vector3.zero, t);
                transform.localScale = Vector3.Lerp(startScale, Vector3.one, t);
                yield return null;
            }
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;
        }

        // public void ListenToScenes(string s) // also ugly
        // {
        //     if (s == "Menu")
        //     {
        //         anim.SetTrigger("Grow");
        //     }
        //     else if (s == "Play" && !anim.GetCurrentAnimatorStateInfo(0).IsName("Hidden")) // so ugly
        //     {
        //         anim.SetTrigger("Shrink");
        //     }
        // }

        // for user interaction
        // [SerializeField] float minRotationVelocity, rotationMultiplier;
        float rotation, rotationTarget, rotationVelocity;
        void Update()
        {
            rotationTarget += (rotationVelocity + Mathf.Sign(rotationVelocity)*.2f) * Time.deltaTime;
            rotation = Mathf.SmoothDamp(rotation, rotationTarget, ref rotationVelocity, .02f);

            transform.localRotation = Quaternion.Euler(0,rotation,0);
        }
        public void OnDrag(PointerEventData ped)
        {
            rotationTarget -= ped.delta.x * .1f;
        }
    }
}