
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.Archie
{
    public class Oculus : MonoBehaviour
    {

        public Mesh[] eye_meshes = new Mesh[8];

        // local variables
        MeshFilter mesh_filter;
        Mesh eye;
        Mesh new_eye;
        Mesh mirror_eye;
        Mesh final_eyes;


        //timing variables
        float time_A = 0;
        float time_B;
        public Mesh blink_mesh;
        Vector3 pos;
        float siz;
        bool currently_blinking = false;

        void Update()
        {
            time_B = Time.time;
            if (!currently_blinking)
            {
                if ((time_B - time_A) > 4.7F)
                {
                    eye = blink_mesh;
                    Refresh(pos, siz, type, true);
                    currently_blinking = true;
                }
            }
            else
            {
                if ((time_B - time_A) > 2.0F)   // Blinking last for two seconds
                {
                    time_A = Time.time;
                    Refresh(pos, siz, type, false);
                    currently_blinking = false;
                }
            }
        }

        private int type;

        public void Refresh(Vector3 position, float size = 1, int eye_type = 0, bool here = false, bool first_time = true)
        {
            if (first_time)
            {
                time_A = Time.time;
                mesh_filter = GetComponent<MeshFilter>();
            }
            // else
            // {
            //     // new_eye.normals = null;
            //     // new_eye.uv = null;
            //     // new_eye.triangles = null;
            //     new_eye.vertices = null;

            //     mirror_eye.normals = null;
            //     mirror_eye.uv = null;
            //     mirror_eye.triangles = null;
            //     mirror_eye.vertices = null;
            // }
            pos = position;
            siz = size;
            currently_blinking = false;
            type = eye_type;

            if (!here)
            {
                time_A = Time.time;
                eye = eye_meshes[eye_type];
            }

            // resizing eye
            var bv = new Vector3[eye.vertices.Length];
            var bt = new int[eye.triangles.Length];
            var size_vector = new Vector3(size, size, size);
            for (int i = 0; i < eye.vertices.Length; i++)
            {
                bv[i] = eye.vertices[i];
                bv[i].Scale(size_vector); // for some reason, this is slightly faster than the one labeled "main"
            };

            //shifting eye to edge of face (metaball)
            var offset = new Vector3(position.z, position.y, -position.x);
            float scale = 1.0f / 20.0f;
            offset.Scale(new Vector3(scale, scale, scale));
            for (int i = 0; i < bv.Length; i++)
            {
                bv[i] = bv[i] + offset; // converts global coordinates to local ones
            }

            new_eye = new Mesh();
            new_eye.vertices = bv;
            new_eye.triangles = eye.triangles;
            new_eye.uv = eye.uv;
            new_eye.normals = eye.normals;

            // mirroring eye
            for (int i = 0; i < bv.Length; i++)
            {
                bv[i] = new Vector3(-bv[i].x, bv[i].y, bv[i].z);
            }
            for (int i = 0; i < eye.triangles.Length; i++)
            {
                bt[i] = eye.triangles[eye.triangles.Length- i - 1];
            }

            mirror_eye = new Mesh();
            mirror_eye.vertices = bv;
            mirror_eye.triangles = bt;
            mirror_eye.uv = eye.uv;
            mirror_eye.RecalculateNormals();

            CombineInstance[] combine = new CombineInstance[2];
            combine[0].mesh = new_eye;
            combine[1].mesh = mirror_eye;

            final_eyes = new Mesh();
            final_eyes.CombineMeshes(combine, true, false);

            mesh_filter.mesh = final_eyes;
        }
    }

}