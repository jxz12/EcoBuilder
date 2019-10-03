using UnityEngine;
using System.Collections;

namespace EcoBuilder.NodeLink
{
    // [RequireComponent(typeof(Animator))]
    public class Node : MonoBehaviour
    {
        public int Idx { get; private set; }
        public Color Col {
            get { return shape!=null? shape.GetComponent<MeshRenderer>().material.color : Color.black; }
        }
        public enum FocusState { Normal, Focus, Hidden }
        public FocusState focusState = FocusState.Normal;
        public Vector3 StressPos { get; set; }
        public Vector3 FocusPos { get; set; }
        public float Size { get; set; }
        public bool CanBeSource { get; set; } = true;
        public bool CanBeTarget { get; set; } = true;
        public bool Removable { get; set; } = true;

        GameObject shape;
        cakeslice.Outline outline;

        public void Init(int idx, float size)
        {
            Idx = idx;
            name = idx.ToString();
            StressPos = Random.insideUnitSphere;
            Size = size;
        }

        public void Shape(GameObject shapeObject)
        {
            // drop it in at the point at shapeObject's position, but at z=-1
            // TODO: this may be buggy
            // StressPos = transform.parent.InverseTransformPoint(new Vector3(shapeObject.transform.position.x, shapeObject.transform.position.y, -1));
            StressPos = new Vector3(1,0,-1); // prevent divide by zero
            StressPos += UnityEngine.Random.insideUnitSphere; // prevent divide by zero
            transform.position = shapeObject.transform.position;

            shape = shapeObject;
            outline = shape.AddComponent<cakeslice.Outline>();

            shape.transform.SetParent(transform, false);
            shape.transform.localPosition = Vector3.zero;
            shape.transform.localRotation = Quaternion.identity;
            shape.transform.localScale = Vector3.one;
        }
        public void Outline(int colourIdx)
        {
            outline.eraseRenderer = false;
            outline.color = colourIdx;
        }
        public void Unoutline()
        {
            if (Removable)
                outline.eraseRenderer = true;
            else
                outline.color = 0;
        }
        bool flashing = false;
        public void Flash(bool isFlashing)
        {
            flashing = isFlashing;
            // GetComponent<Animator>().SetBool("Flashing", isFlashing);
            StartCoroutine(Flash(1f));
        }
        IEnumerator Flash(float time)
        {
            bool enabled = true;
            float start = Time.time;
            while (flashing)
            {
                if ((Time.time-start) % time < time/2)
                {
                    if (enabled)
                    {
                        shape.GetComponent<MeshRenderer>().enabled = false;
                        enabled = false;
                    }
                }
                else
                {
                    if (!enabled)
                    {
                        shape.GetComponent<MeshRenderer>().enabled = true;
                        enabled = true;
                    }
                }
                yield return null;
            }
            shape.GetComponent<MeshRenderer>().enabled = true;
        }
        Vector3 velocity; // for use with smoothdamp
        [SerializeField] float layoutSmoothTime=.5f, sizeTween=.05f;
        void FixedUpdate()
        {
            if (focusState == FocusState.Normal)
            {
                transform.localScale =
                    Vector3.Lerp(transform.localScale, Size*Vector3.one, sizeTween);
                transform.localPosition =
                    Vector3.SmoothDamp(transform.localPosition, StressPos,
                                        ref velocity, layoutSmoothTime);
            }
            else if (focusState == FocusState.Focus)
            {
                transform.localScale =
                    Vector3.Lerp(transform.localScale, Size*Vector3.one, sizeTween);
                transform.localPosition =
                    Vector3.SmoothDamp(transform.localPosition, FocusPos,
                                        ref velocity, layoutSmoothTime);
            }
            else if (focusState == FocusState.Hidden)
            {
                transform.localScale =
                    Vector3.Lerp(transform.localScale, Vector3.zero, sizeTween);
                transform.localPosition =
                    Vector3.SmoothDamp(transform.localPosition, StressPos + new Vector3(0,0,2),
                                        ref velocity, layoutSmoothTime);
            }
        }
    }
}