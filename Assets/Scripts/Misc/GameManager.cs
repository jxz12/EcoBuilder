using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EcoBuilder
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager gameManager;
        public static GameManager Instance {
            get {
                if (gameManager == null)
                {
                    Debug.LogError("No active GameManager");
                    // gameManager = new GameObject("Game Manager").AddComponent<GameManager>();
                    // remove for build
                }
                return gameManager;
            }
        }
        void Awake()
        {
            if (gameManager == null)
                gameManager = this;
            else if (gameManager != this)
            {
                Debug.LogError("Multiple GameManagers, destroying this one");
                Destroy(gameObject); // this means that there can only ever be one GameObject of this type
            }
            for (int i = 0; i < numFrames; i++)
                timeDeltas.Enqueue(0);
        }
        void Start()
        {
            // Screen.SetResolution(576, 1024, false);
            // #if !UNITY_WEBGL
            //     Screen.fullScreen = true;
            // #endif

            if (SceneManager.sceneCount == 1)
                LoadScene("Menu");
        }
        

        public void LoadScene(string sceneName) {
            StartCoroutine(LoadSceneThenSetActive(sceneName));
        }
        public void UnloadScene(string sceneName) {
            SceneManager.UnloadSceneAsync(sceneName);
        }

        // TODO: loading bars with these
        //[SerializeField] UnityEvent startLoadEvent, endLoadEvent;
        //[Serializable] public class FloatEvent : UnityEvent<float>{}
        //[SerializeField] FloatEvent progressEvent;
        private IEnumerator LoadSceneThenSetActive(string sceneName)
        {
            //startLoadEvent.Invoke();
            var loading = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!loading.isDone)
            {
                //progressEvent.Invoke(loading.progress);
                yield return null;
            }
            var scene = SceneManager.GetSceneByName(sceneName);
            SceneManager.SetActiveScene(scene);
            //endLoadEvent.Invoke();
        }


        [SerializeField] Text fpsText;
        readonly int numFrames = 10;        
        Queue<float> timeDeltas = new Queue<float>();
        float totalTime = 0;
        private void Update()
        {
            float oldDelta = timeDeltas.Dequeue();
            float newDelta = Time.deltaTime;

            totalTime -= oldDelta;
            totalTime += newDelta;
            fpsText.text = (1 / (totalTime / numFrames)).ToString("0");

            timeDeltas.Enqueue(newDelta);
        }


        [SerializeField] RectTransform overlayParent;
        public RectTransform Overlay { get { return overlayParent; } }

        [SerializeField] UI.Level levelPrefab;
        public UI.Level LoadLevel(string path=null)
        {
            var nextLevel = Instantiate(levelPrefab);
            bool successful = nextLevel.LoadFromFile(path);
            if (successful)
            {
                return nextLevel;
            }
            else
            {
                Destroy(nextLevel);
                return null;
            }
        }

        public UI.Level PlayedLevel { get; private set; }
        public void PlayLevel(UI.Level level)
        {
            if (PlayedLevel != null)
            {
                if (PlayedLevel != level)
                {
                    Destroy(PlayedLevel.gameObject);
                    PlayedLevel = level;
                }
                else
                {
                    // to make sure it stays alive
                    PlayedLevel.transform.SetParent(Overlay.transform, true);
                }
            }
            else
            {
                PlayedLevel = level;
            }

            UnloadScene(SceneManager.GetActiveScene().name);
            LoadScene("Play");
        }



        int age, gender, education;
        public void SetAge(int age)
        {
            this.age = age;
        }
        public void SetGender(int gender)
        {
            this.gender = gender;
        }
        public void SetEducation(int education)
        {
            this.education = education;
        }
        // TODO: username for leaderboards

        public void ReturnToMenu()
        {
            if (PlayedLevel != null)
            {
                Destroy(PlayedLevel.gameObject);
                PlayedLevel = null;
            }
            GameManager.Instance.UnloadScene("Play");
            GameManager.Instance.LoadScene("Menu");
        }
    }
}