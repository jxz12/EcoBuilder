using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EcoBuilder
{
    public class Effect : MonoBehaviour
    {
        [SerializeField] AudioSource tone;
        void Start()
        {
            if (GetComponent<RectTransform>() == null) {
                StartCoroutine(LookAtCamera());
            }
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
        public void FadeAudio()
        {
            // TODO: some static variable to always take the max set in a frame or something
        }
        public void Destroy()
        {
            Destroy(gameObject);
        }
    }
}