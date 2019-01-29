using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace EcoBuilder.Nichess
{
    public class Piece : MonoBehaviour
    {
        [SerializeField] MeshRenderer shape;
        [SerializeField] MeshFilter pieceMesh;
        // Animator anim;

        public Color Col {
            get { return shape.material.color; }
            set { shape.material.color = value; }
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
            // anim = GetComponent<Animator>();
        }
        public void Init(int idx)
        {
            Idx = idx;
            name = idx.ToString();
            // Lightness = lightness;
            // this.lightness = lightness;
        }
        public void SetShape(Mesh piece)
        {
            pieceMesh.mesh = piece;
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

        // TODO: replace this with animation!
        public void Select()
        {
            shape.transform.localScale += new Vector3(.3f, .3f, .3f);
            OnSelected();
        }
        public void Deselect()
        {
            shape.transform.localScale -= new Vector3(.3f, .3f, .3f);
        }
        public void ThrowAway()
        {
            OnThrownAway();
            Destroy(gameObject);
        }
        public void Lift()
        {
            shape.transform.localPosition += new Vector3(0, .7f, 0);
        }
        public void Drop()
        {
            shape.transform.localPosition -= new Vector3(0, .7f, 0);
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

        ///////////////////////////////////////////////////////////

        // void DrawLasso()
        // {
        //     if (NicheStart != null && NicheEnd != null)
        //     {
        //         lassoSpoke.enabled = lassoLoop.enabled = true;
        //         DrawLasso(NicheStart, NicheEnd);
        //     }
        //     else 
        //     {
        //         lassoSpoke.enabled = lassoLoop.enabled = false;;
        //     }
        // }
        // void DrawLasso(Square s1, Square s2)
        // {
        //     int x1 = s1.X, x2 = s2.X;
        //     int y1 = s1.Y, y2 = s2.Y;
        //     float lassoStartLocalX = x1<=x2? -.5f : .5f;
        //     float lassoStartLocalY = y1<=y2? -.5f : .5f;

        //     Vector3 lassoStart = s1.transform.TransformPoint(lassoStartLocalX,0,lassoStartLocalY);
        //     Vector3 lassoEnd = s2.transform.TransformPoint(-lassoStartLocalX,0,-lassoStartLocalY);

        //     // some maths to calculate the adjacent corners of the lasso
        //     float nicheWidth = x1<=x2? x2-x1+1 : x2-x1-1;
        //     float nicheHeight = y1<=y2? y2-y1+1 : y2-y1-1;
        //     float rotationAngle = -2 * Mathf.Atan2(nicheHeight, nicheWidth);
        //     float cos = Mathf.Cos(rotationAngle);
        //     float sin = Mathf.Sin(rotationAngle);

        //     // by rotating the corners around the middle
        //     Vector3 lassoMiddle = (lassoEnd + lassoStart) / 2f;
        //     Vector3 v = lassoStart - lassoMiddle;
        //     Vector3 lassoCorner1 = new Vector3(v.x*cos-v.z*sin, 0, v.x*sin+v.z*cos) + lassoMiddle;
        //     v = lassoEnd - lassoMiddle;
        //     Vector3 lassoCorner2 = new Vector3(v.x*cos-v.z*sin, 0, v.x*sin+v.z*cos) + lassoMiddle;

        //     if (x1 <= x2 && y1 <= y2 || x1 > x2 && y1 > y2)
        //         DrawLasso(new Vector3[]{ lassoStart, lassoCorner1, lassoEnd, lassoCorner2 });
        //     else
        //         DrawLasso(new Vector3[]{ lassoStart, lassoCorner2, lassoEnd, lassoCorner1 });
        // }
        // void DrawLasso(Vector3[] v)
        // {
        //     if (v.Length != 4)
        //         throw new Exception("lasso loop is not quadrilateral");
        //     Vector3 handle = transform.position;
        //     Vector3 middle = (v[0] + v[2]) / 2;
        //     Vector3 middle2 = (v[1] + v[3]) / 2;
        //     if (middle != middle2)
        //         throw new Exception("lasso loop is not a rectangle");

        //     float d1 = DistanceFromLine(handle, v[0], v[2]);
        //     float d2 = DistanceFromLine(handle, v[3], v[1]);

        //     Vector3 honda;
        //     if (d1 < 0 && d2 <= 0)
        //         honda = LineIntersection(handle, middle, v[0], v[1]);
        //     else if (d1 < 0 && d2 > 0)
        //         honda = LineIntersection(handle, middle, v[1], v[2]);
        //     else if (d1 >= 0 && d2 > 0)
        //         honda = LineIntersection(handle, middle, v[2], v[3]);
        //     else //if (d1 >= 0 && d2 <= 0)
        //         honda = LineIntersection(handle, middle, v[3], v[0]);

        //     lassoLoop.loop = true;
        //     lassoLoop.positionCount = 4;
        //     lassoLoop.SetPosition(0, v[0]);
        //     lassoLoop.SetPosition(1, v[1]);
        //     lassoLoop.SetPosition(2, v[2]);
        //     lassoLoop.SetPosition(3, v[3]);

        //     lassoSpoke.loop = false;
        //     lassoSpoke.positionCount = 2;
        //     lassoSpoke.SetPosition(0, handle);
        //     lassoSpoke.SetPosition(1, honda);
        // }
        // float DistanceFromLine(Vector3 point, Vector3 line1, Vector3 line2)
        // {
        //     return (point.x - line1.x) * (line2.z - line1.z) - (point.z - line1.z) * (line2.x - line1.x);
        // }
        // Vector3 LineIntersection(Vector3 from, Vector3 to, Vector3 line1, Vector3 line2)
        // {
        //     float x1 = from.x, x2 = to.x, x3 = line1.x, x4 = line2.x;
        //     float y1 = from.z, y2 = to.z, y3 = line1.z, y4 = line2.z;
        //     float xP = ((x1*y2-y1*x2)*(x3-x4) - (x1-x2)*(x3*y4-y3*x4)) / ((x1-x2)*(y3-y4) - (y1-y2)*(x3-x4));
        //     float yP = ((x1*y2-y1*x2)*(y3-y4) - (y1-y2)*(x3*y4-y3*x4)) / ((x1-x2)*(y3-y4) - (y1-y2)*(x3-x4));
        //     return new Vector3(xP, 0, yP);
        // }
    }
}