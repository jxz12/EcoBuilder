using UnityEngine;

namespace EcoBuilder.UI
{
    public class ReportCard : MonoBehaviour
    {
        [SerializeField] TMPro.TextMeshProUGUI current, highest, median;
        [SerializeField] GameObject resultsObj;
        [SerializeField] GameObject starPrefab;

        public void ShowResults(int numStars, int score, int prevScore, int globalMedian)
        {
            gameObject.SetActive(true);
            resultsObj.SetActive(true);
            current.text = score.ToString();
            if (score > prevScore) {
                print("TODO: congratulations!");
            }
            highest.text = prevScore.ToString();
            median.text = globalMedian.ToString();
            print("TODO: check if median is valid");
        }

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