using UnityEngine;

namespace EcoBuilder.NodeLink
{
    public class Link : MonoBehaviour
    {
        [SerializeField] LineRenderer lr;

        [SerializeField] float lineWidth;
        [SerializeField] float curveRatio;
        [SerializeField] int curveSegments;
        [SerializeField] float numBalls;

        public bool Curved { get; set; } = false;
        public float TileSpeed { get; set; } = 0;
        public bool Removable { get; set; } = true;
        public int DefaultOutline { get; set; } = -1;

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

        cakeslice.Outline outline;
        void Awake()
        {
            outline = gameObject.AddComponent<cakeslice.Outline>();
            // outline.eraseRenderer = true;
            outline.enabled = false;
        }

        // bool outlined = false;
        public void Outline(int colourIdx=0)
        {
            // outline.eraseRenderer = false;
            // outline.enabled = true;
            // outlined = true;

            // if (Removable)
            //     outline.color = colourIdx;
            // else
            //     outline.color = 0;

            outline.enabled = true;
            outline.color = colourIdx;
        }
        public void Unoutline()
        {
            // outlined = false;
            // if (Removable)
            //     // outline.eraseRenderer = true;
            //     outline.enabled = false;

            if (DefaultOutline < 0)
                // outline.eraseRenderer = true;
                outline.enabled = false;
            else
                outline.color = DefaultOutline;
        }
        // float targetAlpha = 1;
        public void Show(bool showing = true)
        {
        //     if (showing)
        //     {
        //         targetAlpha = 1;
        //         if (outlined)
        //             outline.enabled = true;
        //     }
        //     else
        //     {
        //         targetAlpha = 0;
        //         outline.enabled = false;
        //     }
        }

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

            float height = (Source.transform.position - Target.transform.position).magnitude;
            lr.material.SetFloat("_Spacing", height/(width*numBalls) - 1);
            lr.material.SetFloat("_RepeatCount", numBalls);

            Color c = Target.Col;
            lr.startColor = c;
            c = Source.Col;
            lr.endColor = c;
        }

        private float tileOffset = 0;
        private void FixedUpdate()
        {
            tileOffset -= TileSpeed;
            lr.material.SetFloat("_Offset", tileOffset);
            // lr.material.SetFloat("_Alpha", Mathf.Lerp(lr.material.GetFloat("_Alpha"), targetAlpha, .1f));
        }
    }
}