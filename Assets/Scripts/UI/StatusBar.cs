using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;

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
        }
        Camera eye;
        GameObject target;
        public void FollowSpecies(GameObject target, int size, int greed)
        {
            SetSize(size);
            SetGreed(greed);
            // ShowTraits(true);
            // ShowHealth(false);

            this.target = target;
            this.eye = Camera.main;

            // rt.SetParent(target);
            // rt.localScale = new Vector3(.01f, .01f, .01f);
            // rt.localPosition = new Vector3(0, 0, -.5f);
            // rt.localRotation = Quaternion.identity;
        }
        // public void ShowTraits(bool visible)
        // {
        //     traitsText.enabled = true;
        // }
        // public void ShowHealth(bool visible)
        // {
        //     health.enabled = visible;
        //     healthBG.enabled = visible;
        // }
        public void SetHealth(float normalisedHealth)
        {
            Assert.IsFalse(normalisedHealth < -1 || normalisedHealth > 1, $"given health of {normalisedHealth}");

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
        public void SetSize(int size)
        {
            traitsText.text = $"{size}";
        }
        public void SetGreed(int greed)
        {
            print("TODO: greed status bar");
        }
        void LateUpdate()
        {
            if (target == null) {
                Destroy(gameObject);
                return;
            }
            else if (target.activeSelf)
            {
                if (!health.enabled) {
                    health.enabled = healthBorder.enabled = traitsText.enabled = true;
                }
                Vector3 worldPos = target.transform.TransformPoint(new Vector3(.4f,-.4f,0));
                Vector2 trackedPos = eye.WorldToScreenPoint(worldPos);
                rt.position = trackedPos;
            }
            else if (health.enabled)
            {
                health.enabled = healthBorder.enabled = traitsText.enabled = false;
            }
        }
    }
}
