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
        public Vector3 Pos {
            get { return transform.localPosition; }
            set { transform.localPosition = value;}
        }
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
        public void Inspect()
        {
            anim.SetTrigger("Inspect");
        }
        public void Uninspect()
        {
            anim.SetTrigger("Uninspect");
        }
    }
}