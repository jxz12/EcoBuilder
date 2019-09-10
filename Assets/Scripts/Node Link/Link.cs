using UnityEngine;

namespace EcoBuilder.NodeLink
{
    public class Link : MonoBehaviour
    {
        [SerializeField] LineRenderer lr;

        private Node source;
        public Node Source {
            get { return source; }
            set {
                source = value;
            }
        }
        private Node target;
        public Node Target {
            get { return target; }
            set {
                target = value;
            }
        }

        // public float Width {
        //     get { return lr.widthMultiplier; }
        //     set { lr.widthMultiplier = value; }
        // }
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
            var outline = GetComponent<cakeslice.Outline>();
            if (outline == null)
            {
                outline = gameObject.AddComponent<cakeslice.Outline>();
            }
            outline.color = colourIdx;
        }
        public void Unoutline()
        {
            if (GetComponent<cakeslice.Outline>() != null)
                Destroy(GetComponent<cakeslice.Outline>());
        }
        public void SetTransparency(float alpha)
        {
            Color c = new Color(1,1,1, alpha);
            GetComponent<LineRenderer>().material.color = c;
        }

        [SerializeField] float lineWidth = .2f;
        [SerializeField] float curveRatio = .5f;
        [SerializeField] int curveSegments = 5;
        bool Curved { get; set; } = false;

        private void LateUpdate()
        {
            if (Curved)
            {
                lr.positionCount = curveSegments + 1;

                var from = Source.transform.position;
                var to = Target.transform.position;

                var mid = (from+to) / 2;
                mid += Vector3.Cross(to-from, Vector3.back) * curveRatio;

                lr.SetPosition(0, from);
                for (int i=1; i<curveSegments; i++)
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
            float width = lineWidth * transform.lossyScale.x;
            lr.widthMultiplier = width;
            // lr.material.mainTextureScale = new Vector2(1/lineWidth, 1);

            lr.material.SetFloat("_Spacing", (1-numBalls*lineWidth) / (numBalls*lineWidth));
            lr.material.SetFloat("_RepeatCount", numBalls);

            Color c = Target.Col;
            if (!Removable)
                c.b = 1;
            lr.startColor = c;
            c = Source.Col;
            if (!Removable)
                c.b = 1;
            lr.endColor = c;
        }

        [SerializeField] float numBalls;
        private float tileOffset = 0;
        private void FixedUpdate()
        {
            // lr.material.mainTextureOffset -= new Vector2(TileSpeed, 0);
            tileOffset += TileSpeed;
            lr.material.SetFloat("_Offset", tileOffset);

        }
    }
}