using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
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
            form.Begin();
            form.OnFinished += StartMainMenuFromReg;
        }
        void StartMainMenuFromReg()
        {
            form.OnFinished -= StartMainMenuFromReg;
            ShowMainMenu();
        }

        [SerializeField] UI.Leaderboard leaderboardPrefab;
        [SerializeField] Level firstLearningLevel;
        [SerializeField] Level firstResearchLevel;
        void ShowMainMenu()
        {
            // instantiate all levels and unlock ones that can be played
            var instantiated = new Dictionary<int, Level>();
            var unlockedIdxs = new HashSet<int>();

            unlockedIdxs.Add(firstLearningLevel.Details.Idx);
            unlockedIdxs.Add(firstResearchLevel.Details.Idx);
            int collectedStars = 0;
            int totalStars = 0;

            // lol
            Action<Level> CheckUnlocked = (lvl)=> {
                Assert.IsFalse(instantiated.ContainsKey(lvl.Details.Idx), "two level prefabs with same idx");
                int score = GameManager.Instance.GetHighScoreLocal(lvl.Details.Idx);
                if (score >= 0)
                {
                    unlockedIdxs.Add(lvl.Details.Idx);
                    if (lvl.NextLevelPrefab!=null) {
                        unlockedIdxs.Add(lvl.NextLevelPrefab.Details.Idx);
                    }
                }
                if (lvl.Details.Metric != LevelDetails.ScoreMetric.None)
                {
                    if (score >= 0) collectedStars += 1;
                    if (score > lvl.Details.TargetScore1) collectedStars += 1;
                    if (score > lvl.Details.TargetScore2) collectedStars += 1;
                    totalStars += 3;
                }
            };
            Level prefab = firstLearningLevel;
            while (prefab != null)
            {
                CheckUnlocked.Invoke(prefab);

                var parent = new GameObject().AddComponent<RectTransform>();
                parent.SetParent(learningLevels.transform);
                parent.localScale = Vector3.one;
                parent.name = prefab.Details.Idx.ToString();

                instantiated[prefab.Details.Idx] = Instantiate(prefab, parent);

                prefab = prefab.NextLevelPrefab;
            }
            // then do research levels
            bool learningFinished = IsLearningFinished();
            Action SetResearchLeaderboards = null;
            prefab = firstResearchLevel;
            while (prefab != null)
            {
                CheckUnlocked.Invoke(prefab);
                var leaderboard = Instantiate(leaderboardPrefab, researchLevels.transform);
                instantiated[prefab.Details.Idx] = leaderboard.GiveLevelPrefab(prefab);
                SetResearchLeaderboards += leaderboard.SetFromGameManagerCache;
                prefab = prefab.NextLevelPrefab;
            }
            if (learningFinished)
            {
                researchWorld.interactable = true;
                researchWorld.GetComponentInChildren<TMPro.TextMeshProUGUI>().color = Color.white; // remove transparency
                researchLock.enabled = false;
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
                accountStatus.text = "Hello, " + GameManager.Instance.Username + "! You have collected " + collectedStars + " out of " + totalStars + " stars.";
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
            GameManager.Instance.HelpText.Message = splashHelp;
        }
        bool IsLearningFinished()
        {
            var prefab = firstLearningLevel;
            while (prefab != null) {
                if (GameManager.Instance.GetHighScoreLocal(prefab.Details.Idx) <= 0) {
                    return false;
                }
                prefab = prefab.NextLevelPrefab;
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
            GameManager.Instance.DeleteAccountRemote((b,s)=> print("TODO: show error if could not delete"));
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
        [SerializeField] string lockHelp;
        public void ResetHelpToSplash()
        {
            GameManager.Instance.HelpText.Message = splashHelp;
            GameManager.Instance.HelpText.ResetPosition();
        }
        public void SetHelpHeight(float height)
        {
            GameManager.Instance.HelpText.SetAnchorHeight(height);
        }
        public void ToggleLockHelp()
        {
            if (!GameManager.Instance.HelpText.Showing || GameManager.Instance.HelpText.Message != lockHelp)
            {
                GameManager.Instance.HelpText.Message = lockHelp;
                GameManager.Instance.HelpText.Showing = true;
            }
            else
            {
                GameManager.Instance.HelpText.DelayThenSet(.5f, splashHelp);
                GameManager.Instance.HelpText.Showing = false;
            }
        }
        public void SetHelp(string message)
        {
            GameManager.Instance.HelpText.Message = message;
        }
        public void ShowHelp(string message)
        {
            GameManager.Instance.HelpText.Message = message;
            GameManager.Instance.HelpText.Showing = true;
        }
        public void HideHelp()
        {
            GameManager.Instance.HelpText.Showing = false;
        }
    }
}
