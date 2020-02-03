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

        [SerializeField] TMPro.TextMeshProUGUI loginText, registerText, errorText;
        [SerializeField] TMPro.TMP_InputField username, password, email, recipient;
        [SerializeField] TMPro.TMP_Dropdown age, gender, education;
        [SerializeField] Toggle consent, askAgain;
        [SerializeField] Button loginSubmit, resetSubmit, demoSubmit, skipButton, backButton, regButton, loginButton, privacyButton, forgotButton;
        [SerializeField] Sprite greyButtonSprite, redButtonSprite;
        [SerializeField] Image shade;

        void Start()
        {
            loginSubmit.onClick.AddListener(LoginOrRegister);
            demoSubmit.onClick.AddListener(TakeDemographics);
            resetSubmit.onClick.AddListener(SendResetEmail);
            skipButton.onClick.AddListener(()=> SetState(State.Skip));
            backButton.onClick.AddListener(()=> SetState(State.Start));
            regButton.onClick.AddListener(()=> SetState(State.Register));
            loginButton.onClick.AddListener(()=> SetState(State.Login));
            forgotButton.onClick.AddListener(()=> SetState(State.Reset));
            username.onValueChanged.AddListener(s=> CheckUsernameEmail());
            email.onValueChanged.AddListener(b=> CheckUsernameEmail());
            // consent.onValueChanged.AddListener(b=> CheckUsernameEmail());
            recipient.onValueChanged.AddListener(b=> CheckResetRecipient());
            password.onSubmit.AddListener(s=> LoginOrRegister());
            privacyButton.onClick.AddListener(()=> GameManager.Instance.OpenPrivacyPolicyInBrowser());

            SetState(State.Start);
            StartCoroutine(yTween(1,-1000,0,true));
        }

        [SerializeField] GameObject[] startObj, idObj, resetObj, demoObj, skipObj; // required because hierarchy with sub-layout components causes strange frames
        public enum State { Null=-1, Start=0, Skip=1, Register=2, Login=3, Reset=4, Demographics=5, End=6 };
        private State _state = State.Null;
        public void SetState(State state)
        {
            switch (state)
            {
            case State.Start:
                ShowObjectsOnly(startObj);
                username.text = password.text = email.text = "";
                skipButton.image.sprite = greyButtonSprite;
                backButton.interactable = false;
                break;
            case State.Register:
                ShowObjectsOnly(idObj);
                consent.gameObject.SetActive(false);
                forgotButton.gameObject.SetActive(false);
                email.gameObject.SetActive(true);
                loginSubmit.interactable = false;
                loginText.gameObject.SetActive(false);
                registerText.gameObject.SetActive(true);
                break;
            case State.Login:
                ShowObjectsOnly(idObj);
                consent.gameObject.SetActive(false);
                email.gameObject.SetActive(false);
                loginText.gameObject.SetActive(true);
                registerText.gameObject.SetActive(false);
                break;
            case State.Reset:
                ShowObjectsOnly(resetObj);
                skipButton.interactable = false;
                break;
            case State.Demographics:
                ShowObjectsOnly(demoObj);
                ChooseTeam(); // only select a team if registerng
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
                    askAgain.isOn = false;
                    skipButton.image.sprite = redButtonSprite;
                }
                break;
            case State.End:
                OnFinished.Invoke();
                StartCoroutine(yTween(1,0,-1000,false));
                break;
            }
            _state = state;
        }
        void ChooseTeam()
        {
            // this was previously done by coin, but will now be hidden to the user
            bool heads = UnityEngine.Random.Range(0, 2) == 0;
            var team = heads? GameManager.PlayerDetails.Team.Lion : GameManager.PlayerDetails.Team.Wolf;
            GameManager.Instance.SetTeamLocal(team);
            GameManager.Instance.SetTeamRemote();
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
            SetActives(resetObj, false);
            SetActives(demoObj, false);
            SetActives(skipObj, false);
            backButton.interactable = true;
            skipButton.interactable = true;
            errorText.gameObject.SetActive(false);
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

        void CheckUsernameEmail()
        {
            if (_state == State.Register) {
                loginSubmit.interactable = UsernameOkay() && EmailOkay();// && consent.isOn;
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
            } else {
                throw new Exception("bad state");
            }
        }
        void LoggedinCallback(bool success, string msg)
        {
            if (success) {
                SetState(_state==State.Register? State.Demographics : State.End);
                errorText.text = "Done!";
            } else {
                loginSubmit.interactable = true;
                errorText.text = msg;
            }
        }
        void TakeDemographics()
        {
            GameManager.Instance.SetDemographicsLocal(age.value, gender.value, education.value);
            GameManager.Instance.SetDemographicsRemote();
            SetState(State.End);
        }
        void SendResetEmail()
        {
            resetSubmit.interactable = false;
            GameManager.Instance.SendPasswordResetEmail(recipient.text, ResetCallback);
        }
        void ResetCallback(bool success, string msg)
        {
            errorText.gameObject.SetActive(true);
            resetSubmit.interactable = false;
            if (success) {
                recipient.text = "";
                errorText.text = "Reset email sent!";
            } else {
                errorText.text = msg;
            }
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