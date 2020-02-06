using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

namespace EcoBuilder.UI
{
    public class Menu : MonoBehaviour
    {
        // NOTE: most of the functionality of this class is in UnityEvents!
        //       don't ask me why I chose to do it this way and opposite in Registration
        [SerializeField] GridLayoutGroup learningLevels;
        [SerializeField] VerticalLayoutGroup researchLevels;
        [SerializeField] Registration form;
        [SerializeField] Toggle reverseDrag;

        [SerializeField] Button researchWorld;
        [SerializeField] Image researchLock;

        [SerializeField] Button quit, logout, createAccount, deleteAccount;
        [SerializeField] TMPro.TextMeshProUGUI accountStatus;

        void Start()
        {
            ResetHelpToSplash();
            if (GameManager.Instance.AskForRegistration) {
                StartRegistration();
            } else {
                ShowMainMenu();
            }
#if UNITY_WEBGL
            quit.gameObject.SetActive(false);
#endif
        }

        void StartRegistration()
        {
            form.gameObject.SetActive(true);
            form.OnFinished += StartMainMenuFromReg;
        }
        void StartMainMenuFromReg()
        {
            form.OnFinished -= StartMainMenuFromReg;
            ShowMainMenu();
            ShowHelpDelay(splashHelp, 2f);
        }

        [SerializeField] Levels.Leaderboard leaderboardPrefab;
        [SerializeField] List<Levels.Level> learningLevelPrefabs;
        [SerializeField] List<Levels.Level> researchLevelPrefabs;
        void ShowMainMenu()
        {
            // instantiate all levels and unlock ones that can be played
            var unlockedIdxs = new HashSet<int>();
            Action<Levels.Level> CheckUnlocked = (l)=> {
                if (GameManager.Instance.GetHighScoreLocal(l.Details.idx) > 0)
                {
                    unlockedIdxs.Add(l.Details.idx);
                    if (l.NextLevelPrefab!=null) {
                        unlockedIdxs.Add(l.NextLevelPrefab.Details.idx);
                    }
                }
            };
            unlockedIdxs.Add(learningLevelPrefabs[0].Details.idx); // always unlock first level
            var instantiated = new Dictionary<int, Levels.Level>();
            foreach (var prefab in learningLevelPrefabs)
            {
                var parent = new GameObject().AddComponent<RectTransform>();
                parent.SetParent(learningLevels.transform);
                parent.localScale = Vector3.one;
                parent.name = prefab.Details.idx.ToString();

                CheckUnlocked.Invoke(prefab);
                var level = Instantiate(prefab, parent);
                instantiated[level.Details.idx] = level;
            }
            Action SetResearchLeaderboards = null;
            if (IsLearningFinished())
            {
                unlockedIdxs.Add(researchLevelPrefabs[0].Details.idx); // always unlock first level
                researchWorld.interactable = true;
                researchWorld.GetComponentInChildren<TMPro.TextMeshProUGUI>().color = Color.white;
                researchLock.enabled = false;
                foreach (var prefab in researchLevelPrefabs)
                {
                    var leaderboard = Instantiate(leaderboardPrefab, researchLevels.transform);
                    CheckUnlocked.Invoke(prefab);
                    var level = leaderboard.GiveLevelPrefab(prefab);
                    instantiated[level.Details.idx] = level;
                    SetResearchLeaderboards += leaderboard.SetFromGameManagerCache;
                }
            }
            foreach (var idx in unlockedIdxs) {
                instantiated[idx].Unlock();
            }
            // get level high scores and medians from server
            print("TODO: more than just top 3 scores on scroll");
            GameManager.Instance.CacheLeaderboardsRemote(3, SetResearchLeaderboards);

            // set up settings menu
            reverseDrag.isOn = GameManager.Instance.ReverseDragDirection;
            if (GameManager.Instance.LoggedIn)
            {
                accountStatus.text = "Hello, " + GameManager.Instance.Username + "! You have achieved TODO: out of TODO: stars.";
                createAccount.gameObject.SetActive(false);
                logout.gameObject.SetActive(true);
                deleteAccount.gameObject.SetActive(true);
            }
            else
            {
                accountStatus.text = "You are not currently logged in.";
                createAccount.gameObject.SetActive(true);
                logout.gameObject.SetActive(false);
                deleteAccount.gameObject.SetActive(false);
            }

            GetComponent<Animator>().SetTrigger("Reveal");
            StartCoroutine(WaitThenShowLogo(.7f));
        }
        bool IsLearningFinished()
        {
            foreach (var level in learningLevelPrefabs) {
                if (GameManager.Instance.GetHighScoreLocal(level.Details.idx) <= 0) {
                    return false;
                }
            }
            return true;
        }

        [SerializeField] GameObject logo;
        IEnumerator WaitThenShowLogo(float waitSeconds)
        {
            yield return new WaitForSeconds(waitSeconds);
            logo.SetActive(true);
        }

        ////////////////////////
        // for outside things to attach to
        public void QuitGame()
        {
            GameManager.Instance.Quit();
        }
        public void DeleteAccount()
        {
            GameManager.Instance.DeleteAccountRemote((b,s)=> print(s));
        }
        public void CreateAccount()
        {
            GameManager.Instance.CreateAccount();
        }
        public void LogOut()
        {
            GameManager.Instance.LogOut();
        }
        public void SetReverseDrag(bool reversed)
        {
            GameManager.Instance.SetDragDirectionLocal(reversed);
            if (GameManager.Instance.LoggedIn) {
                GameManager.Instance.SetDragDirectionRemote();
            }
        }
        public void OpenPrivacyPolicy()
        {
            GameManager.Instance.OpenPrivacyPolicyInBrowser();
        }
        [SerializeField] string splashHelp;
        public void ResetHelpToSplash()
        {
            GameManager.Instance.SetHelpText(splashHelp, false, 0, true);
        }
        public void ResetHelpToSplashDelay(float delay)
        {
            GameManager.Instance.SetHelpText(splashHelp, false, delay, true);
        }
        public void SetHelp(string message)
        {
            GameManager.Instance.SetHelpText(message);
        }
        public void ShowHelp(string message)
        {
            GameManager.Instance.SetHelpText(message, true, 0);
        }
        public void HideHelp()
        {
            GameManager.Instance.HideHelpText();
        }
        public void ShowHelpDelay(string message, float delay)
        {
            GameManager.Instance.SetHelpText(message, true, delay);
        }
    }
}
