using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EcoBuilder
{
    public class Effect : MonoBehaviour
    {
        AudioSource tone;
        void Start()
        {
            if (GetComponent<RectTransform>() == null) {
                StartCoroutine(LookAtCamera());
            }
            tone = GetComponent<AudioSource>();

            IEnumerator LookAtCamera()
            {
                Camera cam = Camera.main;
                while (true)
                {
                    transform.LookAt(transform.position + cam.transform.rotation * Vector3.back, cam.transform.rotation * Vector3.up);
                    yield return null;
                }
            }
        }
        public void Destroy()
        {
            float remaining = (tone!=null && tone.isPlaying)? Mathf.Max(tone.clip.length - tone.time, 0) : 0;
            Destroy(gameObject, remaining);
        }
    }
}