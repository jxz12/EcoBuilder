using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EcoBuilder.Archie
{
    using clamp;


    public class Pots : MonoBehaviour
    {
        //public interfaces
        public Mesh[] pots;


        // local variables
        MeshFilter mesh_filter;
        Oculus eye_controls;

        public void Setup(){}

        public void Refresh(float size = 1, bool first_time = true) {
            if ( first_time )
            {
                mesh_filter = GetComponent<MeshFilter>();
                eye_controls = gameObject.transform.Find("eyes").gameObject.GetComponent<Oculus>();
            }
            int dice_role = UnityEngine.Random.Range(0, 2);
            Mesh pottery = pots[dice_role];
            mesh_filter.mesh = pots[dice_role];

            eye_controls.Refresh(new Vector3(0.5f,0.1f,0.25f),size/2, (int)security.clamp(0,7,UnityEngine.Random.Range(0, 8)));

        }

    }

}