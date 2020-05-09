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
            InitPlayer();
            earth = Instantiate(earthPrefab);
        }

        void Start()
        {
#if !UNITY_EDITOR
            StartCoroutine(UnloadSceneThenLoad(null, "Menu"));
#endif
#if UNITY_WEBGL
            print($"TODO: set this to the dependent on screen size for webgl using {Screen.currentResolution}");
            // Screen.SetResolution(576, 1024, false);
            // Screen.fullScreen = true;
#endif
            SendUnsentPost();
        }
        void OnDestroy()
        {
            SendUnsentPost();
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
            yield return new WaitForSeconds(.5f);
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
            OnLoaded?.Invoke();
            SendUnsentPost();
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
            earth.ResetParent(); 
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
        public void ReloadLevelScene(Level toPlay)
        {
            confirmation.GiveChoice(()=>LoadLevelScene(toPlay), "Are you sure you want to restart the level?");
        }




        // for levels to attach to in order to persist across scenes
        [SerializeField] RectTransform cardAnchor, playAnchor;
        public RectTransform CardAnchor { get { return cardAnchor; } }
        public RectTransform PlayAnchor { get { return playAnchor; } }
        [SerializeField] Canvas tutorialCanvas;
        public Canvas TutCanvas { get { return tutorialCanvas; } }

        [SerializeField] UI.Confirmation confirmation;
        public void ReturnToMenu(Action OnConfirm, Action OnMenuLoaded)
        {
            confirmation.GiveChoice(BackToMenu, "Are you sure you want to return to the main menu?");
            void BackToMenu()
            {
                OnConfirm.Invoke(); 
                HelpText.ResetMenuPosition(false);
                earth.ResetParent(); 
                StartCoroutine(UnloadSceneThenLoad("Play", "Menu", ()=>{ OnMenuLoaded?.Invoke(); report.HideIfShowing(); earth.TweenToRestPositionFromNextFrame(2); })); 
                // wait until next frame to avoid the frame spike caused by Awake and Start()
            }
        }

        public void Quit()
        {
            confirmation.GiveChoice(CloseGameFully, "Are you sure you want to quit?");
        }
        public void CloseGameFully()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        [SerializeField] Planet earthPrefab;
        Planet earth;
        public GameObject TakePlanet()
        {
            Assert.IsNotNull(earth, "earth was destroyed :(");
            earth.TweenToRestPositionFromNextFrame(2);
            return earth.gameObject;
        }

        ///////////////////////////
        // showing help messages //
        ///////////////////////////

        [SerializeField] UI.Help helpText;
        public UI.Help HelpText { get { return helpText; } }

        [SerializeField] UI.ReportCard report;

        // called by playmanager
        public void SetResultsScreen(int nStars, long score, string rank, string matrix, string actions)
        {
            int idx = playedLevel.Details.Idx;

            long? prevScore = GetHighScoreLocal(idx);
            SaveHighScoreLocal(idx, score);

            if (LoggedIn) {
                SavePlaythroughRemote(idx, score, matrix, actions);
            }

            if (playedLevel.NextLevelPrefab != null) {
                report.GiveNavigation(playedLevel, Instantiate(playedLevel.NextLevelPrefab));
            } else {
                report.GiveNavigation(playedLevel, null);
            }

            if (playedLevel.Details.Metric == LevelDetails.ScoreMetric.None) {
                report.SetResults(null, null, null, null, null);
            } else if (playedLevel.Details.ResearchMode) {
                report.SetResults(null, score, prevScore, rank, GetCachedMedian(idx));
            } else {
                report.SetResults(nStars, score, prevScore, null, GetCachedMedian(idx));
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