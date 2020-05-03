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
        [SerializeField] float minConstraintsY, maxConstraintsY;
        void Start()
        {
            // custom layout fitter to avoid use of layout groups to cause UI spikes
            float aspect = (float)Screen.width / Screen.height;
            if (aspect < maxAspect)
            {
                float lerp = (aspect-minAspect) / (maxAspect-minAspect);
                float botScale = Mathf.Lerp(minInspectorScale, 1, lerp);
                inspector.transform.localScale = recorder.transform.localScale
                                               = initiator.transform.localScale
                                               = new Vector3(botScale, botScale,1);
                float topScale = Mathf.Lerp(minScoreScale, 1, lerp);
                score.transform.localScale = new Vector3(topScale, topScale, 1);

                float newConstraintsY = Mathf.Lerp(maxConstraintsY, minConstraintsY, lerp);
                constraints.GetComponent<RectTransform>().anchoredPosition = new Vector2(0,newConstraintsY);
            }
        }
    }
}