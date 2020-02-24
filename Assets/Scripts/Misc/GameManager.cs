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
                } else {
                    // replay level so no need to destroy
                }
                StartCoroutine(UnloadSceneThenLoad("Play", "Play"));
            }
            else
            {
                // play from menu
                playedLevel = toPlay;
                StartCoroutine(UnloadSceneThenLoad("Menu", "Play"));
            }
        }
        public void BeginPlayedLevel()
        {
            playedLevel.BeginPlay();
        }
        public void MakePlayedLevelFinishable()
        {
            playedLevel.ShowFinishFlag();
        }
        public event Action OnPlayedLevelFinished;
        public void FinishLevel(Level toFinish)
        {
            Assert.IsTrue(playedLevel == toFinish, "not playing level to be finished");
            OnPlayedLevelFinished.Invoke();
        }

        // for levels to attach to
        [SerializeField] RectTransform cardParent, playParent, navParent, tutParent;
        public RectTransform CardParent { get { return cardParent; } }
        public RectTransform PlayParent { get { return playParent; } }
        public RectTransform NavParent { get { return navParent; } }
        public RectTransform TutParent { get { return tutParent; } }

        ///////////////////
        // scene loading //
        ///////////////////

        [Serializable] public class MyBoolEvent : UnityEvent<bool> {}
        [SerializeField] MyBoolEvent OnSceneLoading;
        [Serializable] public class MyFloatEvent : UnityEvent<float> {}
        [SerializeField] MyFloatEvent OnLoadingProgressed;
        public event Action<string> OnSceneLoaded;
        private IEnumerator UnloadSceneThenLoad(string toUnload, string toLoad)
        {
            OnSceneLoading.Invoke(true);
            if (toUnload != null)
            {
                var unloading = SceneManager.UnloadSceneAsync(toUnload, UnloadSceneOptions.None);
                while (!unloading.isDone)
                {
                    OnLoadingProgressed?.Invoke(unloading.progress/2);
                    yield return null;
                }
            }
            OnLoadingProgressed?.Invoke(.5f);
#if UNITY_EDITOR
            yield return new WaitForSeconds(1);
#endif
            if (toLoad != null)
            {
                var loading = SceneManager.LoadSceneAsync(toLoad, LoadSceneMode.Additive);
                while (!loading.isDone)
                {
                    OnLoadingProgressed?.Invoke(.5f + loading.progress/2);
                    yield return null;
                }
            }
            OnSceneLoading.Invoke(false);
            OnSceneLoaded?.Invoke(toLoad);
        }

        public void ReturnToMenu()
        {
            if (playedLevel != null) {
                playedLevel = null; // level destroys itself so no need to do it here
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
        public void ShowResults(int prevScore, int globalMedian)
        {
            report.ShowResults(HighestStars, HighestScore, prevScore, globalMedian);
        }
    }
}