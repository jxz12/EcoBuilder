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
        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        void OnDestroy()
        {
            print("TODO: send data if possible? and also on every level finish?");
        }
            


        ////////////////////////////////////////////////
        // functions to persist levels through scenes //
        ////////////////////////////////////////////////

        private Level playedLevel;
        public LevelDetails PlayedLevelDetails {
            get {
#if UNITY_EDITOR
                // for convenience in editor
                if (playedLevel == null)
                {
                    playedLevel = Instantiate(Level.DefaultPrefab);
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
                    playedLevel.OnFinished += OnPlayedLevelFinished;
                } else {
                    // replay level so no need to destroy
                }
                StartCoroutine(UnloadSceneThenLoad("Play", "Play"));
            }
            else
            {
                // play from menu
                playedLevel = toPlay;
                playedLevel.OnFinished += OnPlayedLevelFinished;
                StartCoroutine(UnloadSceneThenLoad("Menu", "Play"));
            }
        }
        public event Action OnPlayedLevelFinished; // listened by playmanager

        public void BeginPlayedLevel() // called by playmanager
        {
            playedLevel.BeginPlay();
        }
        public void MakePlayedLevelFinishable() // called by playmanager
        {
            playedLevel.ShowFinishFlag();
        }


        ///////////////////
        // scene loading //
        ///////////////////

        [SerializeField] UI.LoadingBar loadingBar;
        public event Action<string> OnSceneLoaded;
        private IEnumerator UnloadSceneThenLoad(string toUnload, string toLoad)
        {
#if UNITY_EDITOR
            yield return new WaitForSeconds(1);
#endif
            loadingBar.Show(true);
            if (toUnload != null)
            {
                var unloading = SceneManager.UnloadSceneAsync(toUnload, UnloadSceneOptions.None);
                while (!unloading.isDone)
                {
                    loadingBar.SetProgress(unloading.progress/4f);
                    yield return null;
                }
            }
            loadingBar.SetProgress(.25f);
            if (toLoad != null)
            {
                var loading = SceneManager.LoadSceneAsync(toLoad, LoadSceneMode.Additive);
                while (!loading.isDone)
                {
                    loadingBar.SetProgress(.25f + .75f*loading.progress);
                    yield return null;
                }
            }
            loadingBar.Show(true);
            OnSceneLoaded?.Invoke(toLoad);
        }



        // for levels to attach to in order to persist across scenes
        [SerializeField] RectTransform cardAnchor, playAnchor;
        public RectTransform CardAnchor { get { return cardAnchor; } }
        public RectTransform PlayAnchor { get { return playAnchor; } }
        [SerializeField] Canvas tutorialCanvas;
        public Canvas TutCanvas { get { return tutorialCanvas; } }

        public void ReturnToMenu()
        {
            if (playedLevel != null) {
                playedLevel = null; // level destroys itself so probably no need to do it here
            }
            StartCoroutine(UnloadSceneThenLoad("Play", "Menu"));
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
            earth.TweenToRestPositionFromNextFrame(2);
        }


        ///////////////////////////
        // showing help messages //
        ///////////////////////////

        [SerializeField] UI.Help helpText;
        public UI.Help HelpText { get { return helpText; } }

        [SerializeField] UI.ReportCard report;
        public void ShowResultsScreen(int nStars, int score, string matrix, string actions)
        {
            int idx = playedLevel.Details.Idx;

            int prevScore = GetHighScoreLocal(idx);
            SaveHighScoreLocal(idx, score);

            if (playedLevel.Details.Metric != LevelDetails.ScoreMetric.None)
            {
                int worldAvg = GetLeaderboardMedian(idx);
                report.ShowResults(nStars, score, prevScore, worldAvg);
            }

            if (LoggedIn) {
                SavePlaythroughRemote(idx, score, matrix, actions);
            }

            var nextLevel = Instantiate(playedLevel.NextLevelPrefab);
            nextLevel.transform.SetParent(report.NextLevelAnchor);
        }
    }
}