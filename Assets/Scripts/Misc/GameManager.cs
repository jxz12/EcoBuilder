using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.UI;

// for load/save progress
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

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
            LoadProgress();

            if (SceneManager.sceneCount == 1)
                LoadScene("Menu");
        }
        void OnApplicationQuit()
        {
                Progress = new List<int>() {0};
            SaveProgress();
        }
        

        public void LoadScene(string sceneName) {
            StartCoroutine(LoadSceneThenSetActive(sceneName));
        }
        public void UnloadScene(string sceneName) {
            SceneManager.UnloadSceneAsync(sceneName);
        }

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


        // -1 is locked, 0,1,2,3 unlocked plus number of stars
        public List<int> Progress { get; private set; }
        void SaveProgress()
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(Application.persistentDataPath + "/levels.gd");
            bf.Serialize(file, Progress);
            file.Close();
        }
        void LoadProgress()
        {
            if (File.Exists(Application.persistentDataPath + "/levels.gd"))
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(Application.persistentDataPath + "/levels.gd", FileMode.Open);
                Progress = (List<int>)bf.Deserialize(file);
                file.Close();
            }
            else
            {
                Progress = new List<int>();
                Progress.Add(0); // if no save file, only unlock first level, no stars
            }
        }

        /*
        graph constraints:
            min/max chain length
            must contain a cycle of length n
            omnivory (coherence)
            min/max number of basals or apex predators

        model constraints:
            min/max flux
            size/greediness (e.g. only big species)
        */
        [SerializeField] Menu.LevelCard levelCard;
        public int LevelNumber { get; private set; } = 0;
        public int NumProducers { get; private set; } = 10;
        public int NumConsumers { get; private set; } = 10;
        public void ShowLevelCard(int number, string title, string description,
            int numProducers, int numConsumers,
            int minLoop=0, int maxLoop=0, int minChain=0, int maxChain=0, float minOmnivory=0, float maxOmnivory=0 )
        {
            LevelNumber = number;
            NumProducers = numProducers;
            NumConsumers = numConsumers;
            levelCard.Show(title, description, numProducers, numConsumers);
        }
        public void ShowLevelCard()
        {
            levelCard.Show();
        }
        public void HideLevelCard()
        {
            levelCard.Hide();
        }
        public void PlayGame()
        {
            GameManager.Instance.UnloadScene("Menu");
            GameManager.Instance.LoadScene("Play");
        }
        public void EndGame(int numStars)
        {
            if (numStars < 0 || numStars > 3)
                throw new Exception("cannot pass with less than 1 or more than 3 stars");

            if (LevelNumber >= Progress.Count)
            {
                for (int i=Progress.Count; i<LevelNumber; i++)
                    Progress.Add(-1);
            }
            Progress[LevelNumber] = numStars;
            if (numStars >= 1)
                Progress.Add(0); // unlock next level

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