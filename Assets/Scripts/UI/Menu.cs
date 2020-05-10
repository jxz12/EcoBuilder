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
        // NOTE: most of the references to this class are in scene UnityEvents!
        //       I actually prefer the way I did it in Registration
        //       because I can use the compiler to track what is calling what
        [SerializeField] GridLayoutGroup learningGrid;
        [SerializeField] HorizontalLayoutGroup researchList;
        [SerializeField] Registration form;
        [SerializeField] Toggle reverseDrag;

        [SerializeField] Button researchWorld;
        [SerializeField] Image researchLock;

        [SerializeField] Button quit, logout, createAccount, deleteAccount;
        [SerializeField] TMPro.TextMeshProUGUI accountStatus;
        [SerializeField] Animator logoAnim;

        void Awake()
        {
            splashCanvas.enabled = settingsCanvas.enabled = learningCanvas.enabled = researchCanvas.enabled = false;
        }
        void Start()
        {
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

        [SerializeField] Leaderboard leaderboard;
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

            Assert.IsFalse(learningGrid.transform.childCount > 0, "learning levels already spawned");
            Assert.IsFalse(researchList.transform.childCount > 0, "research levels already spawned");

            Level toSpawn = firstLearningLevel;
            while (toSpawn != null)
            {
                SpawnLevel(toSpawn, learningGrid.transform);
                toSpawn = toSpawn.NextLevelPrefab;
            }
            toSpawn = firstResearchLevel;
            while (toSpawn != null)
            {
                SpawnLevel(toSpawn, researchList.transform);
                toSpawn = toSpawn.NextLevelPrefab;
            }
            foreach (var idx in unlockedIdxs) {
                instantiated[idx].Unlock();
            }


            bool unlockResearch = ResearchWorldUnlocked();
            researchWorld.interactable = unlockResearch;
            researchWorld.GetComponentInChildren<TMPro.TextMeshProUGUI>().color = unlockResearch? Color.white : new Color(1,1,1,.5f);
            researchLock.enabled = !unlockResearch;
            GameManager.Instance.HelpText.Message = unlockResearch? splashResearchText : splashLearningText;

            int firstLevel = unlockResearch? firstResearchLevel.Details.Idx : firstLearningLevel.Details.Idx;
            if (GameManager.Instance.GetHighScoreLocal(firstLevel) == null)
            {
                // show help on first time learning/researching
                GameManager.Instance.HelpText.DelayThenShow(2f, splashResearchText);
            }

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

            // don't want to keep layoutgroups enabled as they are slow, so only enable for one frame
            EnableThenDisableLevelLayouts();

            // update medians every time menu is entered
            GameManager.Instance.HelpText.ResetMenuPosition();
            GameManager.Instance.CacheMediansRemote();
            logoAnim.SetTrigger("Show");
            Reveal();

            // local functions below
            void SpawnLevel(Level prefab, Transform layout)
            {
                var parent = new GameObject().AddComponent<RectTransform>();
                parent.SetParent(layout);
                parent.localScale = Vector3.one;
                parent.name = prefab.Details.Idx.ToString();

                Assert.IsFalse(instantiated.ContainsKey(prefab.Details.Idx), $"tried to spawn 2 levels with same idx {prefab.Details.Idx}");
                instantiated[prefab.Details.Idx] = Instantiate(prefab, parent);

                long? score = GameManager.Instance.GetHighScoreLocal(prefab.Details.Idx);

                if (score != null || GameManager.Instance.LevelsUnlockedRegardless)
                {
                    unlockedIdxs.Add(prefab.Details.Idx);
                    if (prefab.NextLevelPrefab!=null) {
                        unlockedIdxs.Add(prefab.NextLevelPrefab.Details.Idx);
                    }
                }
                if (prefab.Details.Metric != LevelDetails.ScoreMetric.None && !prefab.Details.ResearchMode) // only have stars for not tutorial or research
                {
                    if (score >= 0) { collectedStars += 1; }
                    if (score > prefab.Details.TwoStarScore) { collectedStars += 1; }
                    if (score > prefab.Details.ThreeStarScore) { collectedStars += 1; }
                    totalStars += 3;
                }
            };
            bool ResearchWorldUnlocked()
            {
                if (GameManager.Instance.LevelsUnlockedRegardless) {
                    return true;
                }
                var prefab = firstLearningLevel;
                while (prefab != null) {
                    if (GameManager.Instance.GetHighScoreLocal(prefab.Details.Idx) == null) {
                        return false;
                    }
                    prefab = prefab.NextLevelPrefab;
                }
                return true;
            }
            void EnableThenDisableLevelLayouts()
            {
                learningGrid.enabled = researchList.enabled = true;
                IEnumerator WaitThenDisable()
                {
                    yield return null;
                    learningGrid.enabled = researchList.enabled = false;
                }
                StartCoroutine(WaitThenDisable());
            }

        }

        ////////////////////////
        // for outside things to attach to
        public void QuitGame()
        {
            GameManager.Instance.Quit();
        }
        public void SetReverseDrag(bool reversed)
        {
            GameManager.Instance.SetDragDirectionLocal(reversed);
            if (GameManager.Instance.LoggedIn) {
                GameManager.Instance.SetDragDirectionRemote();
            }
        }
        public void CreateAccount()
        {
            GameManager.Instance.AskAgainForLogin();
            Reset();
        }
        public void LogOut()
        {
            GameManager.Instance.LogOut(Reset);
        }
        public void DeleteAccount()
        {
            GameManager.Instance.DeleteAccount(Reset);
        }
        public void UnlockLevelsRegardless()
        {
            GameManager.Instance.UnlockAllLevels(Reset);
        }
        [SerializeField] string splashLearningText, splashResearchText;
        [SerializeField] string settingsText;
        [SerializeField] string levelsLearningText, levelsResearchText;
        [SerializeField] string lockHelp;
        public void ResetHelpToSplash()
        {
            GameManager.Instance.HelpText.Message = researchWorld.interactable? splashResearchText : splashLearningText;
            GameManager.Instance.HelpText.ResetMenuPosition();
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
                GameManager.Instance.HelpText.DelayThenSet(.6f, researchWorld.interactable? splashResearchText : splashLearningText);
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

        ///////////////////////////////////
        // panning levels

        [SerializeField] Button learningPanDown, learningPanUp;
        IEnumerator learningPanRoutine;
        int learningPannedAmount = 0;
        public void PanLearning(bool up)
        {
            int numLevels = learningGrid.transform.childCount;
            int numPositions = (numLevels+2) / 3; // assume 3 col per row
            numPositions = Math.Max(1, numPositions-3); // assume 3 row in gallery

            learningPannedAmount += up? 1:-1;
            learningPanDown.interactable = learningPannedAmount > 0;
            learningPanUp.interactable = learningPannedAmount < numPositions-1;

            IEnumerator Pan()
            {
                float yDelta = 100 + learningGrid.spacing.y;

                var rt = learningGrid.GetComponent<RectTransform>();
                float y0 = rt.anchoredPosition.y;
                float y1 = learningPannedAmount * yDelta;
                float duration = .2f;

                float tStart = Time.time;
                while (Time.time < tStart+duration)
                {
                    float y = Mathf.Lerp(y0, y1, Tweens.QuadraticInOut((Time.time-tStart)/duration));
                    rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
                    yield return null;
                }
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y1);
                learningPanRoutine = null;
            }
            if (learningPanRoutine != null) {
                StopCoroutine(learningPanRoutine);
            }
            StartCoroutine(learningPanRoutine = Pan());
        }
        [SerializeField] Button researchPanRight, researchPanLeft;
        IEnumerator researchPanRoutine;
        int researchPannedAmount = 0;
        public void PanResearch(bool left)
        {
            int numLevels = researchList.transform.childCount;

            researchPannedAmount += left? 1:-1;
            researchPanRight.interactable = researchPannedAmount > 0;
            researchPanLeft.interactable = researchPannedAmount < numLevels-1;
            IEnumerator Pan()
            {
                float xDelta = 100 + researchList.spacing;

                var rt = researchList.GetComponent<RectTransform>();
                float x0 = rt.anchoredPosition.x;
                float x1 = -researchPannedAmount * xDelta;
                float duration = .2f;

                float tStart = Time.time;
                while (Time.time < tStart+duration)
                {
                    float x = Mathf.Lerp(x0, x1, Tweens.QuadraticInOut((Time.time-tStart)/duration));
                    rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);
                    yield return null;
                }
                rt.anchoredPosition = new Vector2(x1, rt.anchoredPosition.y);
                researchPanRoutine = null;
            }
            if (researchPanRoutine != null) {
                StopCoroutine(researchPanRoutine);
            }
            StartCoroutine(researchPanRoutine = Pan());
            SetCurrentResearchLeaderboard();
        }
        void SetCurrentResearchLeaderboard()
        {
            // lol
            Level selected = researchList.transform.GetChild(researchPannedAmount).GetChild(0).GetComponent<Level>();
            leaderboard.SwitchLevel(selected.Details.Idx, selected.Details.Title);
        }



        ///////////////////////
        // animation

        [SerializeField] Canvas learningCanvas, researchCanvas, settingsCanvas, splashCanvas, returnCanvas;
        RectTransform SplashRT { get { return splashCanvas.GetComponent<RectTransform>(); } }
        RectTransform LearningRT { get { return learningCanvas.GetComponent<RectTransform>(); } }
        RectTransform ResearchRT { get { return researchCanvas.GetComponent<RectTransform>(); } }
        RectTransform SettingsRT { get { return settingsCanvas.GetComponent<RectTransform>(); } }
        RectTransform ReturnRT { get { return returnCanvas.GetComponent<RectTransform>(); } }

        enum State { Hidden, Splash, Learning, Research, Settings };
        private State state;
        void Reveal()
        {
            Assert.IsTrue(state == State.Hidden);

            TweenY(splashCanvas.GetComponent<RectTransform>(), -1000, 0);
            splashCanvas.enabled = true;

            state = State.Splash;
        }
        void Reset()
        {
            Assert.IsTrue(state == State.Settings, "should only be able to reset from settings");

            TweenY(SettingsRT, 0, -1000, ()=>settingsCanvas.enabled=false);
            TweenY(ReturnRT, -60, 60);

            // destroy levels as Start() reinstantiates them (unnecessary I know)
            foreach (Transform child in learningGrid.transform) {
                Destroy(child.gameObject);
            }
            foreach (Transform child in researchList.transform) {
                Destroy(child.gameObject);
            }
            state = State.Hidden;

            // GameObjects not destroyed until next frame, so delay is needed
            StartCoroutine(WaitThenStart());
            IEnumerator WaitThenStart()
            {
                yield return null;
                Start();
            }
        }
        public void ShowSplash()
        {
            Assert.IsTrue(state==State.Learning || state==State.Research || state==State.Settings);

            ClearTweens();
            TweenY(SplashRT, -1000, 0);
            TweenY(ReturnRT, -60, 60);
            splashCanvas.enabled = true;

            if (state == State.Learning) {
                TweenY(LearningRT, 0, -1000, ()=>learningCanvas.enabled=false);
            } else if (state == State.Research) {
                TweenY(ResearchRT, 0, -1000, ()=>researchCanvas.enabled=false);
            } else if (state == State.Settings) {
                TweenY(SettingsRT, 0, -1000, ()=>settingsCanvas.enabled=false);
            }
            SetHelp(researchWorld.interactable? splashResearchText : splashLearningText);

            state = State.Splash;
        }
        public void ShowLearningWorld()
        {
            Assert.IsTrue(state == State.Splash);
            
            ClearTweens();
            TweenY(SplashRT, 0, -1000, ()=>splashCanvas.enabled=false);
            TweenY(LearningRT, -1000, 0);
            TweenY(ReturnRT, 60, -60);

            learningCanvas.enabled = true;
            SetHelp(levelsLearningText);

            state = State.Learning;
        }
        public void ShowResearchWorld()
        {
            Assert.IsTrue(state == State.Splash);

            ClearTweens();
            TweenY(SplashRT, 0, -1000, ()=>splashCanvas.enabled=false);
            TweenY(ResearchRT, -1000, 0);
            TweenY(ReturnRT, 60, -60);

            researchCanvas.enabled = true;
            SetHelp(levelsResearchText);

            state = State.Research;
            SetCurrentResearchLeaderboard();
        }

        public void ShowSettings()
        {
            Assert.IsTrue(state == State.Splash);

            ClearTweens();
            TweenY(SplashRT, 0, -1000, ()=>splashCanvas.enabled=false);
            TweenY(SettingsRT, -1000, 0);
            TweenY(ReturnRT, 60, -60);

            settingsCanvas.enabled = true;
            SetHelp(settingsText);

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
        [SerializeField] float tweenDuration;
        private void TweenY(RectTransform toMove, float startY, float endY, Action OnFinish=null)
        {
            IEnumerator TweenYRoutine()
            {
                Vector3 startPos = toMove.anchoredPosition;
                float startTime = Time.time;
                while (Time.time < startTime+tweenDuration)
                {
                    float t = Tweens.QuadraticInOut((Time.time-startTime) / tweenDuration);
                    float y = Mathf.Lerp(startY, endY, t);
                    toMove.anchoredPosition = new Vector3(startPos.x, y, startPos.z);
                    yield return null;
                }
                toMove.anchoredPosition = new Vector3(startPos.x, endY, startPos.z);
                OnFinish?.Invoke();
            }
            var routine = TweenYRoutine();
            StartCoroutine(routine);
            navigationRoutines.Add(routine);
        }
    }
}
