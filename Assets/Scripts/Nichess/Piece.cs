using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace EcoBuilder.Nichess
{
    public class Piece : MonoBehaviour
    {
        [SerializeField] MeshRenderer mr;
        public Color Col {
            get { return mr.material.color; }
            set { mr.material.color = value; }
        }
        [SerializeField] MeshFilter mf;
        public Mesh Shape {
            set { mf.mesh = value; }
        }
        public int Idx { get; private set; }

        public bool StaticPos { get; set; }
        public bool StaticRange { get; set; }

        private void Awake()
        {
        }
        public void Init(int idx)
        {
            Idx = idx;
            name = idx.ToString();
        }

        public void Select()
        {
            // TOOD: some animation here
            OnSelected();
        }
        public Square NichePos { get; private set; }
        public event Action OnPosChanged;
        public event Action OnSelected;
        public void PlaceOnSquare(Square newParent)
        {
            if (NichePos != null)
                NichePos.Occupant = null;

            transform.SetParent(newParent.transform, false);
            NichePos = newParent;
            OnPosChanged();

            // // below is required to get colliders to work with eventsystem
            // transform.parent = newParent.transform;
            // transform.localScale = Vector3.one;
            // transform.localPosition = Vector3.zero;
            // transform.localRotation = Quaternion.identity;
        }
        public Square NicheMin { get; set; }
        public Square NicheMax { get; set; }

        public bool SquareInNiche(Square toConsume)
        {
            if (NicheMin == null || NicheMax == null)
                return false;

            int resX = toConsume.X;
            int resY = toConsume.Y;

            int conL = NicheMin.X;
            int conR = NicheMax.X;
            int conB = NicheMin.Y;
            int conT = NicheMax.Y;

            if (conL<=resX && resX<=conR && conB<=resY && resY<=conT)
                return true;
            else
                return false;
        }
        public bool IsResourceOf(Piece consumer)
        {
            if (NichePos == null)
                return false;

            return consumer.SquareInNiche(NichePos);
        }

















        /*
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
        */

    }
}