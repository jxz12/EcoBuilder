using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using System.Text;

namespace EcoBuilder.UI
{
    public class StatusBar : MonoBehaviour
    {
        [SerializeField] Image health, healthBorder;
        [SerializeField] TMPro.TextMeshProUGUI traitsText;

        RectTransform rt;
        void Awake()
        {
            rt = GetComponent<RectTransform>();
            health.enabled = healthBorder.enabled = healthShowing;
        }
        GameObject target;
        public void FollowSpecies(GameObject target)
        {
            this.target = target;
        }
        static Camera eye;
        public static void SetCamera(Camera cam)
        {
            StatusBar.eye = cam;
        }
        static bool sizeShowing=true, greedShowing=true;
        public static void HideSize(bool hidden=true)
        {
            sizeShowing = !hidden;
        }
        public static void HideGreed(bool hidden=true)
        {
            greedShowing = !hidden;
        }
        bool healthShowing=false;
        public void ShowHealth(bool visible=true)
        {
            health.enabled = healthBorder.enabled = healthShowing = visible;
        }
        public void SetHealth(float normalisedHealth)
        {
            Assert.IsFalse(normalisedHealth < -1 || normalisedHealth > 1, $"given health of {normalisedHealth}");
            print("TODO: make this flash and shit");

            if (normalisedHealth >= 0)
            {
                health.color = new Color(.2f, 1f, .2f, 1);
                health.fillAmount = normalisedHealth;
            }
            else
            {
                health.color = new Color(.4f, .2f, .2f, .2f);
                health.fillAmount = -normalisedHealth;
            }
        }
        StringBuilder sb = new StringBuilder("<color=#ffffff>5 <color=#ffb284>0");
        public void SetSize(int size)
        {
            Assert.IsFalse(size.ToString().Length != 1);

            char digit = size.ToString()[0];
            sb[15] = sizeShowing? digit : ' ';
            traitsText.text = sb.ToString();
        }
        public void SetGreed(int greed)
        {
            Assert.IsFalse(greed.ToString().Length != 1);

            char digit = greed.ToString()[0];
            sb[32] = greedShowing? digit : ' ';
            traitsText.text = sb.ToString();
        }
        void LateUpdate()
        {
            if (target == null) {
                Destroy(gameObject);
                return;
            }
            else if (target.activeSelf)
            {
                if (!traitsText.enabled) {
                    health.enabled = healthBorder.enabled = healthShowing;
                    traitsText.enabled = true;
                }
                Vector3 worldPos = target.transform.TransformPoint(new Vector3(.4f,-.4f,0));
                Vector2 trackedPos = eye.WorldToScreenPoint(worldPos);
                rt.position = trackedPos;
            }
            else if (traitsText.enabled)
            {
                health.enabled = healthBorder.enabled = traitsText.enabled = false;
            }
        }
    }
}
