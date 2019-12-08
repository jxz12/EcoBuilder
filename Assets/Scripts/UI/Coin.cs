using System;
using UnityEngine;

namespace EcoBuilder.UI
{
    public class Coin : MonoBehaviour
    {
        public event Action<bool> OnLanded;

        [SerializeField] Animator anim;
        bool heads = false;
        public void Flip()
        {
            heads = UnityEngine.Random.Range(0, 2) == 0;
            print(heads);
            // TODO: use time.ticks instead?
            anim.SetBool("heads", heads);
            anim.SetTrigger("Flip");
        }
        public void Land()
        {
            OnLanded.Invoke(heads);
        }
        public void Exit()
        {
            anim.SetTrigger("Exit");
        }
        void Update()
        {
            if (Input.anyKeyDown)
            {
                Flip();
            }
        }
    }
}