using UnityEngine;
using System.Collections.Generic;

namespace EcoBuilder.NodeLink
{
    public class Link : MonoBehaviour
    {
        [SerializeField] LineRenderer lr;

        [SerializeField] float curveRatio;
        [SerializeField] int curveSegments;
        [SerializeField] float numBalls;

        // these are not get set for performance in the hot path
        public bool Curved = false;
        public float TileSpeed = 0;
        public bool Removable = true;

        public Node Source;
        public Node Target;

        public void Init(Node source, Node target)
        {
            Source = source;
            Target = target;
            name = Source.Idx + " " + Target.Idx;
        }

        cakeslice.Outline outline;
        float defaultLineWidth;
        void Awake()
        {
            outline = gameObject.AddComponent<cakeslice.Outline>();
            // outline.eraseRenderer = true;
            outline.enabled = false;
            lr.material.SetFloat("_RepeatCount", numBalls);
            defaultLineWidth = lr.widthMultiplier;
        }

        Stack<cakeslice.Outline.Colour> outlines = new Stack<cakeslice.Outline.Colour>();
        public void PushOutline(cakeslice.Outline.Colour colour)
        {
            outline.enabled = true;
            outline.colour = colour;
            outlines.Push(colour);
        }
        public void PopOutline()
        {
            outlines.Pop();
            if (outlines.Count > 0) {
                outline.colour = outlines.Peek();
            } else {
                outline.enabled = false;
            }
        }
        bool show = true;
        public void Show(bool showing=true)
        {
            show = showing;
        }

        private float tileOffset = 0;
        private float widthocity, localWidth;
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
            localWidth = Mathf.SmoothDamp(localWidth, show?defaultLineWidth:0, ref widthocity, .2f);
            float lossyWidth = transform.lossyScale.x * localWidth;
            lr.widthMultiplier = lossyWidth;

            float height = (Source.transform.position - Target.transform.position).magnitude;
            lr.material.SetFloat("_Spacing", height/(lossyWidth*numBalls) - 1);

            Color c = Target.Col;
            lr.startColor = c;
            c = Source.Col;
            lr.endColor = c;

            tileOffset -= TileSpeed * Time.deltaTime;
            lr.material.SetFloat("_Offset", tileOffset);
        }
    }
}