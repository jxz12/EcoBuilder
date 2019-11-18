using UnityEngine;

namespace EcoBuilder.UI
{
    public class ReportCard : MonoBehaviour
    {
        [SerializeField] TMPro.TextMeshProUGUI message;
        
        public void SetMessage(string message)
        {
            this.message.text = message;
        }
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
        public void Toggle()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }

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