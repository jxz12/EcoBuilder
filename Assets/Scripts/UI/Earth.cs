using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EcoBuilder.UI
{
    public class Earth : MonoBehaviour, IDragHandler
    {
        Animator anim;
        public void Awake()
        {
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

        // for gamemanager
        public void Grow()
        {
            anim.SetTrigger("Grow");
        }
        public void Shrink()
        {
            anim.SetTrigger("Shrink");
        }

        // for user interaction
        // [SerializeField] float minRotationVelocity, rotationMultiplier;
        float rotationVelocity, rotation;
        void Update()
        {
            rotationVelocity = Mathf.Lerp(rotationVelocity, Mathf.Sign(rotationVelocity)*.02f, .1f);
            rotation += rotationVelocity;
            transform.localRotation = Quaternion.Euler(0,rotation,0);
        }
        public void OnDrag(PointerEventData ped)
        {
            float y = -ped.delta.x * .5f;
            transform.Rotate(new Vector3(0, y, 0));
            rotationVelocity = y;
        }
    }
}