﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EcoBuilder.UI
{
    public class Planet : MonoBehaviour, IDragHandler
    {
        Animator anim;
        void Awake()
        {
            print("TODO: make this persistent across the whole game");
            anim = GetComponent<Animator>();
        }

        // for animation events
        public void Disable()
        {
            this.enabled = false;
        }
        public void Enable()
        {
            this.enabled = true;
        }

        public void ListenToScenes(string s) // also ugly
        {
            if (s == "Menu")
            {
                anim.SetTrigger("Grow");
            }
            else if (s == "Play" && !anim.GetCurrentAnimatorStateInfo(0).IsName("Hidden")) // so ugly
            {
                anim.SetTrigger("Shrink");
            }
        }

        // for user interaction
        // [SerializeField] float minRotationVelocity, rotationMultiplier;
        float rotation, rotationTarget, rotationVelocity;
        void Update()
        {
            rotationTarget += (rotationVelocity + Mathf.Sign(rotationVelocity)*.2f) * Time.deltaTime;
            rotation = Mathf.SmoothDamp(rotation, rotationTarget, ref rotationVelocity, .02f);

            transform.localRotation = Quaternion.Euler(0,rotation,0);
        }
        public void OnDrag(PointerEventData ped)
        {
            rotationTarget -= ped.delta.x * .1f;
        }
    }
}