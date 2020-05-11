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

        [SerializeField] float maxAspect, minAspect;
        [SerializeField] float minInspectorScale, minScoreScale;
        [SerializeField] float maxConstraintsOffset;

        public float TopScale { get; private set; }
        public float BottomScale { get; private set; }
        public float ConstraintsOffset { get; private set; }
        void Start()
        {
            // custom layout fitter to avoid use of layout groups to cause UI spikes
            float aspect = (float)Screen.width / Screen.height;
            if (aspect < maxAspect)
            {
                float lerp = (aspect-minAspect) / (maxAspect-minAspect);
                BottomScale = Mathf.Lerp(minInspectorScale, 1, lerp);
                inspector.transform.localScale = recorder.transform.localScale
                                               = initiator.transform.localScale
                                               = new Vector3(BottomScale, BottomScale,1);
                TopScale = Mathf.Lerp(minScoreScale, 1, lerp);
                score.transform.localScale = new Vector3(TopScale, TopScale, 1);

                ConstraintsOffset = Mathf.Lerp(maxConstraintsOffset, 0, lerp);
                constraints.GetComponent<RectTransform>().anchoredPosition += new Vector2(0,ConstraintsOffset);
            }
            else
            {
                BottomScale = 1;
                TopScale = 1;
                ConstraintsOffset = 0;
            }
        }
    }
}