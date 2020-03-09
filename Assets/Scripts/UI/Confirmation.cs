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
        Action YesCallback;
        void Yes()
        {
            YesCallback?.Invoke();
            GetComponent<Canvas>().enabled = false;
        }
        void No()
        {
            GetComponent<Canvas>().enabled = false;
        }


        public void GiveChoice(Action OnYes, string action=null)
        {
            YesCallback = OnYes;
            if (action == null) {
                question.text = "Are you sure?";
            } else {
                question.text = $"Are you sure you want to {action}?";
            }
            GetComponent<Canvas>().enabled = true;
        }
        // public void GiveChoiceWithCompletionCallback(Action<Action<bool, string>> OnYes, string action)
        // {
        //     GiveChoice(()=> OnYes(DoCompletionCallback), action);
        // }
        // private void DoCompletionCallback(bool successful, string error)
        // {
        //     if (successful)
        //     {
        //         question.text = "done!";
        //     }
        //     else
        //     {
        //         question.text = error;
        //     }
        //     // TODO: change buttons and shit
        // }
    }
}