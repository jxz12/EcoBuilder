using UnityEngine;

namespace EcoBuilder.NodeLink
{
    [RequireComponent(typeof(Animator))]
    public class Node : MonoBehaviour
    {
        public int Idx { get; private set; }
        public Color Col {
            get { return shape!=null? shape.GetComponent<MeshRenderer>().material.color : Color.black; }
            // set { if (mr!=null) mr.material.color = new Color(value.r, value.g, value.b, mr.material.color.a); }
        }
        public Vector3 StressPos { get; set; }
        public Vector3 FocusPos { get; set; }
        public float Size { get; set; }
        public bool CanBeSource { get; set; } = true;
        public bool CanBeTarget { get; set; } = true;
        public bool Removable { get; set; } = true;

        public Vector3 velocity;  // for public use with Vector3.SmoothDamp

        GameObject shape;

        public void Init(int idx, Vector3 pos, float size, GameObject shapeObject)
        {
            Idx = idx;
            name = idx.ToString();
            transform.localPosition = StressPos = FocusPos = pos;
            Size = size;

            shape = shapeObject;
            shape.transform.SetParent(transform, false);
            shape.transform.localPosition = Vector3.zero;
            shape.transform.localRotation = Quaternion.identity;

            GetComponent<SphereCollider>().enabled = true;

            // // TODO: change this messiness
            // Mesh outlineMesh = shapeObject.GetComponent<MeshFilter>().mesh;
            // outlineMesh = Instantiate(outlineMesh);
 
            // // https://wiki.unity3d.com/index.php/ReverseNormals
            // Vector3[] normals = outlineMesh.normals;
            // for (int i=0;i<normals.Length;i++)
            //     normals[i] = -normals[i];
            // outlineMesh.normals = normals;
 
            // for (int m=0;m<outlineMesh.subMeshCount;m++)
            // {
            //     int[] triangles = outlineMesh.GetTriangles(m);
            //     for (int i=0;i<triangles.Length;i+=3)
            //     {
            //         int temp = triangles[i + 0];
            //         triangles[i + 0] = triangles[i + 1];
            //         triangles[i + 1] = temp;
            //     }
            //     outlineMesh.SetTriangles(triangles, m);
            // }
            // GetComponent<MeshFilter>().mesh = outlineMesh;
        }
        public void Outline(int colourIdx=0)
        {
            var outline = shape.GetComponent<cakeslice.Outline>();
            if (outline == null)
            {
                outline = shape.AddComponent<cakeslice.Outline>();
            }
            outline.color = colourIdx;
        }
        public void Unoutline()
        {
            if (shape.GetComponent<cakeslice.Outline>() != null)
                Destroy(shape.GetComponent<cakeslice.Outline>());
        }
        bool flashing;
        public void Flash(bool isFlashing)
        {
            flashing = isFlashing;
            // anim.SetBool("Flashing", isFlashing);
        }
        // void Update()
        // {
        //     if ((Time.time*60) % 60 < 30 && flashing)
        //     {
        //         GetComponent<MeshRenderer>().material.color = new Color(1,.01f,.01f,1);
        //     }
        //     else
        //     {
        //         GetComponent<MeshRenderer>().material.color = new Color(.01f,.3f,1,.3f);
        //     }
        // }
        // public void Shake(bool isShaking)
        // {
        //     anim.SetBool("Shaking", isShaking);
        // }
    }
}