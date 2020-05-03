using UnityEngine;

namespace EcoBuilder.UI
{
    public class HUD : MonoBehaviour
    {
        // TODO: refactor PlayManager to only need a reference to this
        [SerializeField] Score score;
        [SerializeField] Constraints constraints;

        [SerializeField] Inspector inspector;
        [SerializeField] Recorder recorder;
        [SerializeField] Initiator initiator;

        void Start()
        {
            // TODO: scale things if screen is too thin
        }
    }
}