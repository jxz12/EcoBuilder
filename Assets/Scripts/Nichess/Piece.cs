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
            private set { mr.material.color = value; }
        }
        [SerializeField] MeshFilter mf;
        public Mesh Shape {
            set { mf.mesh = value; }
        }
        [SerializeField] float lightPce, darkPce;

        public int Idx { get; private set; }
        public bool StaticPos { get; set; }
        public bool StaticRange { get; set; }

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
        public void Remove()
        {
            // TODO: some animation here too
            Destroy(gameObject);
        }

        [SerializeField] float uvRange = .7f;
        float y=.5f, u=0, v=0;
        public void Colour2D(float a, float b)
        {
            u = uvRange * (a-.5f);
            v = uvRange * (b-.5f);
            Col = ColorHelper.YUVtoRGBtruncated(y, u, v);
            OnColoured.Invoke(Col);
        }

        public Square NichePos { get; private set; }
        public event Action<Color> OnColoured;
        public event Action OnSelected;
        public void PlaceOnSquare(Square newParent)
        {
            if (NichePos != null)
                NichePos.Occupant = null;

            transform.SetParent(newParent.transform, false);
            NichePos = newParent;
            if ((newParent.X + newParent.Y) % 2 == 0)
            {
                y = lightPce;
            }
            else
            {
                y = darkPce;
            }
            Col = ColorHelper.YUVtoRGBtruncated(y, u, v);
            OnColoured.Invoke(Col);

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