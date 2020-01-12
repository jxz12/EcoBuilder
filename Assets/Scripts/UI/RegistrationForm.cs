using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace EcoBuilder.UI
{
    public class RegistrationForm : MonoBehaviour
    {
        public event Action<bool> OnFinished;

        [SerializeField] TMPro.TMP_InputField username, password, email;
        [SerializeField] TMPro.TMP_Dropdown age, gender, education;
        [SerializeField] Toggle GDPR, askagain;
        [SerializeField] Button loginSubmit, demoSubmit, skipButton, backButton, regButton, loginButton;
        [SerializeField] Sprite tanButton, redButton;
        [SerializeField] Image shade;

        [SerializeField] GameObject startObj, skipObj, idObj, demoObj;
        public enum State { Null=-1, Start=0, Skip=1, Register=2, Login=3, Demographics=4, End=5 };
        private State _state = State.Null;
        public void SetState(State state)
        {
            switch (state)
            {
                case State.Start:
                    ResetObjects();
                    startObj.SetActive(true);
                    skipButton.image.sprite = tanButton;
                    backButton.interactable = false;
                    break;
                case State.Skip:
                    if (_state == State.Skip)
                    {
                        SetState(State.End);
                        if (askagain.isOn)
                            GameManager.Instance.DontAskAgainForLogin();
                        OnFinished.Invoke(false);
                    }
                    else
                    {
                        ResetObjects();
                        skipObj.SetActive(true);
                        skipButton.image.sprite = redButton;
                    }
                    break;
                case State.Register:
                    ResetObjects();
                    // FIXME: unity is a pile of shit
                    idObj.SetActive(true);
                    GDPR.isOn = false;
                    GDPR.gameObject.SetActive(true);
                    email.gameObject.SetActive(true);
                    loginSubmit.interactable = false;
                    break;
                case State.Login:
                    GDPR.gameObject.SetActive(false);
                    email.gameObject.SetActive(false);
                    ResetObjects();
                    idObj.SetActive(true);
                    break;
                case State.Demographics:
                    ResetObjects();
                    demoObj.SetActive(true);
                    break;
                case State.End:
                    Disappear();
                    break;
            }
            _state = state;
        }
        private void ResetObjects()
        {
            startObj.SetActive(false);
            skipObj.SetActive(false);
            idObj.SetActive(false);
            demoObj.SetActive(false);
            backButton.interactable = true;
        }

        void Start()
        {
            loginSubmit.onClick.AddListener(()=> LoginOrRegister());
            demoSubmit.onClick.AddListener(()=> TakeDemographics());
            skipButton.onClick.AddListener(()=> SetState(State.Skip));
            backButton.onClick.AddListener(()=> SetState(State.Start));
            regButton.onClick.AddListener(()=> SetState(State.Register));
            loginButton.onClick.AddListener(()=> SetState(State.Login));
            username.onValueChanged.AddListener(s=> CheckUsername());
            GDPR.onValueChanged.AddListener(b=> CheckUsername());
            SetState(State.Start);
            Appear();
        }
        void Appear()
        {
            StartCoroutine(yTween(1,-1000,0,true));
        }
        void Disappear()
        {
            StartCoroutine(yTween(1,0,-1000,false));
        }
        IEnumerator yTween(float duration, float yStart, float yEnd, bool applyShade)
        {
            Color startCol = new Color(0,0,0,applyShade?0:.5f);
            Vector3 startPos = new Vector3(0, yStart, 0);
            Vector3 endPos = new Vector3(0, yEnd, 0);

            float startTime = Time.time;
            while (Time.time < startTime+duration)
            {
                float t = (Time.time-startTime)/duration;
                // quadratic ease in-out
                if (t < .5f)
                    t = 2*t*t;
                else
                    t = -1 + (4-2*t)*t;

                transform.localPosition = Vector3.Lerp(startPos, endPos, t);
                shade.color = Color.Lerp(startCol, new Color(0,0,0,applyShade?.5f:0), t);
                yield return null;
            }
            if (!applyShade)
                gameObject.SetActive(false);
        }

        public void CheckUsername()
        {
            if (_state == State.Register)
            {
                loginSubmit.interactable = UsernameOkay() && GDPR.isOn;
            }
            else
            {
                loginSubmit.interactable = UsernameOkay();
            }
        }
        private bool UsernameOkay()
        {
            return true; // TODO: profanity filter?
        }
        private static Regex emailRegex = new Regex(@"(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|""(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*"")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])");
        private bool EmailOkay()
        {
            return string.IsNullOrEmpty(email.text) || emailRegex.IsMatch(email.text);
        }
        public void LoginOrRegister()
        {
            if (_state == State.Register)
            {
                GameManager.Instance.Register(username.text, password.text, email.text);
                StartCoroutine(GameManager.Instance.TryRegisterRemote());
                // TODO: check if successful
                SetState(State.Demographics);
            }
            else if (_state == State.Login)
            {
                // try to get 
                bool success = GameManager.Instance.Login(username.text, password.text);
                // StartCoroutine(GameManager.Instance.T)
                // TODO: check if successful
                SetState(State.End);
            }
            else
                throw new Exception("bad state");
        }
        public void TakeDemographics()
        {
            GameManager.Instance.SetDemographics(age.value, gender.value, education.value);
            StartCoroutine(GameManager.Instance.TryDemographicsRemote());
            SetState(State.End);
            OnFinished.Invoke(true);
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (username.isFocused)
                    password.ActivateInputField();
                else if (password.isFocused)
                    username.ActivateInputField();
            }
        }
    }
}