using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
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
                    gameManager = new GameObject("Game Manager").AddComponent<GameManager>();
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
            Screen.SetResolution(576, 1024, false);
            Screen.fullScreen = true;
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

        [SerializeField] UI.Level LevelPrefabDevOnly; // purely for development
        public UI.Level DefaultLevel { get { return Instantiate(LevelPrefabDevOnly); } }

        public UI.Level PlayedLevel { get; private set; }
        public void PlayLevel(UI.Level level)
        {
            PlayedLevel = level;
            GameManager.Instance.UnloadScene("Menu");
            GameManager.Instance.LoadScene("Play");
        }
        public void SavePlayedLevel(int numStars)
        {
            if (numStars < 0 || numStars > 3)
                throw new Exception("cannot pass with less than 0 or more than 3 stars");

            if (PlayedLevel == null)
            {
                print("HOW DID YOU GET HERE");
                return;
            }

            if (numStars > PlayedLevel.Details.numStars)
                PlayedLevel.Details.numStars = numStars;

            PlayedLevel.SaveToFile();
        }
        public void SaveFoodWebToCsv(
            List<int> speciesIdxs, List<int> randomSeeds,
            List<float> sizes, List<float> greeds,
            List<int> resources, List<int> consumers,
            Dictionary<string, double> parameterisation)
        {
            print("TODO: save a .csv to record the foodweb");
        }
        public void ReturnToMenu()
        {
            GameManager.Instance.UnloadScene("Play");
            GameManager.Instance.LoadScene("Menu");
        }








        // [SerializeField] List<Mesh> numbers;
        // public Mesh GetNumberMesh(int number)
        // {
        //     if (number < 0 || number > 9)
        //         throw new Exception("number out of range");

        //     return numbers[number];
        // }

        // int landscapeNumber = 0;
		// [SerializeField] List<GameObject> landscapes;
        // public void SwitchLandscape(bool increment)
        // {
        //     if (increment)
        //         landscapeNumber += 1;
        //     else
        //         landscapeNumber -= 1;

        //     if (landscapeNumber < 0)
        //         landscapeNumber = landscapes.Count - 1;
        //     if (landscapeNumber >= landscapes.Count)
        //         landscapeNumber = 0;
        // }
        // public GameObject SelectedLandscape { get { return landscapes[landscapeNumber]; } }
    }
}