using UnityEngine;
using System.Collections.Generic;

namespace EcoBuilder.NodeLink
{
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] Material green, red;
        [SerializeField] float health;
        MeshRenderer mr;
        MeshFilter mf;

        List<Vector3> verts;
        void Awake()
        {
            mf = gameObject.AddComponent<MeshFilter>();
            mr = gameObject.AddComponent<MeshRenderer>();

            verts = new List<Vector3>()
            {
                new Vector3(-1,-1,-1),
                new Vector3(-1,-1, 1),
                new Vector3(-1, 1, 1),
                new Vector3(-1, 1,-1),

                new Vector3( 0,-1,-1),
                new Vector3( 0,-1, 1),
                new Vector3( 0, 1, 1),
                new Vector3( 0, 1,-1),

                new Vector3( 1,-1,-1),
                new Vector3( 1,-1, 1),
                new Vector3( 1, 1, 1),
                new Vector3( 1, 1,-1),
            };
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
            }, 0);
            mf.mesh.SetTriangles(new int[]
            {
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
            mr.materials = new Material[]{ green, red };
        }
        void Update()
        {
            float x = -1 + 2*health;
            verts[4] = new Vector3( x,-1,-1);
            verts[5] = new Vector3( x,-1, 1);
            verts[6] = new Vector3( x, 1, 1);
            verts[7] = new Vector3( x, 1,-1);

            mf.mesh.SetVertices(verts);
        }
    }
}