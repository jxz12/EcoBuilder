using UnityEngine;
using System.Collections;

namespace EcoBuilder.NodeLink
{
    // [RequireComponent(typeof(Animator))]
    public class Node : MonoBehaviour
    {
        public int Idx { get; private set; }
        public Color Col {
            get { return shapeRenderer!=null? shapeRenderer.material.color : Color.black; }
        }
        public enum FocusState { Normal, Focus, Hidden }
        public FocusState focusState { get; set; } = FocusState.Normal;
        public Vector3 StressPos { get; set; }
        public Vector3 FocusPos { get; set; }
        public bool CanBeSource { get; set; } = true;
        public bool CanBeTarget { get; set; } = true;
        public bool Removable { get; set; } = true;
        public int DefaultOutline { get; set; } = -1;

        GameObject shape;
        MeshRenderer shapeRenderer;
        HealthBar healthBar;
        cakeslice.Outline outlineHealth, outlineShape;

        public void Init(int idx)
        {
            Idx = idx;
            name = idx.ToString();
            outlineHealth = gameObject.AddComponent<cakeslice.Outline>();
        }

        public void Shape(GameObject shapeObject)
        {
            // drop it in at the point at shapeObject's position
            transform.position = shapeObject.transform.position;
            shape = shapeObject;
            shapeRenderer = shape.GetComponent<MeshRenderer>();
            outlineShape = shape.AddComponent<cakeslice.Outline>();

            shape.transform.SetParent(transform, false);
            shape.transform.localPosition = Vector3.zero;
            shape.transform.localRotation = Quaternion.identity;
        }
        public void Outline(int colourIdx)
        {
            // outline.eraseRenderer = false;
            outlineShape.enabled = true;
            outlineShape.color = colourIdx;
            outlineHealth.enabled = true;
            outlineHealth.color = colourIdx;
        }
        public void Unoutline()
        {
            if (DefaultOutline < 0)
            {
                outlineShape.enabled = false;
                outlineHealth.enabled = false;
            }
            else
            {
                outlineShape.color = DefaultOutline;
                outlineHealth.color = DefaultOutline;
            }
        }
        bool flashing = false;
        public void Flash(bool isFlashing)
        {
            flashing = isFlashing;
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
                        shapeRenderer.enabled = false;
                        enabled = false;
                    }
                }
                else
                {
                    if (!enabled)
                    {
                        shapeRenderer.enabled = true;
                        enabled = true;
                    }
                }
                yield return null;
            }
            shapeRenderer.enabled = true;
        }
        Vector3 velocity; // for use with smoothdamp
        public void TweenPos(float smoothTime)
        {
            if (focusState == FocusState.Normal)
            {
                transform.localPosition =
                    Vector3.SmoothDamp(transform.localPosition, StressPos,
                                        ref velocity, smoothTime);
            }
            else if (focusState == FocusState.Focus)
            {
                transform.localPosition =
                    Vector3.SmoothDamp(transform.localPosition, FocusPos,
                                        ref velocity, smoothTime);
            }
            else if (focusState == FocusState.Hidden)
            {
                transform.localPosition =
                    Vector3.SmoothDamp(transform.localPosition, StressPos + new Vector3(0,0,2),
                                        ref velocity, smoothTime);
            }
        }
        // public void TweenSize(float sizeTween)
        // {
        //     if (focusState != FocusState.Hidden)
        //     {
        //         transform.localScale =
        //             Vector3.Lerp(transform.localScale, Size*Vector3.one, sizeTween);
        //     }
        //     else
        //     {
        //         transform.localScale =
        //             Vector3.Lerp(transform.localScale, Vector3.zero, sizeTween);
        //     }
        // }
    }
}