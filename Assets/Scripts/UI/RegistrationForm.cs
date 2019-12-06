using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace EcoBuilder.UI
{
    public class RegistrationForm : MonoBehaviour
    {
        [SerializeField] TMPro.TMP_InputField email, password;
        [SerializeField] TMPro.TMP_Dropdown age, gender, education;
        [SerializeField] Toggle GDPR; // TODO:

        public event Action<string, string, int, int, int> OnSubmitted;
        public event Action OnLoginSkipped;

        void Register()
        {
            bool success = GameManager.Instance.TryRegister(email.text, password.text);
            // show demographics here, then submit
            if (success)
            {
                CollectDemographics();
            }
            else
            {
                // show error, try again
                // ALWAYS GIVE OPTION TO NOT REGISTER
            }
        }
        void Login()
        {
            // try to get 
            bool success = GameManager.Instance.TryLogin(email.text, password.text);
            if (success)
            {
                // continue
            }
            else
            {
                // show error message
            }
        }
        public void SkipLogin()
        {
            // show note to say that scores will not be stored
            OnLoginSkipped.Invoke();
        }

        public void CollectDemographics()
        {

        }

        public void Submit()
        {
            OnSubmitted.Invoke(email.text, password.text, age.value, gender.value, education.value);
        }
    }
}