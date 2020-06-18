using UnityEngine;

namespace EcoBuilder.UI
{
    public class Star : MonoBehaviour
    {
        [SerializeField] AudioClip get, lose;
        AudioSource speaker;
        void Awake()
        {
            speaker = GetComponent<AudioSource>();
        }
        public void PlayGetTone()
        {
            speaker.clip = get;
            speaker.Play();
        }
        public void PlayLoseTone()
        {
            speaker.clip = lose;
            speaker.Play();
        }
    }
}