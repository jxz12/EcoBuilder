using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace EcoBuilder.Nichess
{
    public class SpawnPlatform : MonoBehaviour
    {
        [SerializeField] float alpha=.5f;
        public bool Active { get; private set; } = false;
        public UnityEvent DespawnedEvent;

        void Awake()
        {
            var mr = GetComponent<MeshRenderer>();
            Color c = mr.material.color;
            c.a = alpha;
            mr.material.color = c;
        }
        public void Spawn(Piece toSpawn)
        {
            gameObject.SetActive(true);
            // make things shiny
            toSpawn.transform.parent = transform;
            toSpawn.transform.localScale = Vector3.one;
            toSpawn.transform.localPosition = Vector3.zero;
            toSpawn.transform.localRotation = Quaternion.identity;
            // make it glow and stuff

            Active = true;
        }
        public void Despawn()
        {
            if (transform.childCount <= 1) // egg
            {
                gameObject.SetActive(false);
                Active = false;
                DespawnedEvent.Invoke();
            }
        }
    }
}