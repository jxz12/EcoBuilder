using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Collections;

namespace EcoBuilder.UI
{
    public class LoadingBar : MonoBehaviour
    {
        static string[] possibleMessages = new string[]
        {
            "Preparing your ecosystem...",
            "Watering the plants...",
            "Feeding the lions...",
            "Holding a General Election to elect the King of the Jungle...",//, but with Single Transferable Vote instead of First Past the Post because Democracy is important, even in the wild...",
            "Cloning Velociraptors from Jurassic fossils...",
            "Noah's most difficult job was probably not building the ark, but keeping his ecosystem alive...",
            "Forging food chains...",
            "Who let the carnivores out?",
            "Spinning your food web...",
            "Using Fungi to decompose the Ecosystem of the previous player...",
            "Waiting for evolution to occur... eventually..."
        };
        [SerializeField] TMPro.TextMeshProUGUI funnyMessage;
        [SerializeField] Slider bar;

        RectTransform barRT;
        void Awake()
        {
            barRT = bar.GetComponent<RectTransform>();
        }
        public void Show(bool showing)
        {
            GetComponent<Canvas>().enabled = showing;
            if (showing) {
                funnyMessage.text = possibleMessages[Random.Range(0, possibleMessages.Length)];
                StopAllCoroutines();
                StartCoroutine(MoveUp());
            }
        }
        IEnumerator MoveUp(float duration=.5f)
        {
            Vector2 startPos = barRT.anchoredPosition;
            float startTime = Time.time;
            while (Time.time < startTime + duration)
            {
                float t = (Time.time - startTime) / duration;
                float y = Mathf.Lerp(startPos.y-200, startPos.y, t);
                barRT.anchoredPosition = new Vector2(startPos.x, y);
                yield return null;
            }
            barRT.anchoredPosition = startPos;
        }
        public void SetProgress(float progress)
        {
            Assert.IsFalse(progress < 0 || progress > 1, "not normalised");
            bar.normalizedValue = Mathf.Max(progress, .05f); // to stop it squishing at left
        }
    }
}