using UnityEngine;

namespace EcoBuilder.NodeLink
{
    public class Link : MonoBehaviour
    {
        [SerializeField] LineRenderer lr;

        public Node Source { get; private set; }
        public Node Target { get; private set; }

        public float Width {
            get { return lr.widthMultiplier; }
            set { lr.widthMultiplier = value; }
        }
        public float TileSpeed { get; set; } = .1f;

        public void Init(Node source, Node target, bool curved=false)
        {
            Source = source;
            Target = target;
            name = Source.Idx + " " + Target.Idx;
            Curved = curved;
        }

        [SerializeField] float curveRatio = .2f;
        [SerializeField] int curveSegments = 5;
        bool Curved { get; set; }

        private void Update()
        {
            if (Curved)
            {
                lr.positionCount = curveSegments + 1;

                var from = Source.transform.position;
                var to = Target.transform.position;

                var extra = (to - from) * .1f;
                from += extra;
                to -= extra;

                var mid = (from+to) / 2;
                mid += Vector3.Cross(to-from, Vector3.back) * curveRatio;

                lr.SetPosition(0, from);
                for (int i=1; i<=curveSegments-1; i++)
                {
                    float t = (float)i / curveSegments; 
                    float t1 = 1-t;
                    Vector3 pos = from*t1*t1 + mid*2*t*t1 + to*t*t; // Bezier curve
                    lr.SetPosition(i, pos);
                }
                lr.SetPosition(curveSegments, to);
            }
            else
            {
                lr.positionCount = 2;
                lr.SetPosition(0, Source.transform.position);
                lr.SetPosition(1, Target.transform.position);
            }
            lr.startColor = Target.Col;
            lr.endColor = Source.Col;
            // lr.material.color = Target.Col;

            lr.material.mainTextureOffset -= new Vector2(TileSpeed, 0);
        }
    }
}