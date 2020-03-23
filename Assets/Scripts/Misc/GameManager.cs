using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

namespace EcoBuilder
{
    public partial class GameManager : MonoBehaviour
    {
        // singleton pattern
        private static GameManager _gameManager;
        public static GameManager Instance {
            get {
                if (_gameManager == null)
                {
                    _gameManager = FindObjectOfType<GameManager>();
                    Assert.IsNotNull(_gameManager, "no GameManager in any scene");
                }
                return _gameManager;
            }
        }
        void Awake()
        {
            Assert.IsFalse(_gameManager!=null && _gameManager!=this, "more than one GameManager in scene");
            if (_gameManager == null) {
                _gameManager = this;
            }
            if (SceneManager.sceneCount == 1) { // on startup
                StartCoroutine(UnloadSceneThenLoad(null, "Menu"));
            }
            InitPlayer();
        }

        void Start()
        {
            print("TODO: set this to the dependent on screen size for webgl");
#if UNITY_WEBGL
            // Screen.SetResolution(576, 1024, false);
            // Screen.fullScreen = true;
#endif
        }
        void OnDestroy()
        {
            print("TODO: send data if possible? and also on every level finish?");
        }
            


        ///////////////////
        // scene loading //
        ///////////////////

        [SerializeField] UI.LoadingBar loadingBar;
        private IEnumerator UnloadSceneThenLoad(string toUnload, string toLoad, Action OnLoaded=null)
        {
            loadingBar.Show(true);
            if (toUnload != null)
            {
                var unloading = SceneManager.UnloadSceneAsync(toUnload, UnloadSceneOptions.None);
                while (unloading!=null && !unloading.isDone)
                {
                    loadingBar.SetProgress(unloading.progress * .333f);
                    yield return null;
                }
            }
            loadingBar.SetProgress(.333f);
#if UNITY_EDITOR
            yield return new WaitForSeconds(1);
#endif
            if (toLoad != null)
            {
                var loading = SceneManager.LoadSceneAsync(toLoad, LoadSceneMode.Additive);
                while (loading!=null && !loading.isDone)
                {
                    loadingBar.SetProgress(.333f + .667f*loading.progress);
                    yield return null;
                }
            }
            loadingBar.Show(false);
            // OnSceneLoaded?.Invoke(toLoad);
            OnLoaded?.Invoke();
        }


        ////////////////////////////////////////////////
        // functions to persist levels through scenes //
        ////////////////////////////////////////////////

        private Level playedLevel;
        public event Action OnPlayedLevelFinished; // listened by playmanager
        public void BeginPlayedLevel() // called by playmanager
        {
            playedLevel.BeginPlay();
        }
        public void MakePlayedLevelFinishable() // called by playmanager
        {
            playedLevel.ShowFinishFlag();
        }

#if UNITY_EDITOR
        [SerializeField] Level defaultLevelPrefab;
#endif
        public LevelDetails PlayedLevelDetails {
            get {
#if UNITY_EDITOR
                // for convenience in editor
                if (playedLevel == null)
                {
                    playedLevel = Instantiate(defaultLevelPrefab);
                    playedLevel.OnFinished += ()=>OnPlayedLevelFinished.Invoke();

                    playedLevel.Unlock();
                }
#endif
                Assert.IsNotNull(playedLevel, "no level being played");
                return playedLevel.Details;
            }
        }

        public void LoadLevelScene(Level toPlay)
        {
            if (playedLevel != null) // if playing already
            {
                if (playedLevel != toPlay)
                {
                    Destroy(playedLevel.gameObject);
                    playedLevel = toPlay;
                    playedLevel.OnFinished += ()=>OnPlayedLevelFinished.Invoke();
                } else {
                    // replay level so no need to destroy
                }
                StartCoroutine(UnloadSceneThenLoad("Play", "Play"));
            }
            else
            {
                // play from menu
                playedLevel = toPlay;
                playedLevel.OnFinished += ()=>OnPlayedLevelFinished.Invoke();
                StartCoroutine(UnloadSceneThenLoad("Menu", "Play"));
            }
            report.HideIfShowing();
        }



        // for levels to attach to in order to persist across scenes
        [SerializeField] RectTransform cardAnchor, playAnchor;
        public RectTransform CardAnchor { get { return cardAnchor; } }
        public RectTransform PlayAnchor { get { return playAnchor; } }
        [SerializeField] Canvas tutorialCanvas;
        public Canvas TutCanvas { get { return tutorialCanvas; } }

        [SerializeField] UI.Confirmation confirmation;
        public void ReturnToMenu()
        {
            confirmation.GiveChoice(BackToMenu, "return to the main menu");
        }
        private void BackToMenu()
        {
            HelpText.Showing = false;

            // make level uninteractable
            var group = playedLevel.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;

            StartCoroutine(UnloadSceneThenLoad("Play", "Menu", ()=>{ playedLevel.LeaveThenDestroyFromNextFrame(); report.HideIfShowing(); earth.TweenToRestPositionFromNextFrame(2); })); 
            // wait until next frame to avoid the frame spike caused by Awake and Start()
        }

        public void Quit()
        {
            confirmation.GiveChoice(CloseGameFully, "quit");
        }
        public void CloseGameFully()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        public void LogOut(Action Reset)
        {
            Assert.IsTrue(LoggedIn);
            confirmation.GiveChoice(()=>{ DeletePlayerDetailsLocal(); Reset(); }, "Are you sure you want to log out? Any scores you achieve when not logged in will not be saved to this account.");
        }
        public void DeleteAccount(Action Reset)
        {
            Assert.IsTrue(LoggedIn);
            confirmation.GiveChoiceAndWait(()=> DeleteAccountRemote((b,s)=>{ confirmation.FinishWaiting(Reset, b, s); if (b) DeletePlayerDetailsLocal(); }), "Are you sure you want to delete your account? Any high scores you have achieved will be lost.", "Deleting account...");
        }

        [SerializeField] Planet earth;
        Transform earthParent;
        public GameObject TakePlanet()
        {
            Assert.IsNotNull(earth, "earth was destroyed :(");
            earthParent = earth.transform.parent;
            earth.TweenToRestPositionFromNextFrame(2);
            return earth.gameObject;
        }
        public void ReturnPlanet()
        {
            Assert.IsNotNull(earth, "earth was destroyed :(");
            earth.transform.SetParent(earthParent, true);
            // TweenToRestPosition called in ReturnToMenu() above
        }


        ///////////////////////////
        // showing help messages //
        ///////////////////////////

        [SerializeField] UI.Help helpText;
        public UI.Help HelpText { get { return helpText; } }

        [SerializeField] UI.ReportCard report;

        // called by playmanager
        public void SetResultsScreen(int nStars, long score, string matrix, string actions)
        {
            int idx = playedLevel.Details.Idx;

            long prevScore = GetHighScoreLocal(idx);
            SaveHighScoreLocal(idx, score);

            if (playedLevel.Details.Metric != LevelDetails.ScoreMetric.None)
            {
                long worldAvg = GetLeaderboardMedian(idx);
                report.SetResults(nStars, score, prevScore, worldAvg);
            }
            else
            {
                report.SetResults(3, 0, 0, 0);
            }

            if (LoggedIn) {
                SavePlaythroughRemote(idx, score, matrix, actions);
            }

            if (playedLevel.NextLevelPrefab != null) {
                report.GiveNavigation(playedLevel, Instantiate(playedLevel.NextLevelPrefab));
            } else {
                report.GiveNavigation(playedLevel, null);
            }
        }
        // called by level
        public void ShowResultsScreen()
        {
            report.ShowResults();
            HelpText.Showing = false;
        }
    }
}