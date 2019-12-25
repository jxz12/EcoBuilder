using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

namespace EcoBuilder
{
    public partial class GameManager : MonoBehaviour
    {
        // singleton pattern
        private static GameManager gameManager;
        public static GameManager Instance {
            get {
                if (gameManager == null)
                {
                    throw new Exception("No active GameManager");
                }
                return gameManager;
            }
        }
        void Awake()
        {
            if (gameManager == null)
                gameManager = this;
            else if (gameManager != this)
                throw new Exception("More than one GameManager in scene");

            InitPlayer();
            if (SceneManager.sceneCount == 1) // on startup
            {
                StartCoroutine(UnloadSceneThenLoad(null, "Menu"));
            }
        }

        void Start()
        {
            // TODO: set this to the size of the screen for webgl
            // Screen.SetResolution(576, 1024, false);
            // #if !UNITY_WEBGL
            //     Screen.fullScreen = true;
            // #endif
        }
        public void Quit()
        {
            // TODO: send data if possible? and also on every level finish!
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }


        ////////////////////////////////////
        // storing information for levels //
        ////////////////////////////////////

        [SerializeField] List<Levels.Level> levelPrefabs; // each level is a prefab
        [SerializeField] List<Levels.Level> researchLevelPrefabs;
        public IEnumerable<Levels.Level> GetLevelPrefabs()
        {
            return levelPrefabs;
        }

        public Levels.Level PlayedLevel { get; private set; }
        public void PlayLevel(Levels.Level toPlay)
        {
            if (PlayedLevel != null)
            {
                if (PlayedLevel.Details.idx != toPlay.Details.idx)
                {
                    Destroy(PlayedLevel.gameObject);
                    PlayedLevel = toPlay;
                }
                else
                {
                    // replay level
                }
                // UnloadSceneThenLoadAnother("Play", "Play");
                StartCoroutine(UnloadSceneThenLoad("Play", "Play"));
            }
            else
            {
                // play from menu
                PlayedLevel = toPlay;
                // UnloadSceneThenLoadAnother("Menu", "Play");
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

        // TODO: loading screen or loading events here
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
            yield return new WaitForSeconds(1);
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
            if (PlayedLevel != null)
            {
                // Destroy(PlayedLevel.gameObject);
                PlayedLevel = null;
            }
            StartCoroutine(UnloadSceneThenLoad("Play", "Menu"));
        }
    }
}