using System;
using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.UI
{
    public class RegistrationForm : MonoBehaviour
    {
        [SerializeField] TMPro.TMP_InputField email, password;
        [SerializeField] TMPro.TMP_Dropdown age, gender, education;
        [SerializeField] Toggle GDPR; // TODO:

        public event Action<string, string, int, int, int> OnSubmitted;
        public event Action OnLoginSkipped;

        public void Begin()
        {
            // show login/register
        }


        public void Login()
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
            OnLoginSkipped.Invoke();
        }

        public void Register()
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
        public void CollectDemographics()
        {

        }

        public void Submit()
        {
            OnSubmitted.Invoke(email.text, password.text, age.value, gender.value, education.value);
        }
    }
}