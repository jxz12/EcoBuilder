using UnityEngine;

namespace EcoBuilder.NodeLink
{
    [RequireComponent(typeof(Animator))]
    public class Node : MonoBehaviour
    {
        public int Idx { get; private set; }

        public Color Col {
            get { return mr.material.GetColor("_Color"); }
            set { mr.material.SetColor("_Color", value); }
        }
        public Vector3 TargetPos { get; set; }
        // private Vector3 velocity = Vector3.zero;
        // [SerializeField] private float smoothTime = .2f; // TODO: scale this with body size?
        public float Size {
            get { return shape.localScale.x; }
            set { shape.localScale = new Vector3(value, value, value); }
        }

        Animator anim;
        MeshRenderer mr;
        private void Awake()
        {
            anim = GetComponent<Animator>();
        }

        Transform shape, outline;
        // MeshFilter nodeMesh;
        public void Init(int idx, Vector3 target, GameObject shapeObject)
        {
            Idx = idx;
            name = idx.ToString();
            TargetPos = target;

            shape = shapeObject.transform;
            shape.SetParent(transform, false);
            Size = 1;

            mr = shapeObject.GetComponent<MeshRenderer>();
            if (mr == null)
                throw new System.Exception("shape has no meshrenderer!");

            outline = shape; // TODO: change to a system that copies the shape dynamically!
        }
        public void Reshape(GameObject shapeObject)
        {
            Destroy(shape.gameObject);
            shape = shapeObject.transform;
            shape.SetParent(transform, false);
            Size = 1;

            mr = shapeObject.GetComponent<MeshRenderer>();
            if (mr == null)
                throw new System.Exception("shape has no meshrenderer!");
            
            outline = shape; // TODO: change here too, as above
        }
        // public void TweenToTarget()
        // {
        //     transform.localPosition = Vector3.SmoothDamp(transform.localPosition, TargetPos, ref velocity, smoothTime);
        //     // transform.localPosition = TargetPos;
        // }
        public void Flash()
        {
            anim.SetTrigger("Flash");
        }
        public void Idle()
        {
            anim.SetTrigger("Idle");
        }
        // public void HeavyFlash()
        // {
        //     anim.SetTrigger("Heavy Flash");
        // }
    }
}