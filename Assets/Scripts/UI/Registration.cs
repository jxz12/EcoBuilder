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

        [SerializeField] TMPro.TextMeshProUGUI loginText, registerText;
        [SerializeField] TMPro.TMP_InputField username, password, email;
        [SerializeField] TMPro.TMP_Dropdown age, gender, education;
        [SerializeField] Toggle GDPR, askAgain;
        [SerializeField] Button loginSubmit, demoSubmit, skipButton, backButton, regButton, loginButton;
        [SerializeField] Sprite greyButton, redButton;
        [SerializeField] Image shade;

        [SerializeField] GameObject[] startObj, idObj, demoObj, skipObj;
        public enum State { Null=-1, Start=0, Skip=1, Register=2, Login=3, Demographics=4, End=5 };
        private State _state = State.Null;
        public void SetState(State state)
        {
            switch (state)
            {
            case State.Start:
                ShowObjects(startObj);
                username.text = password.text = email.text = "";
                skipButton.image.sprite = greyButton;
                backButton.interactable = false;
                break;
            case State.Register:
                ShowObjects(idObj);
                GDPR.isOn = false;
                GDPR.gameObject.SetActive(true);
                email.gameObject.SetActive(true);
                loginSubmit.interactable = false;
                loginText.gameObject.SetActive(false);
                registerText.gameObject.SetActive(true);
                break;
            case State.Login:
                ShowObjects(idObj);
                GDPR.gameObject.SetActive(false);
                email.gameObject.SetActive(false);
                loginText.gameObject.SetActive(true);
                registerText.gameObject.SetActive(false);
                break;
            case State.Demographics:
                ShowObjects(demoObj);
                backButton.interactable = false;
                break;
            case State.End:
                OnFinished.Invoke();
                Disappear();
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
                    SetActives(skipObj, true);
                    askAgain.isOn = false;
                    skipButton.image.sprite = redButton;
                }
                break;
            }
            _state = state;
        }
        private void ShowObjects(GameObject[] objects)
        {
            ResetObjects();
            SetActives(objects, true);
        }
        private void SetActives(GameObject[] objects, bool actives)
        {
            foreach (var go in objects) {
                go.SetActive(actives);
            }
        }
        private void ResetObjects()
        {
            SetActives(startObj, false);
            SetActives(idObj, false);
            SetActives(demoObj, false);
            SetActives(skipObj, false);
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
            email.onValueChanged.AddListener(b=> CheckUsernameEmail());
            password.onSubmit.AddListener(s=> LoginOrRegister());
            GDPR.onValueChanged.AddListener(b=> CheckUsernameEmail());
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
                    if (_state == State.Login) {
                        password.ActivateInputField();
                    } else {
                        email.ActivateInputField();
                    }
                } else if (email.isFocused) {
                    password.ActivateInputField();
                } else if (password.isFocused) {
                    username.ActivateInputField();
                }
            }
        }
    }
}