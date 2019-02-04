using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace EcoBuilder.Nichess
{
    public class Piece : MonoBehaviour
    {
        [SerializeField] Transform meshTransform;
        [SerializeField] MeshRenderer baseRenderer;
        [SerializeField] MeshFilter baseMesh;
        [SerializeField] MeshFilter numberMesh;
        // [SerializeField] MeshRenderer numberRenderer;
        Animator anim;

        public Color Col {
            get { return baseRenderer.material.color; }
            set { baseRenderer.material.color = value; }
        }
        public int Idx { get; private set; }
        private float lightness;
        public float Lightness {
            private get { return lightness; }
            set {
                lightness = value;
                Col = ColorHelper.SetY(NichePos.Col, value);
            }
        }

        public bool StaticPos { get; set; }
        public bool StaticRange { get; set; }

        private void Awake()
        {
            anim = GetComponent<Animator>();
        }
        public void Init(int idx)
        {
            Idx = idx;
            name = idx.ToString();
            // Lightness = lightness;
            // this.lightness = lightness;
        }
        public void SetBaseMesh(Mesh mesh)
        {
            baseMesh.mesh = mesh;
        }
        public void SetNumberMesh(Mesh mesh)
        {
            numberMesh.mesh = mesh;
        }

        private void ParentTo(Transform parent)
        {
            // below is required to get colliders to work with eventsystem
            transform.parent = parent;
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        //////////////////////////////////////////////////////////////////

        public event Action OnSelected;
        public event Action OnPosChanged;
        public event Action OnThrownAway;

        Square nichePos, nicheMin, nicheMax;
        public Square NichePos { 
            get { return nichePos; }
            set {
                // this setter probably makes more sense as a function
                if (value == nichePos)
                    return;
                
                ParentTo(value.transform);
                // transform.SetParent(value.transform);
                nichePos = value;
                Col = ColorHelper.SetY(value.Col, Lightness);

                if (OnPosChanged != null)
                    OnPosChanged();
            }
        }
        public Square NicheMin {
            get { return nicheMin; }
            set { nicheMin = value; }
        }
        public Square NicheMax {
            get { return nicheMax; }
            set { nicheMax = value; }
        }

        public void Select()
        {
            anim.SetTrigger("Select");
            OnSelected();
        }
        public void Deselect()
        {
            anim.SetTrigger("Deselect");
        }
        public void ThrowAway()
        {
            anim.SetTrigger("Destroy"); // Destroy(gameObject) called in script
            OnThrownAway();
        }
        public void ThrowAwayExternal()
        {
            anim.SetTrigger("Destroy");
        }

        public void DestroyMe()
        {
            Destroy(gameObject);
        }
        public void Lift()
        {
            anim.SetTrigger("Lift");
        }
        public void Drop()
        {
            anim.SetTrigger("Drop");
        }

        public bool IsResourceOf(Piece consumer)
        {
            if (NichePos == null || consumer.NicheMin == null || consumer.NicheMax == null)
                return false;

            int resX = NichePos.X;
            int resY = NichePos.Y;
            int conL = consumer.NicheMin.X;
            int conR = consumer.NicheMax.X;
            int conB = consumer.NicheMin.Y;
            int conT = consumer.NicheMax.Y;
            if (conL<=resX && resX<=conR && conB<=resY && resY<=conT)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool IsPotentialNewSquare(Square newSquare)
        {
            if (newSquare.Occupant != null)
                return false;

            if (NicheMin == null && NicheMax == null)
                return true;

            // check if within range
            int xNew = newSquare.X, yNew = newSquare.Y;
            if (NicheMin.X<=xNew && xNew<=NicheMax.X && NicheMin.Y<=yNew && yNew<=NicheMax.Y)
            {
                return false;
            }
            return true;
        }
        public void LookAt(Vector3 position)
        {
            // meshTransform.LookAt(position, Vector3.up);
        }
        public void LookNowhere()
        {
            // meshTransform.localRotation = Quaternion.identity;
        }


    }
}