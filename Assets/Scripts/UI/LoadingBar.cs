using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;

namespace EcoBuilder.UI
{
    public class LoadingBar : MonoBehaviour
    {
        static string[] possibleMessages = new string[]
        {
            "Preparing your ecosystem...",
            "Watering the plants...",
            "Feeding the lions...",
            // "Holding a General Election to choose the King of the Animal Kingdom, but with Single Transferable Vote instead of First Past the Post because Democracy is important, even in the wild...",
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

        public void Show(bool showing)
        {
            gameObject.SetActive(showing);
            if (showing) {
                funnyMessage.text = possibleMessages[Random.Range(0, possibleMessages.Length)];
            }
        }
        public void SetLoadingProgress(float progress)
        {
            Assert.IsFalse(progress < 0 || progress > 1, "not normalised");
            bar.normalizedValue = Mathf.Max(progress, .05f); // to stop it squishing at left
        }
    }
}