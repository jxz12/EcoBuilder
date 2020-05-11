using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace EcoBuilder.UI
{
    public class Registration : MonoBehaviour
    {
        // NOTE: none of the functionality of this class is in UnityEvents!
        //       don't ask me why I chose to do it this way and opposite in Menu
        public event Action OnFinished;

        [SerializeField] TMPro.TextMeshProUGUI loginText, registerText, errorText;
        [SerializeField] TMPro.TMP_InputField username, password, email, recipient;
        [SerializeField] TMPro.TMP_Dropdown age, gender, education;
        [SerializeField] Toggle termsConsent, emailConsent, askAgain;
        [SerializeField] Button skipButton, backButton, cancelButton, regGoto, loginGoto, forgotGoto;
        [SerializeField] Button loginSubmit, resetSubmit, gdprSubmit, demoSubmit;

        [SerializeField] Sprite greyButtonSprite, redButtonSprite;
        [SerializeField] Image shade;

        void Awake()
        {
            skipButton.onClick.AddListener(()=> SetState(State.Skip));
            backButton.onClick.AddListener(()=> SetState(State.Start));
            regGoto.onClick.AddListener(()=> SetState(State.Register));
            loginGoto.onClick.AddListener(()=> SetState(State.Login));
            forgotGoto.onClick.AddListener(()=> SetState(State.Reset));

            loginSubmit.onClick.AddListener(LoginOrRegister);
            gdprSubmit.onClick.AddListener(TakeGDPR);
            demoSubmit.onClick.AddListener(TakeDemographics);
            resetSubmit.onClick.AddListener(SendResetEmail);
            cancelButton.onClick.AddListener(CancelRegistration);

            username.onValueChanged.AddListener(s=> CheckUsernameEmail());
            email.onValueChanged.AddListener(b=> CheckUsernameEmail());
            password.onSubmit.AddListener(s=> LoginOrRegister());
            recipient.onValueChanged.AddListener(b=> CheckResetRecipient());

            termsConsent.onValueChanged.AddListener(b=> gdprSubmit.interactable = b);

            // // to stop Unity UI complaining
            // foreach (var fitter in fittersToDisable) {
            //     fitter.enabled = false;
            // }
            // errorText.GetComponent<ContentSizeFitter>().enabled = true;
        }
        public void Begin()
        {
            SetState(State.Start);
            StartCoroutine(TweenY(1,-1000,0,true));
        }

        [SerializeField] GameObject[] startObj, idObj, gdprObj, resetObj, demoObj, skipObj; // required because hierarchy with sub-layout components causes strange frames
        // [SerializeField] ContentSizeFitter[] fittersToDisable;
        [SerializeField] GameObject navObj;
        public enum State { Null, Start, Skip, Register, Login, GDPR, Reset, Demographics, End };
        private State _state = State.Null;
        public void SetState(State state)
        {
            switch (state)
            {
            case State.Start:
                ShowObjectsOnly(startObj);
                username.text = password.text = email.text = "";
                termsConsent.isOn = emailConsent.isOn = askAgain.isOn = false;
                skipButton.image.sprite = greyButtonSprite;
                backButton.interactable = false; 
                if (_state == State.GDPR) {
                    errorText.gameObject.SetActive(true);
                    errorText.text = "Account deleted!";
                }
                break;
            case State.Register:
                ShowObjectsOnly(idObj);
                forgotGoto.gameObject.SetActive(false);
                email.gameObject.SetActive(true);
                loginSubmit.interactable = false;
                loginText.gameObject.SetActive(false);
                registerText.gameObject.SetActive(true);
                break;
            case State.Login:
                ShowObjectsOnly(idObj);
                email.gameObject.SetActive(false);
                loginText.gameObject.SetActive(true);
                registerText.gameObject.SetActive(false);
                break;
            case State.GDPR:
                ShowObjectsOnly(gdprObj);
                skipButton.interactable = false;
                backButton.interactable = false;
                emailConsent.gameObject.SetActive(email.text != "");
                navObj.SetActive(false);
                break;
            case State.Reset:
                ShowObjectsOnly(resetObj);
                skipButton.interactable = false;
                break;
            case State.Demographics:
                ShowObjectsOnly(demoObj);
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
                    ShowObjectsOnly(skipObj);
                    skipButton.image.sprite = redButtonSprite;
                }
                break;
            case State.End:
                OnFinished?.Invoke();
                StartCoroutine(TweenY(1,0,-1000,false));
                break;
            }
            _state = state;
        }
        private void ShowObjectsOnly(GameObject[] objects)
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
            SetActives(gdprObj, false);
            SetActives(resetObj, false);
            SetActives(demoObj, false);
            SetActives(skipObj, false);
            backButton.interactable = true;
            skipButton.interactable = true;
            cancelButton.interactable = true;
            errorText.gameObject.SetActive(false);
            navObj.SetActive(true);
        }

        IEnumerator TweenY(float duration, float yStart, float yEnd, bool applyShade)
        {
            Vector3 startPos = new Vector3(0, yStart, 0);
            Vector3 endPos = new Vector3(0, yEnd, 0);

            GetComponent<Canvas>().enabled = true;
            float startTime = Time.time;
            shade.raycastTarget = applyShade;
            shade.color = new Color(0,0,0,applyShade?0:.5f);
            while (Time.time < startTime+duration)
            {
                float t = Tweens.QuadraticInOut((Time.time-startTime)/duration);
                transform.localPosition = Vector3.Lerp(startPos, endPos, t);
                shade.color = new Color(0,0,0,Mathf.Lerp(applyShade?0:.5f, applyShade?.5f:0, t));
                yield return null;
            }
            transform.localPosition = endPos;
            shade.color = new Color(0,0,0,applyShade?.5f:0);
            if (!applyShade) {
                GetComponent<Canvas>().enabled = false;
            }
        }

        void CheckUsernameEmail()
        {
            if (_state == State.Register) {
                loginSubmit.interactable = UsernameOkay() && EmailOkay();
            } else {
                loginSubmit.interactable = UsernameOkay();
            }
        }
        private bool UsernameOkay()
        {
            return username.text.Length > 0;
        }
        private static Regex emailRegex = new Regex(@"(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|""(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*"")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])");
        private bool EmailOkay()
        {
            return string.IsNullOrEmpty(email.text) || emailRegex.IsMatch(email.text);
        }
        void CheckResetRecipient()
        {
            resetSubmit.interactable = !string.IsNullOrEmpty(recipient.text);
        }
        void LoginOrRegister()
        {
            Assert.IsTrue(_state==State.Register || _state==State.Login, "not registering or logging in");

            loginSubmit.interactable = false;
            errorText.gameObject.SetActive(true);
            errorText.text = "Connecting...";
            if (_state == State.Register)
            {
                GameManager.Instance.RegisterLocal(username.text, password.text, email.text);
                GameManager.Instance.RegisterRemote(LoggedinCallback);
            }
            else if (_state == State.Login)
            {
                GameManager.Instance.LoginRemote(username.text, password.text, LoggedinCallback);
            }
        }
        void LoggedinCallback(bool success, string msg)
        {
            if (success) {
                SetState(_state==State.Register? State.GDPR : State.End);
                errorText.text = "Done!";
            } else {
                loginSubmit.interactable = true;
                errorText.text = "Error: " + msg;
            }
        }
        void TakeGDPR()
        {
            gdprSubmit.interactable = false;
            errorText.gameObject.SetActive(true);
            errorText.text = "Connecting...";
            GameManager.Instance.SetGDPRRemote(emailConsent.isOn, GDPRCallback);
        }
        void GDPRCallback(bool success, string msg)
        {
            if (success) {
                SetState(State.Demographics);
                errorText.text = "Done!";
            } else {
                gdprSubmit.interactable = true;
                errorText.text = "Error: " + msg;
            }
        }
        void TakeDemographics()
        {
            GameManager.Instance.SetDemographicsRemote(age.value, gender.value, education.value);
            SetState(State.End);
        }
        void SendResetEmail()
        {
            resetSubmit.interactable = false;
            GameManager.Instance.SendPasswordResetEmail(recipient.text, ResetCallback);
            errorText.text = "Sending...";
        }
        void ResetCallback(bool success, string msg)
        {
            errorText.gameObject.SetActive(true);
            if (success) {
                recipient.text = "";
                errorText.text = "Reset email sent!";
            } else {
                errorText.text = msg;
                resetSubmit.interactable = true;
            }
        }
        void CancelRegistration()
        {
            cancelButton.interactable = false;
            errorText.gameObject.SetActive(true);
            errorText.text = "Deleting account";
            GameManager.Instance.DeleteAccountRemote(DeleteCallback);
        }
        void DeleteCallback(bool success, string msg)
        {
            if (success) {
                SetState(State.Start);
            } else {
                cancelButton.interactable = true;
                errorText.text = "Error: " + msg;
            }
        }
        void Update()
        {
            // TODO: shift+tab
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