﻿using UnityEngine;
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

        // GameObject shape;
        Renderer shapeRenderer;
        cakeslice.Outline outline;

        public void Init(int idx)
        {
            Idx = idx;
            name = idx.ToString();
            StressPos = UnityEngine.Random.insideUnitCircle; // prevent divide-by-zero in local majorization
        }

        public void SetShape(GameObject shapeObject)
        {
            GameObject shape = shapeObject;
            shapeRenderer = shape.GetComponentInChildren<Renderer>();
            outline = shapeRenderer.gameObject.AddComponent<cakeslice.Outline>();

            // drop it in at the point at shapeObject's position
            transform.position = shapeObject.transform.position;

            shape.transform.SetParent(transform, false);
            shape.transform.localPosition = Vector3.zero;
            shape.transform.localRotation = Quaternion.identity;
        }
        public void Hide(bool hidden)
        {
            if (shapeRenderer != null) {
                shapeRenderer.enabled = false;
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
        public void SetNewParentKeepPos(Transform newParent)
        {
            transform.SetParent(newParent, true);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * defaultSize;
        }

        public void Highlight()
        {
            // defaultSize *= 1.2f;
            // transform.localScale = Vector3.one * defaultSize;
        }
        public void Unhighlight()
        {
            // defaultSize /= 1.2f;
            // transform.localScale = Vector3.one * defaultSize;
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


        Vector3 velocity; // for use with smoothdamp
        public void TweenPos(float smoothTime)
        {
            if (State == PositionState.Stress)
            {
                transform.localPosition = Vector3.SmoothDamp(transform.localPosition, StressPos, ref velocity, smoothTime);
            } else { // superfocus
                transform.localPosition = Vector3.SmoothDamp(transform.localPosition, FocusPos, ref velocity, smoothTime);
            }
        }
    }
}