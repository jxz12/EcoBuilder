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
        public enum PositionState { Stress, Focus }
        public PositionState State { get; set; } = PositionState.Stress;
        public Vector3 StressPos { get; set; }
        public Vector3 FocusPos { get; set; }

        public bool CanBeSource { get; set; } = true;
        public bool CanBeTarget { get; set; } = true;
        public bool Removable { get; set; } = true;

        GameObject shape;
        MeshRenderer shapeRenderer;
        HealthBar healthBar;

        public int DefaultOutline { get; set; } = -1;
        cakeslice.Outline outlineHealth, outlineShape; // TODO: may be slow to have 2 of these

        void Awake()
        {
            healthBar = GetComponent<HealthBar>();
            outlineHealth = gameObject.AddComponent<cakeslice.Outline>();
        }
        public void Init(int idx)
        {
            Idx = idx;
            name = idx.ToString();
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
        public void SetHealth(float health)
        {
            healthBar.TargetHealth = health;
        }
        public void SetNewParent(Transform newParent)
        {
            transform.SetParent(newParent, true);
            transform.localRotation = Quaternion.identity;
        }

        IEnumerator flashRoutine;
        public void Flash(bool isFlashing)
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
                shapeRenderer.enabled = true;
            }

            if (isFlashing)
                StartCoroutine(flashRoutine = Flash(1f));
        }
        IEnumerator Flash(float period)
        {
            bool enabled = true;
            float start = Time.time;
            while (true)
            {
                if ((Time.time-start) % period < period/2)
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
        }
        float defaultSize = .5f; // TODO:
        IEnumerator bounceRoutine;
        public void Bounce()
        {
            if (bounceRoutine != null)
            {
                StopCoroutine(bounceRoutine);
                // transform.localScale = Vector3.one;
            }

            StartCoroutine(bounceRoutine = Bounce(.5f, .1f));
        }
        IEnumerator Bounce(float length, float magnitude)
        {
            float startTime = Time.time;
            while (Time.time < startTime+length)
            {
                float t = (Time.time-startTime) / length;
                // float t1 = t-1;
                // transform.localScale = (.5f + magnitude*(4*Mathf.Sqrt(t) * -(t1*t1*t1))) * Vector3.one;
                transform.localScale = (defaultSize + magnitude*(4*Mathf.Sqrt(t) * -Mathf.Pow(t-1,3))) * Vector3.one;
                yield return null;
            }
            bounceRoutine = null;
        }
        Vector3 velocity; // for use with smoothdamp
        Vector3 sizocity; // for use with smoothdamp
        public void TweenPos(float smoothTime)
        {
            if (State == PositionState.Stress)
            {
                transform.localPosition =
                    Vector3.SmoothDamp(transform.localPosition, StressPos,
                                        ref velocity, smoothTime);
            }
            else //(State == FocusState.Focus)
            {
                transform.localPosition =
                    Vector3.SmoothDamp(transform.localPosition, FocusPos,
                                        ref velocity, smoothTime);
            }

            if (bounceRoutine == null)
            {
                transform.localScale =
                    Vector3.SmoothDamp(transform.localScale, defaultSize*Vector3.one,
                                       ref sizocity, smoothTime);
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