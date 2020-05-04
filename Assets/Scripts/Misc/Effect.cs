using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EcoBuilder
{
    public class Effect : MonoBehaviour
    {
        void Start()
        {
            if (GetComponent<RectTransform>() == null) {
                StartCoroutine(LookAtCamera());
            }
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
        public void Destroy()
        {
            Destroy(gameObject);
        }
    }
}