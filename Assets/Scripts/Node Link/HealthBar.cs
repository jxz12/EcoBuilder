using UnityEngine;
using System.Collections.Generic;

namespace EcoBuilder.UI
{
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] Vector3 offset;
        [SerializeField] float width, height, depth;
        MeshFilter mf;

        List<Vector3> verts;
        void Awake()
        {
            mf = GetComponent<MeshFilter>();

            verts = new List<Vector3>()
            {
                new Vector3(-width,-height,-depth),
                new Vector3(-width,-height, depth),
                new Vector3(-width, height, depth),
                new Vector3(-width, height,-depth),

                new Vector3( 0,-height,-depth),
                new Vector3( 0,-height, depth),
                new Vector3( 0, height, depth),
                new Vector3( 0, height,-depth),

                new Vector3( width,-height,-depth),
                new Vector3( width,-height, depth),
                new Vector3( width, height, depth),
                new Vector3( width, height,-depth),
            };
            for (int i=0; i<verts.Count; i++)
                verts[i] += offset;

            mf.mesh.SetVertices(verts);

            mf.mesh.subMeshCount = 2;
            mf.mesh.SetTriangles(new int[]
            {
                0,1,2,
                2,3,0,

                1,0,4,
                4,5,1,
                2,1,5,
                5,6,2,
                3,2,6,
                6,7,3,
                0,3,7,
                7,4,0,

                6,5,4,
                4,7,6,
            }, 0);
            mf.mesh.SetTriangles(new int[]
            {
                4,5,6,
                6,7,4,

                5,4,8,
                8,9,5,
                6,5,9,
                9,10,6,
                7,6,10,
                10,11,7,
                4,7,11,
                11,8,4,

                10,9,8,
                8,11,10,
            }, 1);

            mf.mesh.RecalculateNormals();
        }
        public float TargetHealth { get; set; }
        float health = 0;
        public void TweenHealth(float healthTween)
        {
            health = Mathf.Lerp(health, TargetHealth, healthTween);

            float mid = Mathf.Min(-1.01f + 2.02f*health, 1.01f) * width;
            verts[4] = new Vector3( mid,-height,-depth) + offset;
            verts[5] = new Vector3( mid,-height, depth) + offset;
            verts[6] = new Vector3( mid, height, depth) + offset;
            verts[7] = new Vector3( mid, height,-depth) + offset;

            mf.mesh.SetVertices(verts);
        }
    }
}