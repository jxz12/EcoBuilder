using UnityEngine;

namespace EcoBuilder.NodeLink
{
    [RequireComponent(typeof(Animator))]
    public class Node : MonoBehaviour
    {
        public int Idx { get; private set; }

        public Color Col {
            get { return mr.material.color; }
            set { mr.material.color = value; }
        }
        public Vector3 TargetPos { get; set; }
        public float TargetSize { get; set; }
        //     get { return transform.localScale.x; }
        //     set { transform.localScale = new Vector3(value, value, value); }
        // }
        public bool IsSourceOnly {
            get; set;
        }
        public bool IsSinkOnly {
            get; set;
        }

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
            shape.localScale = Vector3.one * .8f;

            mr = shapeObject.GetComponent<MeshRenderer>();
            if (mr == null)
                throw new System.Exception("shape has no meshrenderer!");

            Mesh shapeMesh = shapeObject.GetComponent<MeshFilter>().mesh; // TODO: change this messiness
            GetComponent<MeshFilter>().mesh = shapeMesh;
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