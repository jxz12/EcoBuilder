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
        public Vector3 TargetPos { get; set; }
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
        public void Init(int idx, Vector3 pos)
        {
            Idx = idx;
            name = idx.ToString();
            transform.localPosition = pos;
            TargetPos = pos;
        }
        public void SetShape(Mesh node, Mesh outline)
        {
            nodeMesh.mesh = node;
            outlineMesh.mesh = outline;
        }
        void FixedUpdate()
        {
            transform.localPosition = Vector3.SmoothDamp(transform.localPosition, TargetPos, ref velocity, smoothTime);
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
        public void HeavyFlash()
        {
            anim.SetTrigger("Heavy Flash");
        }
        public void Idle()
        {
            anim.SetTrigger("Idle");
        }
    }
}