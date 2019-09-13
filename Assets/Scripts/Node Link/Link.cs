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

        public void Init(Node source, Node target)
        {
            Source = source;
            Target = target;
            name = Source.Idx + " " + Target.Idx;
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
            lr.material.SetFloat("_Alpha", alpha);
        }

        [SerializeField] float lineWidth;
        [SerializeField] float curveRatio;
        [SerializeField] int curveSegments;
        [SerializeField] float numBalls;

        public bool Curved { get; set; } = false;
        public float TileSpeed { get; set; } = 0;
        public bool Removable { get; set; } = true;

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

            // TODO: include length of line so that balls don't get ellipsed
            // lr.material.SetFloat("_Spacing", (1-NumBalls*lineWidth) / (NumBalls*lineWidth));
            lr.material.SetFloat("_Spacing", (1-numBalls*width) / (numBalls*width));
            lr.material.SetFloat("_RepeatCount", numBalls);

            Color c = Target.Col;
            // if (!Removable)
            //     c.b = 1;
            lr.startColor = c;
            c = Source.Col;
            // if (!Removable)
            //     c.b = 1;
            lr.endColor = c;
        }

        private float tileOffset = 0;
        private void FixedUpdate()
        {
            tileOffset -= TileSpeed;
            lr.material.SetFloat("_Offset", tileOffset);
        }
    }
}