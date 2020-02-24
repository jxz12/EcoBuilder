using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace EcoBuilder.UI
{
    public class ReportCard : MonoBehaviour
    {
        [SerializeField] TMPro.TextMeshProUGUI current, highest, median;
        [SerializeField] GameObject starPrefab;
        [SerializeField] Image shade;
        [SerializeField] Animator star1, star2, star3;

        RectTransform rt;
        Canvas canvas;
        void Awake()
        {
            rt = GetComponent<RectTransform>();
            canvas = GetComponent<Canvas>();
        }

        public void ShowResults(int numStars, int score, int prevScore, int globalMedian)
        {
            current.text = score.ToString();
            if (score > prevScore) {
                print("TODO: congratulations!");
            }
            highest.text = prevScore.ToString();
            median.text = globalMedian.ToString();
            print("TODO: check if median is valid");

            StartCoroutine(ShowRoutine(1.5f));
            StartCoroutine(StarRoutine(2, .5f, .5f, numStars));
        }
        IEnumerator ShowRoutine(float duration)
        {
            canvas.enabled = true;
            float startTime = Time.time;
            while (Time.time < startTime+duration)
            {
                float t = (Time.time-startTime)/duration;
                // quadratic ease in-out
                if (t < .5f) {
                    t = 2*t*t;
                } else {
                    t = -1 + (4-2*t)*t;
                }
                float y = Mathf.Lerp(-1000, 0, t);
                float a = Mathf.Lerp(0,.5f, t);

                rt.anchoredPosition = new Vector2(0, y);
                shade.color = new Color(0,0,0,a);
                yield return null;
            }
        }
        IEnumerator StarRoutine(float delay1, float delay2, float delay3, int numStars)
        {
            yield return new WaitForSeconds(delay1);
            star1.SetBool("Filled", true);
            yield return new WaitForSeconds(delay2);
            star2.SetBool("Filled", numStars >= 2);
            yield return new WaitForSeconds(delay3);
            star3.SetBool("Filled", numStars >= 3);
        }

        [SerializeField] RectTransform prevLevelAnchor, nextLevelAnchor;
        public RectTransform PrevLevelAnchor { get { return prevLevelAnchor; } }
        public RectTransform NextLevelAnchor { get { return nextLevelAnchor; } }

        // public Level NextLevelInstantiated { get; private set; }
        // public void UnlockNextLevel() // because of silly animator gameobject active stuff
        // {
        //     Assert.IsFalse(GameManager.Instance.NavParent.transform.childCount > 0, "more than one level on navigation?");

        //     StartCoroutine(TweenToZeroPosFrom(0, GameManager.Instance.NavParent));
        //     print("TODO: make navigation pop to below screen then rise");
        // }



        // public void SetMessage(string message)
        // {
        //     this.message.text = message;
        // }
        // public void Toggle()
        // {
        //     bool showing = GetComponent<Animator>().GetBool("Visible");
        //     if (showing)
        //     {
        //         text.text = "";
        //     }
        //     else
        //     {
        //         text.text = message;
        //     }
        //     GetComponent<Animator>().SetBool("Visible", !showing);
        // }
        // public void Toggle()
        // {
        //     gameObject.SetActive(!gameObject.activeSelf);
        // }

        // public void Show(string report)
        // {
        //     GetComponent<Animator>().SetBool("Visible", true);
        //     text.text = report;
        // }
        // public void Hide()
        // {
        //     GetComponent<Animator>().SetBool("Visible", false);
        //     text.text = "";
        // }
    }
}