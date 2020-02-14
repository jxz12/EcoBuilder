using UnityEngine;

namespace EcoBuilder.Archie
{
    public class animal_object : MonoBehaviour
    {
        [SerializeField] SkinnedMeshRenderer smr;
        [SerializeField] Animator anim;
        public SkinnedMeshRenderer Renderer { get { return smr; } }
        public Animator Animator { get { return anim; } }

        public void Awake()
        {
            print("TODO: idle animations");
        }
    }
}