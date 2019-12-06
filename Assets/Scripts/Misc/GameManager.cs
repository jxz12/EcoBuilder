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
            InitPlayer();
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
        public IEnumerable<Levels.Level> GetLevelPrefabs()
        {
            return levelPrefabs;
        }

        [SerializeField] UI.Earth earth;
        [SerializeField] RectTransform cardParent, playParent, navParent, tutParent;
        public RectTransform CardParent { get { return cardParent; } }
        public RectTransform PlayParent { get { return playParent; } }
        public RectTransform NavParent { get { return navParent; } }
        public RectTransform TutParent { get { return tutParent; } }

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

        // This whole structure is necessary because you cannot change prefabs from script when compiled
        // Ideally we would keep this inside Levels.Level, but that is not possible
        public int GetLevelHighScore(int levelIdx)
        {
            return player.highScores[levelIdx];
        }

        public void SavePlayedLevelHighScore(int score)
        {
            int idx = PlayedLevel.Details.idx;
            if (score > player.highScores[idx])
                player.highScores[idx] = score;

            int nextIdx = PlayedLevel.Details.idx + 1;
            if (nextIdx >= player.highScores.Count)
                return;

            if (player.highScores[nextIdx] < 0)
                player.highScores[nextIdx] = 0;
            // TODO: animation

            SavePlayerDetailsLocal();
        }


        ///////////////////
        // scene loading //
        ///////////////////

        // TODO: loading screen or loading events here
        [SerializeField] UnityEvent OnSceneUnloaded, OnSceneLoaded;
        [Serializable] public class MyFloatEvent : UnityEvent<float> {}
        [SerializeField] MyFloatEvent OnLoadingProgress;
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
        public event Action<string> OnLoaded;

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