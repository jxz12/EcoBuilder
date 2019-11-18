using System;
using UnityEngine;

namespace EcoBuilder.UI
{
    public class Name : MonoBehaviour
    {
        public event Action<string> OnUserNameChanged;

        TMPro.TMP_InputField input;
        Color defaultColour;
        [SerializeField] Color userColour;
        public bool Interactable {
            get { return input.interactable; }
            set { input.interactable = value; }
        }
        void Awake()
        {
            input = GetComponent<TMPro.TMP_InputField>();
            input.onValueChanged.AddListener(OnValueChanged);
            defaultColour = input.textComponent.color;
        }
        private void OnValueChanged(string s)
        {
            OnUserNameChanged.Invoke(s);
            input.textComponent.color = Color.blue;
        }
        public void SetNameWithoutCallback(string newName)
        {
            input.onValueChanged.RemoveListener(OnValueChanged);
            input.text = newName;
            input.onValueChanged.AddListener(OnValueChanged);
        }
        public void SetDefaultColour()
        {
            input.textComponent.color = defaultColour;
        }
        public void SetUserColour()
        {
            input.textComponent.color = userColour;
        }
    }
}