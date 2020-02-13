using UnityEngine;

namespace EcoBuilder.UI
{
    public class Results : MonoBehaviour
    {
        public TMPro.TextMeshProUGUI current, highest, median;
        public void Display(int numStars, int score, int prevScore, int globalMedian)
        {
            gameObject.SetActive(true);
            current.text = score.ToString();
            if (score > prevScore) {
                print("TODO: congratulations!");
            }
            highest.text = prevScore.ToString();
            median.text = globalMedian.ToString();
        }
    }

}