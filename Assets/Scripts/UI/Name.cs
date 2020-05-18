using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Assertions;

namespace EcoBuilder.UI
{
    public class Name : MonoBehaviour
    {
        public event Action<string> OnUserNameChanged;

        Color defaultColour;
        [SerializeField] Color userColour;
        [SerializeField] TMPro.TMP_InputField input;
        public bool Interactable {
            get { return input.interactable; }
            set { input.interactable = value; }
        }
        void Awake()
        {
            input.onValueChanged.AddListener(OnValueChanged);
            defaultColour = input.textComponent.color;

            // attach drop to shield eventtrigger
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener(ped=>DropShield());
            shield.triggers.Add(entry);

            textRT = input.textComponent.GetComponent<RectTransform>();
            shieldRT = shield.GetComponent<RectTransform>();
            Assert.IsTrue(textRT.sizeDelta == shieldRT.sizeDelta);
            defaultWidth = textRT.rect.width;
        }
        private void OnValueChanged(string s)
        {
            OnUserNameChanged?.Invoke(s);
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

        // this is because Unity autolayout sucks and/or inputfield caret is crap
        RectTransform textRT, shieldRT;
        float defaultWidth;
        bool expanded = false;
        [SerializeField] float expandedWidth=269;
        public void ExpandIntoRefrove(bool expand)
        {
            if (expand && !expanded)
            {
                textRT.sizeDelta = shieldRT.sizeDelta = new Vector2(expandedWidth, textRT.sizeDelta.y);
            }
            else if (!expand && expanded)
            {
                textRT.sizeDelta = shieldRT.sizeDelta = new Vector2(defaultWidth, textRT.sizeDelta.y);
            }
            input.textComponent.ForceMeshUpdate();
            expanded = expand;
        }

        float showTime;
        [SerializeField] EventTrigger shield;
        [SerializeField] float doubleTapThreshold = .5f;
        public void DropShield()
        {
            var shieldImage = shield.GetComponent<Image>();
            StartCoroutine(Drop());
            IEnumerator Drop()
            {
                shieldImage.raycastTarget = false;
                showTime = Time.time + doubleTapThreshold;
                while (Time.time < showTime)
                {
                    yield return null;
                }
                shieldImage.raycastTarget = true;
            }
        }
    }
}