using UnityEngine;

namespace EcoBuilder.NodeLink
{
    [RequireComponent(typeof(Animator))]
    public class Node : MonoBehaviour
    {
        public int Idx { get; private set; }
        public Color Col {
            get { return mr!=null? mr.material.color : Color.white; }
            set { if (mr!=null) mr.material.color = value; }
        }
        public Vector3 TargetPos { get; set; }
        public float TargetSize { get; set; } = .5f;
        public bool IsSourceOnly { get; set; }
        public bool IsTargetOnly { get; set; }

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
            TargetPos = pos;

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
            Mesh shapeMesh = shapeObject.GetComponent<MeshFilter>().mesh;
            GetComponent<MeshFilter>().mesh = shapeMesh; // flash?
        }
        public void Reshape(GameObject shapeObject)
        {
            Destroy(shape.gameObject);
            shape = shapeObject.transform;
            shape.SetParent(transform, false);
            // TargetSize = 1;

            mr = shapeObject.GetComponent<MeshRenderer>();
            if (mr == null)
                throw new System.Exception("shape has no meshrenderer!");
        }

        public void Outline()
        {
            gameObject.AddComponent<cakeslice.Outline>();
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