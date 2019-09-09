using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System.Diagnostics; // TIME TESTING

namespace EcoBuilder.Archie
{


    public class Maw : MonoBehaviour
{

    //mouth controls
	// public GameObject mouth_list;
    public Mesh[] mouth_meshes = new Mesh[4];

	// local variables
	MeshFilter mesh_filter;
    Mesh new_mouth;

    public void Refresh(Vector3 position, float size = 1, int mouth_type = 0, bool first_time = true) {
        Stopwatch maw_startup_time = new Stopwatch(); // TIME TESTING
        maw_startup_time.Start();// TIME TESTING
        if ( first_time )
        {
            mesh_filter = GetComponent<MeshFilter>();
            new_mouth = new Mesh();
        }
        maw_startup_time.Stop(); // TIME TESTING
        UnityEngine.Debug.Log("maw startup time: " + maw_startup_time.Elapsed); // TIME TESTING

        // resizing mouth
        Stopwatch maw_resize_time = new Stopwatch(); // TIME TESTING
        maw_resize_time.Start();// TIME TESTING
        var bv = new Vector3[mouth_meshes[mouth_type].vertices.Length];
        var size_vector = new Vector3(size,size,size);
        for (int i = 0; i < mouth_meshes[mouth_type].vertices.Length; i++)
        {
            // bv[i] = (mouth.vertices[i] * size); // main
            bv[i] = mouth_meshes[mouth_type].vertices[i];
            bv[i].Scale(size_vector); // for some reason, this is slightly faster than the one labeled "main"
        }
        maw_resize_time.Stop(); // TIME TESTING
        UnityEngine.Debug.Log("maw resize time: " + maw_resize_time.Elapsed); // TIME TESTING

        //shift mouth to edge of face
        Stopwatch maw_shift_time = new Stopwatch(); // TIME TESTING
        maw_shift_time.Start();// TIME TESTING
        var offset = new Vector3(position.z, position.y, -position.x);
        float scale = 1.0f/20.0f;
        offset.Scale(new Vector3(scale,scale,scale));
        for (int i = 0; i < bv.Length; i++)
        {
            bv[i] = bv[i] + offset; // converts global coordinates to local ones
        }
        new_mouth.vertices = bv;
        new_mouth.triangles = mouth_meshes[mouth_type].triangles;
        new_mouth.uv = mouth_meshes[mouth_type].uv;
        new_mouth.normals = mouth_meshes[mouth_type].normals;
        maw_shift_time.Stop(); // TIME TESTING
        UnityEngine.Debug.Log("maw shift time: " + maw_shift_time.Elapsed); // TIME TESTING

        mesh_filter.mesh = new_mouth;

    }
}

}