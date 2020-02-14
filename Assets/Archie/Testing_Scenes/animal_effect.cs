using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EcoBuilder.Archie
{
    public class animal_effect : MonoBehaviour
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