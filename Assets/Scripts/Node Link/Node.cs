using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EcoBuilder.NodeLink
{
    public class Node : MonoBehaviour
    {
        public int Idx { get; private set; }
        public Color Col {
            get { return shapeRenderer!=null? shapeRenderer.material.color : Color.black; }
        }
        public enum PositionState { Stress, Focus }
        public PositionState State { get; set; } = PositionState.Stress;
        public Vector2 StressPos { get; set; }
        public Vector3 FocusPos { get; set; }

        [SerializeField] float defaultSize = .5f;
        public bool CanBeSource { get; set; } = true;
        public bool CanBeTarget { get; set; } = true;
        public bool Removable { get; set; } = true;
        public bool Disconnected {get; set; } = true;

        GameObject shape;
        public GameObject Shape { get { return shape; } }
        MeshRenderer shapeRenderer;
        cakeslice.Outline outline;

        public void Init(int idx)
        {
            Idx = idx;
            name = idx.ToString();
        }

        public void SetShape(GameObject shapeObject)
        {
            // drop it in at the point at shapeObject's position
            // transform.position = shapeObject.transform.position;
            shape = shapeObject;
            shapeRenderer = shape.GetComponent<MeshRenderer>();
            outline = shape.AddComponent<cakeslice.Outline>();

            shape.transform.SetParent(transform, false);
            shape.transform.localPosition = Vector3.zero;
            shape.transform.localRotation = Quaternion.identity;
        }
        public void Hide(bool hidden)
        {
            shape.SetActive(!hidden);
            GetComponent<Collider>().enabled = !hidden;
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
            }
            else {
                outline.enabled = false;
            }
        }
        public void SetNewParentKeepPos(Transform newParent)
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
        public void LieDown()
        {
            transform.localRotation = Quaternion.Euler(0, 0, -90);
        }
        IEnumerator bounceRoutine;
        public void Bounce()
        {
            transform.localRotation = Quaternion.identity;
            if (bounceRoutine != null)
            {
                StopCoroutine(bounceRoutine);
            }
            StartCoroutine(bounceRoutine = Bounce(.6f, .1f));
        }
        IEnumerator Bounce(float length, float magnitude)
        {
            float startTime = Time.time;
            while (Time.time < startTime+length)
            {
                float t = (Time.time-startTime) / length;
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
                if (!Disconnected) {
                    transform.localPosition = Vector3.SmoothDamp(transform.localPosition, StressPos, ref velocity, smoothTime);
                } else {
                    transform.localPosition = Vector3.SmoothDamp(transform.localPosition, (Vector3)StressPos+.5f*Vector3.back, ref velocity, smoothTime);
                }
            } else { //(State == FocusState.Focus)
                transform.localPosition = Vector3.SmoothDamp(transform.localPosition, FocusPos, ref velocity, smoothTime);
            }
            if (bounceRoutine == null)
            {
                transform.localScale = Vector3.SmoothDamp(transform.localScale, defaultSize*Vector3.one, ref sizocity, smoothTime);
            }
        }
    }
}