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

        public void Outline(int colourIdx=0)
        {
            outline.enabled = true;
            outline.color = colourIdx;
        }
        public void Unoutline()
        {
            if (DefaultOutline < 0) // if no default outline
            {
                outline.enabled = false;
            }
            else
            {
                outline.color = DefaultOutline;
            }
        }
        bool show = true;
        public void Show(bool showing=true)
        {
            show = showing;
            if (showing)
            {
                // gameObject.SetActive(true);
                // outline.enabled = true;
            }
            else
            {
                // gameObject.SetActive(false);
                // outline.enabled = false;
            }
        }

        private float tileOffset = 0;
        private float widthocity;
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
            float width = show? lineWidth*transform.lossyScale.x : 0; // TODO: make this smooth on lossyScale
            lr.widthMultiplier = Mathf.SmoothDamp(lr.widthMultiplier, width, ref widthocity, .2f);

            float height = (Source.transform.position - Target.transform.position).magnitude;
            lr.material.SetFloat("_Spacing", height/(width*numBalls) - 1);
            lr.material.SetFloat("_RepeatCount", numBalls);

            Color c = Target.Col;
            lr.startColor = c;
            c = Source.Col;
            lr.endColor = c;

            tileOffset -= TileSpeed * Time.deltaTime;
            lr.material.SetFloat("_Offset", tileOffset);
        }
    }
}