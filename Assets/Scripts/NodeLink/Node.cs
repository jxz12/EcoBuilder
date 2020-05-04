using UnityEngine;
using UnityEngine.Assertions;
using System.Collections;
using System.Collections.Generic;

namespace EcoBuilder.NodeLink
{
    public class Node : MonoBehaviour
    {
        public int Idx;
        public Color Col {
            get { return shapeRenderer!=null? shapeRenderer.material.color : Color.black; }
        }
        
        // these are not get/set for performance in the hot path
        public enum PositionState { Stress, Focus }
        public PositionState State = PositionState.Stress;
        public Vector2 StressPos;
        public Vector3 FocusPos;

        [SerializeField] float defaultSize = .5f;
        public bool CanBeSource = true;
        public bool CanBeTarget = true;
        public bool CanBeFocused = true;

        GameObject shapeObject;
        Renderer shapeRenderer;
        cakeslice.Outline outline;

        public void Init(int idx)
        {
            Idx = idx;
            name = idx.ToString();
            StressPos = UnityEngine.Random.insideUnitCircle; // prevent divide-by-zero in local majorization
        }

        public void SetShape(GameObject shape)
        {
            Assert.IsNotNull(shape, "shape is null");
            Assert.IsNotNull(shape.GetComponentInChildren<Renderer>(), "shape has no renderer");
            shapeObject = shape;
            shapeRenderer = shape.GetComponentInChildren<Renderer>();
            if (shapeRenderer.GetComponent<cakeslice.Outline>() == null) {
                outline = shapeRenderer.gameObject.AddComponent<cakeslice.Outline>();
            }
            // drop it in at the point at shapeObject's position
            transform.position = shapeObject.transform.position;

            shapeObject.transform.SetParent(transform, false);
            shapeObject.transform.localPosition = Vector3.zero;
            shapeObject.transform.localRotation = Quaternion.identity;
        }
        public void HideShape(bool hidden)
        {
            if (shapeObject != null) {
                shapeObject.SetActive(!hidden);
            }
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
        public void Highlight()
        {
            // TODO: make the node bigger maybe
            // defaultSize *= 1.2f;
            // transform.localScale = Vector3.one * defaultSize;
        }
        public void Unhighlight()
        {
            // TODO: make the node smaller maybe
            // defaultSize /= 1.2f;
            // transform.localScale = Vector3.one * defaultSize;
        }

        IEnumerator resizeRoutine;
        public void SetNewParentKeepPos(Transform newParent)
        {
            transform.SetParent(newParent, true);
            transform.localRotation = Quaternion.identity;

            // if (resizeRoutine != null)
            // {
            //     StopCoroutine(resizeRoutine);
            //     resizeRoutine = null;
            // }
            // StartCoroutine(resizeRoutine = Resize(.5f));
            // IEnumerator Resize(float duration)
            // {
            //     float startSize = transform.localScale.x;
            //     float startTime = Time.time;
            //     while (Time.time < startTime + duration)
            //     {
            //         float t = (Time.time-startTime) / duration;
            //         t = t<.5f? 2*t*t : (4-2*t)*t-1;

            //         float size = Mathf.Lerp(startSize, defaultSize, t);
            //         transform.localScale = new Vector3(size, size, size);
            //         yield return null;
            //     }
            //     transform.localScale = new Vector3(defaultSize, defaultSize, defaultSize);
            //     resizeRoutine = null;
            // }
        }

        IEnumerator flashRoutine;
        public void Flash(bool isFlashing)
        {
            if (shapeRenderer == null) {
                return;
            }
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
                shapeRenderer.enabled = true;
            }
            if (isFlashing) {
                StartCoroutine(flashRoutine = Flash(1f));
            }

            IEnumerator Flash(float period)
            {
                bool enabled = true;
                float start = Time.time;
                while (true)
                {
                    if ((Time.time-start) % period > period/2)
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
        }
        // for use with smoothdamp
        Vector3 velocity;
        float sizocity;
        public void TweenPos(float smoothTime)
        {
            if (State == PositionState.Stress) {
                transform.localPosition = Vector3.SmoothDamp(transform.localPosition, StressPos, ref velocity, smoothTime);
            } else { // superfocus
                transform.localPosition = Vector3.SmoothDamp(transform.localPosition, FocusPos, ref velocity, smoothTime);
            }
            float size = Mathf.SmoothDamp(transform.localScale.x, defaultSize, ref sizocity, smoothTime);
            transform.localScale = new Vector3(size, size, size);
        }
    }
}