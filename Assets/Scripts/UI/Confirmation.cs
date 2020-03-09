using System;
using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.UI
{
    public class Confirmation : MonoBehaviour
    {
        [SerializeField] Button yes, no;
        [SerializeField] TMPro.TextMeshProUGUI question;
        [SerializeField] ContentSizeFitter fitter;
        [SerializeField] VerticalLayoutGroup layout;
        void Start()
        {
            yes.onClick.AddListener(Yes);
            no.onClick.AddListener(No);

            // enabling here prevents scene getting dirty, Unity autolayout sucks
            fitter.enabled = true;
            layout.enabled = true;
        }
        Action OnYes, OnNo;
        void Yes()
        {
            OnYes?.Invoke();
            GetComponent<Canvas>().enabled = false;
        }
        void No()
        {
            OnNo?.Invoke();
            GetComponent<Canvas>().enabled = false;
        }


        public void GiveChoice(Action OnYes, string action=null)
        {
            this.OnYes = OnYes;
            OnNo = null;
            if (action == null) {
                question.text = "Are you sure?";
            } else {
                question.text = $"Are you sure you want to {action}?";
            }
            GetComponent<Canvas>().enabled = true;
        }
        public void GiveChoiceWithCompletionCallback(Action<Action<bool, string>> OnYes, string action)
        {
            this.OnYes = ()=>DoCompletionCallback(OnYes);
            OnSuccess += DoCompletionCallback;
            confirmation.GiveChoice(()=> DeleteAccountRemote((b,s)=> print("TODO: show error if could not delete")));

        }
        private void DoCompletionCallback(bool successful, string error)
        {
            if (b) question.text = "done!"; else question.text = s;
            // TODO: change buttons and shit
        }
        // TODO: add tweening
    }
}