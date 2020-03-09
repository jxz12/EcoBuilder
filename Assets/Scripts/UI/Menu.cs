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
        // NOTE: most of the functionality of this class is in scene UnityEvents!
        //       don't ask me why I do the opposite in Registration...
        [SerializeField] GridLayoutGroup learningGrid;
        [SerializeField] VerticalLayoutGroup researchList;
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
                    if (score > lvl.Details.TwoStarScore) collectedStars += 1;
                    if (score > lvl.Details.ThreeStarScore) collectedStars += 1;
                    totalStars += 3;
                }
            };
            Level prefab = firstLearningLevel;
            while (prefab != null)
            {
                CheckUnlocked.Invoke(prefab);

                var parent = new GameObject().AddComponent<RectTransform>();
                parent.SetParent(learningGrid.transform);
                parent.localScale = Vector3.one;
                parent.name = prefab.Details.Idx.ToString();

                instantiated[prefab.Details.Idx] = Instantiate(prefab, parent);

                prefab = prefab.NextLevelPrefab;
            }
            // then do research levels
            Action SetResearchLeaderboards = null;

            // this is to make sure the tutorial does not have a leaderboard
            var firstResearchParent = new GameObject().AddComponent<RectTransform>();
            firstResearchParent.SetParent(researchList.transform, false);
            firstResearchParent.name = firstResearchLevel.Details.Idx.ToString();
            instantiated[firstResearchLevel.Details.Idx] = Instantiate(firstResearchLevel, firstResearchParent);
            prefab = firstResearchLevel.NextLevelPrefab;
            while (prefab != null)
            {
                CheckUnlocked.Invoke(prefab);
                var leaderboard = Instantiate(leaderboardPrefab, researchList.transform);
                instantiated[prefab.Details.Idx] = leaderboard.GiveLevelPrefab(prefab);
                SetResearchLeaderboards += leaderboard.SetFromGameManagerCache;
                prefab = prefab.NextLevelPrefab;
            }

            if (IsLearningFinished())
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
                accountStatus.text = $"Hello, {GameManager.Instance.Username}! You have collected {collectedStars} out of {totalStars} stars.";
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

            StartCoroutine(WaitThenShowLogo(.7f));
            StartCoroutine(WaitThenDisableCanvases());
            Reveal();
        }
        bool IsLearningFinished()
        {
#if UNITY_EDITOR
            return true;
#endif
            var prefab = firstLearningLevel;
            while (prefab != null) {
                if (GameManager.Instance.GetHighScoreLocal(prefab.Details.Idx) < 0) {
                    return false;
                }
                prefab = prefab.NextLevelPrefab;
            }
            return true;
        }

        [SerializeField] Animator logoAnim;
        IEnumerator WaitThenShowLogo(float waitSeconds)
        {
            yield return new WaitForSeconds(waitSeconds);
            logoAnim.enabled = true;
        }
        [SerializeField] Canvas learningCanvas, researchCanvas;
        IEnumerator WaitThenDisableCanvases(float delay=0)
        {
            // this delay is so that textmeshpro components don't get messed up
            yield return null;
            yield return new WaitForSeconds(delay);
            learningCanvas.enabled = researchCanvas.enabled = false;
        }

        ////////////////////////
        // for outside things to attach to
        public void QuitGame()
        {
            GameManager.Instance.Quit();
        }
        public void DeleteAccount()
        {
            GameManager.Instance.DeleteAccount();
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

        ///////////////
        // animation
        [SerializeField] RectTransform splashRT, levelsRT, settingsRT, returnRT;
        enum State { Hidden, Splash, Levels, Settings };
        private State state;
        void Reveal()
        {
            TweenY(splashRT, -1000, 0);

            Assert.IsTrue(state == State.Hidden);
            state = State.Splash;
            GameManager.Instance.HelpText.Message = splashHelp;
        }
        public void ShowSplash()
        {
            ClearTweens();
            TweenY(splashRT, -1000, 0);
            TweenY(returnRT, 60, -60);

            Assert.IsTrue(state==State.Levels || state==State.Settings);
            if (state == State.Levels) {
                TweenY(levelsRT, 0, -1000);
            } else if (state == State.Settings) {
                TweenY(settingsRT, 0, -1000);
            }
        }
        public void ShowLevels()
        {
            ClearTweens();
            TweenY(splashRT, 0, -1000);
            TweenY(levelsRT, -1000, 0);
            TweenY(returnRT, -60, 60);

            state = State.Levels;
        }
        public void ShowSettings()
        {
            ClearTweens();
            TweenY(splashRT, 0, -1000);
            TweenY(settingsRT, -1000, 0);
            TweenY(returnRT, -60, 60);

            state = State.Settings;
        }

        private List<IEnumerator> navigationRoutines = new List<IEnumerator>();
        private void ClearTweens()
        {
            foreach (var routine in navigationRoutines) {
                StopCoroutine(routine);
            }
            navigationRoutines.Clear();
        }
        private void TweenY(RectTransform toMove, float startY, float endY)
        {
            var routine = TweenYRoutine(toMove, startY, endY);
            StartCoroutine(routine);
            navigationRoutines.Add(routine);
        }
        [SerializeField] float tweenDuration;
        private IEnumerator TweenYRoutine(RectTransform toMove, float startY, float endY)
        {
            Vector3 startPos = toMove.anchoredPosition;
            float startTime = Time.time;
            while (Time.time < startTime+tweenDuration)
            {
                float t = (Time.time-startTime) / tweenDuration;
                // quadratic ease in-out
                if (t < .5f) {
                    t = 2*t*t;
                } else {
                    t = -1 + (4-2*t)*t;
                }
                float y = Mathf.Lerp(startY, endY, t);
                toMove.anchoredPosition = new Vector3(startPos.x, y, startPos.z);
                yield return null;
            }
            toMove.anchoredPosition = new Vector3(startPos.x, endY, startPos.z);
        }
    }
}
