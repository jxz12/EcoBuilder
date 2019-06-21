using UnityEngine;

namespace EcoBuilder.NodeLink
{
    [RequireComponent(typeof(Animator))]
    public class Node : MonoBehaviour
    {
        public int Idx { get; private set; }
        public Color Col {
            get { return mr!=null? mr.material.color : Color.black; }
            set { if (mr!=null) mr.material.color = value; }
        }
        public Vector3 GoalPos { get; set; }
        public float GoalSize { get; set; }// = .5f;
        public bool CanBeSource { get; set; } = true;
        public bool CanBeTarget { get; set; } = true;
        public bool Removable { get; set; } = true;

        Animator anim;
        MeshRenderer mr;
        private void Awake()
        {
            anim = GetComponent<Animator>();
        }

        Transform shape;
        public void Init(int idx, Vector3 pos, GameObject shapeObject)
        {
            Idx = idx;
            name = idx.ToString();
            GoalPos = pos;

            shape = shapeObject.transform;
            shape.SetParent(transform, false);
            shape.localPosition = Vector3.zero;
            shape.localRotation = Quaternion.identity;
            shape.localScale = Vector3.one * .8f; // TODO: magic number

            GetComponent<SphereCollider>().enabled = true;

            mr = shapeObject.GetComponent<MeshRenderer>();
            if (mr == null)
                throw new System.Exception("shape has no meshrenderer!");

            // TODO: change this messiness
            Mesh outlineMesh = shapeObject.GetComponent<MeshFilter>().mesh;
            outlineMesh = Instantiate(outlineMesh);
 
            // https://wiki.unity3d.com/index.php/ReverseNormals
            Vector3[] normals = outlineMesh.normals;
            for (int i=0;i<normals.Length;i++)
                normals[i] = -normals[i];
            outlineMesh.normals = normals;
 
            for (int m=0;m<outlineMesh.subMeshCount;m++)
            {
                int[] triangles = outlineMesh.GetTriangles(m);
                for (int i=0;i<triangles.Length;i+=3)
                {
                    int temp = triangles[i + 0];
                    triangles[i + 0] = triangles[i + 1];
                    triangles[i + 1] = temp;
                }
                outlineMesh.SetTriangles(triangles, m);
            }
            GetComponent<MeshFilter>().mesh = outlineMesh;
        }
        public void Outline(int colourIdx=0)
        {
            // if (colourIdx < 0 || colourIdx > 3)
            //     throw new System.Exception("bad outline colour");

            var outline = gameObject.AddComponent<cakeslice.Outline>();
            outline.color = colourIdx;
        }
        public void Unoutline()
        {
            Destroy(GetComponent<cakeslice.Outline>());
        }
        public void Flash()
        {
            anim.SetTrigger("Flash");
        }
        public void Idle()
        {
            anim.SetTrigger("Idle");
        }
    }
}