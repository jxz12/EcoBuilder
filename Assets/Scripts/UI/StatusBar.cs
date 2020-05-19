using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace EcoBuilder.UI
{
    public class StatusBar : MonoBehaviour
    {
        [SerializeField] Image health, healthBorder;
        [SerializeField] TMPro.TextMeshProUGUI traitsText;

        static HashSet<StatusBar> allBars = new HashSet<StatusBar>();
        RectTransform rt;
        void Awake()
        {
            rt = GetComponent<RectTransform>();
            health.enabled = healthBorder.enabled = healthShowing;
            allBars.Add(this);
            StartCoroutine(Grow());
        }
        IEnumerator Grow(float duration=1)
        {
            float tStart = Time.time;
            while (Time.time < tStart+duration)
            {
                float size = Tweens.ElasticOut((Time.time-tStart)/duration);
                transform.localScale = new Vector3(size,size,1);
                yield return null;
            }
        }
        GameObject target;
        public void FollowSpecies(GameObject target)
        {
            name = $"{target.name} Status";
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
            // redraw if needed
            foreach (var bar in allBars) {
                bar.SetSize(bar.Size);
            }
        }
        public static void HideGreed(bool hidden=true)
        {
            greedShowing = !hidden;
            // redraw if needed
            foreach (var bar in allBars) {
                bar.SetGreed(bar.Greed);
            }
        }
        bool healthShowing=false;
        public void ShowHealth(bool showing=true)
        {
            health.enabled = healthBorder.enabled = healthShowing = showing;
        }
        float currentHealth=0;
        public void SetHealth(float normalisedHealth)
        {
            Assert.IsFalse(normalisedHealth < -1 || normalisedHealth > 1, $"given health of {normalisedHealth}");

            if (normalisedHealth > currentHealth && normalisedHealth > 0) {
                health.color = new Color(0,1,0);
            }
            if (normalisedHealth < currentHealth) {
                health.color = new Color(.5f,0,0);
            }
            currentHealth = normalisedHealth;
            if (currentHealth > 0)
            {
                health.fillAmount = .333f + .667f*normalisedHealth;
            }
            else
            {
                health.fillAmount = .333f*(1+normalisedHealth);
            }
        }
        StringBuilder sb = new StringBuilder("<color=#ffffff>5 <color=#84b2ff>3");
        public int Size { get; private set; }
        public int Greed { get; private set; }
        public void SetSize(int size)
        {
            Assert.IsTrue(size.ToString().Length == 1);

            char digit = size.ToString()[0];
            sb[15] = sizeShowing? digit : ' ';
            traitsText.text = sb.ToString();

            Size = size;
        }
        public void SetGreed(int greed)
        {
            Assert.IsTrue(greed.ToString().Length == 1);

            char digit = greed.ToString()[0];
            sb[32] = greedShowing? digit : ' ';
            traitsText.text = sb.ToString();

            Greed = greed;
        }
        void LateUpdate()
        {
            if (target == null || eye == null)
            {
                allBars.Remove(this);
                Destroy(gameObject);
                return;
            }
            else if (target.activeSelf)
            {
                if (!traitsText.enabled)
                {
                    health.enabled = healthBorder.enabled = healthShowing;
                    traitsText.enabled = true;
                    StartCoroutine(Grow());
                }
                Vector3 worldPos = target.transform.TransformPoint(new Vector3(.4f,-.4f,0));
                Vector2 canvasPos = eye.WorldToViewportPoint(worldPos);
                rt.anchorMin = rt.anchorMax = canvasPos;

                if (currentHealth > 0) {
                    health.color = Color.Lerp(health.color, new Color(.3f,.8f,.5f), 2*Time.deltaTime);
                } else {
                    health.color = Color.Lerp(health.color, new Color(.9f,0,0), 2*Time.deltaTime);
                }
            }
            else if (traitsText.enabled)
            {
                health.enabled = healthBorder.enabled = traitsText.enabled = false;
            }
        }
    }
}
