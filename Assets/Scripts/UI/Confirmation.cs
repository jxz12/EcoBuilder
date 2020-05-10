using System;
using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.UI
{
    public class Confirmation : MonoBehaviour
    {
        [SerializeField] Button shade, yes, no, okay;
        [SerializeField] TMPro.TextMeshProUGUI question;
        [SerializeField] ContentSizeFitter fitter;
        [SerializeField] VerticalLayoutGroup layout;
        void Start()
        {
            yes.onClick.AddListener(()=>YesCallback?.Invoke());
            shade.onClick.AddListener(()=>NoCallback?.Invoke());
            no.onClick.AddListener(()=>NoCallback?.Invoke());
            okay.onClick.AddListener(()=>OkayCallback?.Invoke());
            canvas = GetComponent<Canvas>();

            // enabling here prevents scene getting dirty, Unity autolayout sucks
            fitter.enabled = true;
            layout.enabled = true;
        }
        private void SetText(string text)
        {
            question.text = text;
            Canvas.ForceUpdateCanvases();
            layout.CalculateLayoutInputVertical();
            layout.SetLayoutVertical();
            fitter.SetLayoutVertical();
        }
        Canvas canvas;
        Action YesCallback;
        Action NoCallback;
        Action OkayCallback;


        public void GiveChoice(Action OnYes, string description=null)
        {
            YesCallback = ()=>{ OnYes?.Invoke(); canvas.enabled = false; };
            NoCallback = ()=> canvas.enabled = false;
            canvas.enabled = true;
            SetQuestion(description);
        }
        private void SetQuestion(string description)
        {
            if (description == null) {
                SetText("Are you sure you want to do that?");
            } else {
                SetText(description);
            }
            yes.gameObject.SetActive(true);
            yes.interactable = true;
            no.gameObject.SetActive(true);
            no.interactable = true;
            okay.gameObject.SetActive(false);
        }

        // callback that attaches a callback
        public void GiveChoiceAndWait(Action OnYes, string description=null, string waitMessage=null)
        {
            YesCallback = ()=>{ OnYes?.Invoke(); Wait(waitMessage); };
            NoCallback = ()=> canvas.enabled = false;
            canvas.enabled = true;
            SetQuestion(description);
            void Wait(string message)
            {
                if (message == null) {
                    SetText("Attempting...");
                } else {
                    SetText(message);
                }
                yes.interactable = false;
                no.interactable = false;
            }
        }
        public void FinishWaiting(Action OnOkay, bool successful, string msg)
        {
            OkayCallback = ()=>{ OnOkay?.Invoke(); canvas.enabled = false; };
            if (successful) {
                SetText("Success!");
            } else {
                SetText($"An error occurred, please try again later or contact ecobuilder@imperial.ac.uk for support. Error code: {msg}");
            }
            yes.gameObject.SetActive(false);
            no.gameObject.SetActive(false);
            okay.gameObject.SetActive(true);
        }
        public void Alert(string message)
        {
            // does nothing but show a temporary message 
            canvas.enabled = true;
            yes.gameObject.SetActive(false);
            no.gameObject.SetActive(false);
            okay.gameObject.SetActive(true);
            OkayCallback = NoCallback = ()=>canvas.enabled = false;
            SetText(message);
        }
    }
}