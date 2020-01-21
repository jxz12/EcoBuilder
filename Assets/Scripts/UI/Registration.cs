using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.UI
{
    public class Registration : MonoBehaviour
    {
        public event Action OnFinished;

        [SerializeField] TMPro.TMP_InputField username, password, email;
        [SerializeField] TMPro.TMP_Dropdown age, gender, education;
        [SerializeField] Toggle GDPR, askAgain;
        [SerializeField] Button loginSubmit, demoSubmit, skipButton, backButton, regButton, loginButton;
        [SerializeField] Sprite greyButton, redButton;
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
                username.text = password.text = email.text = "";
                skipButton.image.sprite = greyButton;
                backButton.interactable = false;
                break;
            case State.Skip:
                if (_state == State.Skip || _state == State.Demographics)
                {
                    if (askAgain.isOn) {
                        GameManager.Instance.DontAskAgainForLogin();
                    }
                    SetState(State.End);
                }
                else
                {
                    ResetObjects();
                    skipObj.SetActive(true);
                    askAgain.isOn = false;
                    skipButton.image.sprite = redButton;
                }
                break;
            case State.Register:
                ResetObjects();
                // FIXME: unity is very annoying (one frame error crap)
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
                backButton.interactable = false;
                demoObj.SetActive(true);
                break;
            case State.End:
                OnFinished.Invoke();
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
            username.onValueChanged.AddListener(s=> CheckUsernameEmail());
            GDPR.onValueChanged.AddListener(b=> CheckUsernameEmail());
            email.onValueChanged.AddListener(b=> CheckUsernameEmail());
        }
        public void Reveal()
        {
            SetState(State.Start);
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
                if (t < .5f) {
                    t = 2*t*t;
                } else {
                    t = -1 + (4-2*t)*t;
                }
                transform.localPosition = Vector3.Lerp(startPos, endPos, t);
                shade.color = Color.Lerp(startCol, new Color(0,0,0,applyShade?.5f:0), t);
                yield return null;
            }
            if (!applyShade) {
                gameObject.SetActive(false);
            }
        }

        public void CheckUsernameEmail()
        {
            if (_state == State.Register) {
                loginSubmit.interactable = UsernameOkay() && EmailOkay() && GDPR.isOn;
            } else {
                loginSubmit.interactable = UsernameOkay();
            }
        }
        private bool UsernameOkay()
        {
            print("TODO: profanity filter?");
            return username.text.Length > 0;
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
                GameManager.Instance.RegisterLocal(username.text, password.text, email.text);
                GameManager.Instance.RegisterRemote(b=>{ if (b) SetState(State.Demographics); });
            }
            else if (_state == State.Login)
            {
                GameManager.Instance.LoginRemote(username.text, password.text, b=>{ if (b) SetState(State.End); });
            } else {
                throw new Exception("bad state");
            }
        }
        public void TakeDemographics()
        {
            GameManager.Instance.SetDemographicsLocal(age.value, gender.value, education.value);
            GameManager.Instance.SetDemographicsRemote(s=>SetState(State.End));
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (username.isFocused) {
                    password.ActivateInputField();
                } else if (password.isFocused) {
                    if (_state == State.Register) {
                        email.ActivateInputField();
                    } else {
                        username.ActivateInputField();
                    }
                } else if (email.isFocused) {
                    username.ActivateInputField();
                }
            }
        }
    }
}