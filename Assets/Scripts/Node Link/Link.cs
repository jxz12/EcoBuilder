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

        public void Init(Node source, Node target)
        {
            Source = source;
            Target = target;
            name = Source.Idx + " " + Target.Idx;
        }

        [SerializeField] float curveRatio = .2f;
        [SerializeField] int curveSegments = 5;
        bool curved = false;
        public void Curve()
        {
            curved = true;
        }
        public void Straighten()
        {
            curved = false;
        }

        private void Update()
        {
            if (curved)
            {
                lr.positionCount = curveSegments + 1;
                // lr.positionCount = 3;

                var from = Source.transform.position;
                var to = Target.transform.position;
                var mid = (from+to) / 2;
                // mid += Vector3.Cross(to-from, Vector3.back);
                mid += Vector3.Cross(to-from, Vector3.back) * curveRatio;

                lr.SetPosition(0, from);
                for (int i=1; i<=curveSegments-1; i++)
                {
                    float t = (float)i / curveSegments; 
                    float t1 = 1-t;
                    Vector3 pos = from*t1*t1 + mid*2*t*t1 + to*t*t;
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
            // lr.startColor = Target.Col;
            // lr.endColor = Source.Col;
            lr.material.color = Source.Col;
        }
    }
}