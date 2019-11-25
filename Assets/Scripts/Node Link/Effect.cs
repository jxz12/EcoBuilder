using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EcoBuilder.NodeLink
{
    public class Effect : MonoBehaviour
    {
        void Update()
        {
        	transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.back, Camera.main.transform.rotation * Vector3.up);
        }
        public void Destroy()
        {
            Destroy(gameObject);
        }
    }
}