using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Text.RegularExpressions;

namespace EcoBuilder.UI
{
    public class RegistrationForm : MonoBehaviour
    {
        [SerializeField] TMPro.TMP_InputField email, password;
        [SerializeField] TMPro.TMP_Dropdown age, gender, education;
        [SerializeField] Toggle GDPR;
        [SerializeField] Button loginSubmit;

        void Start()
        {
        }

        static Regex re = new Regex(@"(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|""(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*"")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])");
        public void CheckEmail()
        {
            loginSubmit.interactable = re.IsMatch(email.text);
        }

        public event Action OnFinished;
        public UnityEvent OnLoggedIn;
        void Register()
        {
            bool success = GameManager.Instance.TryRegister(email.text, password.text);
            // show demographics here, then submit
            if (success)
            {
                OnLoggedIn.Invoke();
            }
            else
            {
                // TODO: show error message
            }
        }
        void Login()
        {
            // try to get 
            bool success = GameManager.Instance.TryLogin(email.text, password.text);
            if (success)
            {
                // continue
                OnLoggedIn.Invoke();
            }
            else
            {
                // TODO: show error message
            }
        }
        public void SkipLogin()
        {
            GameManager.Instance.Logout();
        }
        public void TakeDetails()
        {
            GameManager.Instance.SetDemographics(age.value, gender.value, education.value);
        }
        public void Finish()
        {
            OnFinished.Invoke();
        }

    }
}