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
            print("TODO: disable full screen for desktop?!");
#if !UNITY_WEBGL
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
                if (playedLevel == null)
                {
                    playedLevel = Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<Level>("Assets/Prefabs/Levels/Level.prefab"));
                    playedLevel.Play(); // should cause replay behaviour
                    playedLevel.Unlock();
                }
#else
                Assert.IsNotNull(playedLevel, "no level being played");
#endif
                return playedLevel.Details;
            }
        }
        public void PlayLevel(Level toPlay)
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
        public void MakePlayedLevelFinishable()
        {
            playedLevel.ShowFinishFlag();
        }
        public event Action OnPlayedLevelFinished;
        public void FinishLevel(Level toFinish)
        {
            Assert.IsTrue(playedLevel == toFinish, "not playing level to be initialised");
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

        [SerializeField] UnityEvent OnSceneUnloaded, OnSceneLoaded;
        [Serializable] public class MyFloatEvent : UnityEvent<float> {}
        [SerializeField] MyFloatEvent OnLoadingProgress;
        [Serializable] public class MyStringEvent : UnityEvent<string> {}
        public MyStringEvent OnLoaded;
        private IEnumerator UnloadSceneThenLoad(string toUnload, string toLoad)
        {
            OnSceneUnloaded.Invoke();
            if (toUnload != null)
            {
                var unloading = SceneManager.UnloadSceneAsync(toUnload, UnloadSceneOptions.None);
                while (!unloading.isDone)
                {
                    OnLoadingProgress?.Invoke(unloading.progress/2);
                    yield return null;
                }
            }
            if (toLoad != null)
            {
                var loading = SceneManager.LoadSceneAsync(toLoad, LoadSceneMode.Additive);
                while (!loading.isDone)
                {
                    OnLoadingProgress?.Invoke(.5f + loading.progress/2);
                    yield return null;
                }
                OnLoaded?.Invoke(toLoad);
            }
            OnSceneLoaded?.Invoke();
        }

        public void ReturnToMenu()
        {
            if (playedLevel != null) {
                playedLevel = null; // level destroys itself so no need to do it here
            }
            StartCoroutine(UnloadSceneThenLoad("Play", "Menu"));
        }


        ///////////////////////////
        // showing help messages //
        ///////////////////////////

        [SerializeField] UI.Help helpText;
        public UI.Help HelpText { get { return helpText; } }
    }
}