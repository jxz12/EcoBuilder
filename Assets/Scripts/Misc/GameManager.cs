using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
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
                Assert.IsNotNull(_gameManager, "No active GameManager");
                return _gameManager;
            }
        }
        void Awake()
        {
            Assert.IsNull(_gameManager, "More than one GameManager in scene");
            _gameManager = this;
            if (SceneManager.sceneCount == 1) { // on startup
                StartCoroutine(UnloadSceneThenLoad(null, "Menu"));
            }
            InitPlayer();
        }

        void Start()
        {
            print("TODO: set this to the dependent on screen size for webgl");
            print("TODO: disable full screen for desktop?!");
// #if !UNITY_WEBGL
//             Screen.SetResolution(576, 1024, false);
//             Screen.fullScreen = true;
// #endif
#if UNITY_EDITOR
            for (int i=0; i<SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name == "Play")
                {
                    PlayedLevel = Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<Level>("Assets/Prefabs/Levels/Level.prefab"));
                    PlayedLevel.Play(); // should cause replay behaviour
                    break;
                }
            }
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

        public Level PlayedLevel { get; private set; } = null;
        public void LoadLevelScene(Level toPlay)
        {
            if (PlayedLevel != null) // if playing already
            {
                if (PlayedLevel != toPlay)
                {
                    Destroy(PlayedLevel.gameObject);
                    PlayedLevel = toPlay;
                } else {
                    // replay level so no need to destroy
                }
                StartCoroutine(UnloadSceneThenLoad("Play", "Play"));
            }
            else
            {
                // play from menu
                PlayedLevel = toPlay;
                StartCoroutine(UnloadSceneThenLoad("Menu", "Play"));
            }
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
            if (PlayedLevel != null) {
                PlayedLevel = null; // level destroys itself so no need to do it here
            }
            StartCoroutine(UnloadSceneThenLoad("Play", "Menu"));
        }


        ///////////////////////////
        // showing help messages

        [SerializeField] UI.Help helpText;
        public UI.Help HelpText { get { return helpText; } }
    }
}