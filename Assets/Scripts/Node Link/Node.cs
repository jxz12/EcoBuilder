using UnityEngine;

namespace EcoBuilder.NodeLink
{
    [RequireComponent(typeof(Animator))]
    public class Node : MonoBehaviour
    {
        public int Idx { get; private set; }
        [SerializeField] Transform shape;
        [SerializeField] MeshFilter nodeMesh;
        [SerializeField] MeshFilter outlineMesh;

        public Color Col {
            get { return mr.material.GetColor("_Color"); }
            set { mr.material.SetColor("_Color", value); }
        }
        private Vector3 target;
        public Vector3 Pos {
            get { return target; }
            set { target = value; }
        }
        private Vector3 velocity = Vector3.zero;
        [SerializeField] private float smoothTime = .2f;
        // public Vector3 Pos {
        //     get { return transform.localPosition; }
        //     set { transform.localPosition = value;}
        // }
        public float Size {
            get { return shape.localScale.x; }
            set { shape.localScale = new Vector3(value, value, value); }
        }

        Animator anim;
        MeshRenderer mr;
        private void Awake()
        {
            anim = GetComponent<Animator>();
            mr = shape.GetComponent<MeshRenderer>();
        }

        //Rigidbody rb;
        public void Init(int idx)
        {
            Idx = idx;
            name = idx.ToString();
        }
        public void SetShape(Mesh node, Mesh outline)
        {
            nodeMesh.mesh = node;
            outlineMesh.mesh = outline;
        }
        void FixedUpdate()
        {
            transform.localPosition = Vector3.SmoothDamp(transform.localPosition, target, ref velocity, smoothTime);
        }
        // public void Inspect()
        // {
        //     anim.SetTrigger("Inspect");
        // }
        // public void Uninspect()
        // {
        //     anim.SetTrigger("Uninspect");
        // }
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