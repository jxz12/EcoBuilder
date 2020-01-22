using System.Collections;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace EcoBuilder.Archie
{
public class saver : MonoBehaviour
{
    // a script for exporting procederially generated meshes as an .obj file ( for later use / printing )

    public MeshFilter source;

    int RandomSeed;
    // Start is called before the first frame update
    void Start()
    {
        RandomSeed = UnityEngine.Random.Range(0, System.Int32.MaxValue);
    
        
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S)) {
            Save();
        }
        
    }



    // Update is called once per frame
    void Save()
    {
        Debug.Log("saving...");
        Debug.Log(Application.dataPath);
        StreamWriter sw = new StreamWriter(RandomSeed+".obj",true,Encoding.ASCII);
        // StreamWriter sw = new StreamWriter("Saved_Animals"+Path.PathSeparator+"outtest.txt",true,Encoding.ASCII);
        // StreamWriter sw = new StreamWriter("\\Saved_Animals\\outtest.txt",true,Encoding.ASCII);
        // sw.Write("hello world, I'm in a file!");
        // sw.Close();
        // Debug.Log("saving done!");
        string file_content = "";
        Mesh saving = source.mesh;
        for ( int i = 0 ; i < saving.vertices.Length ; i++ ) {
            file_content += ( "v " + saving.vertices[i].x + " " + saving.vertices[i].y + " " + -saving.vertices[i].z + "\n" );
        }
        Debug.Log("ding...");
        
        for ( int i = 0 ; i < saving.normals.Length ; i++ ) {
            file_content += ( "vn " + saving.normals[i].x + " " + saving.normals[i].y + " " + -saving.normals[i].z + "\n" );
        }
        Debug.Log("dang...");

        for ( int i = 0 ; i < saving.triangles.Length ; i = i + 3 ) {
            file_content += ( "f " + ( saving.triangles[i] + 1 ) + " " + ( saving.triangles[i + 1] + 1 ) + " " + ( saving.triangles[i + 2] + 1 ) + "\n" );
        } 
        Debug.Log("dong...");

        sw.Write(file_content);
        sw.Close();
        Debug.Log("saving done!");
    }
}
}