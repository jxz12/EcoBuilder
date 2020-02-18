using UnityEngine;

namespace EcoBuilder.Archie
{
    public class animal_object : MonoBehaviour
    {
        [SerializeField] SkinnedMeshRenderer smr;
        [SerializeField] Animator anim;
        public SkinnedMeshRenderer Renderer { get { return smr; } }
        // public Animator Animator { get { return anim; } }

        public Texture2D Eyes { get; set; }
        public void Die()
        {
            anim.SetTrigger("Die");
        }
        public void Live()
        {
            anim.SetTrigger("Live");
        }
        public void IdleAnimation()
        {
            anim.SetInteger("Which Cute", Random.Range(0,2));
            anim.SetTrigger("Be Cute");
        }
    }
}