using UnityEngine;

namespace EcoBuilder.NodeLink
{
    public class Link : MonoBehaviour
    {
        [SerializeField] LineRenderer lr;

        private Node source;
        public Node Source {
            get { return source; }
            set { source = value; lr.endColor = value.Col; }
        }
        private Node target;
        public Node Target {
            get { return target; }
            set { target = value; lr.startColor = value.Col; }
        }

        public float Width {
            get { return lr.widthMultiplier; }
            set { lr.widthMultiplier = value; }
        }
        public float TileSpeed { get; set; }// = .01f;
        public bool Removable { get; set; } = true;

        public void Init(Node source, Node target, bool curved)
        {
            Source = source;
            Target = target;
            name = Source.Idx + " " + Target.Idx;
            Curved = curved;
        }

        public void Outline(int colourIdx=0)
        {
            var outline = gameObject.AddComponent<cakeslice.Outline>();
            outline.color = colourIdx;
        }
        public void Unoutline()
        {
            if (GetComponent<cakeslice.Outline>() != null)
                Destroy(GetComponent<cakeslice.Outline>());
        }

        [SerializeField] float curveRatio = .2f;
        [SerializeField] int curveSegments = 5;
        bool Curved { get; set; } = true;

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
        }
        private void FixedUpdate()
        {
            lr.material.mainTextureOffset -= new Vector2(TileSpeed, 0);
        }
    }
}