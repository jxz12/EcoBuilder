// this code is only for purposes of testing the 3D quickhull code I wrote
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace EcoBuilder.Archie
{
    // using QuickHull3D;
    
    public class RandomPointGenerator : MonoBehaviour
    {
        public Mesh RandomVertices(int Seed, int number_of_vertices)
        {
            UnityEngine.Random.InitState(Seed);
            var Cloud = new Mesh();
            var points = new List<Vector3>();
            for ( int i = 0 ; i < number_of_vertices ; i++ )
            {
                var tmp = new Vector3(UnityEngine.Random.Range(-100, 100),UnityEngine.Random.Range(-100, 100),UnityEngine.Random.Range(-100, 100));
                UnityEngine.Debug.Log(tmp);
                points.Add(tmp);
            }
            Cloud.vertices = points.ToArray();
            return Cloud;
        }

        public Mesh simple()
        {
            var Cloud = new Mesh();
            var points = new List<Vector3>();
            points.Add(new Vector3(0,0,-20));
            points.Add(new Vector3(0,0, 20));
            points.Add(new Vector3(0, 10, 0));
            points.Add(new Vector3(-10, 0, 0));

            // points.Add(new Vector3(0, 0, 0));
            // points.Add(new Vector3(-10, -10, 20));
            // points.Add(new Vector3(-10, -10, -20));
            // points.Add(new Vector3(20, -10, 0));
            // points.Add(new Vector3(0, 10, 0));
            Cloud.vertices = points.ToArray();
            return Cloud;
        }

        public int seed = 0;
        public int number_of_points = 5;
        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.U))
            {
                GetComponent<MeshFilter>().mesh = RandomVertices(seed,number_of_points);
                // GetComponent<MeshFilter>().mesh = simple();
            }
            if (Input.GetKeyDown(KeyCode.H))
            {
                GetComponent<MeshFilter>().mesh.triangles = QuickHull3D.MakeHull(GetComponent<MeshFilter>().mesh.vertices);
            }
            
        }
    }

}