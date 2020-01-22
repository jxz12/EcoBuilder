using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
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
                if (gameManager == null) {
                    throw new Exception("No active GameManager");
                }
                return gameManager;
            }
        }
        void Awake()
        {
            if (gameManager == null) {
                gameManager = this;
            } else if (gameManager != this) {
                throw new Exception("More than one GameManager in scene");
            }
            if (SceneManager.sceneCount == 1) { // on startup
                StartCoroutine(UnloadSceneThenLoad(null, "Menu"));
            }
        }

        void Start()
        {
            InitPlayer();
            // TODO: set this to the size of the screen for webgl
// #if !UNITY_WEBGL
//             Screen.SetResolution(576, 1024, false);
//             Screen.fullScreen = true;
// #endif
#if UNITY_EDITOR
            // if (SceneManager.sceneCount >= 2)
            // {
            //     PlayedLevel = Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<Levels.Level>("Assets/Prefabs/Levels/Level.prefab"));
            //     PlayedLevel.Play();
            // }
#endif
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


        ////////////////////////////////////////////////
        // functions to persist levels through scenes //
        ////////////////////////////////////////////////

        public Levels.Level PlayedLevel { get; private set; } = null;
        public void LoadLevelScene(Levels.Level toPlay)
        {
            if (PlayedLevel != null) // if in play mode
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
            print("TODO: loading message!");
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
        public void ShowHelpText(float delay, string message)
        {
            helpText.DelayThenShow(delay, message);
        }
        public void HideHelpText()
        {
            helpText.Show(false);
        }
    }
}