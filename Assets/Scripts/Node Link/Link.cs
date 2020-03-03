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

        float defaultLineWidth;
        void Awake()
        {
            lr.material.SetFloat("_RepeatCount", numBalls);
            defaultLineWidth = lr.widthMultiplier;
        }

        Stack<Color> outlines = new Stack<Color>();
        public void PushOutlineColour(Color colour)
        {
            outlines.Push(colour);
            if (outlines.Count == 1)
            {

            }
        }
        public void PopOutlineColour()
        {
            outlines.Pop();
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

            Color c = Target.Colour;
            lr.startColor = c;
            c = Source.Colour;
            lr.endColor = c;

            tileOffset -= TileSpeed * Time.deltaTime;
            lr.material.SetFloat("_Offset", tileOffset);
        }
    }
}