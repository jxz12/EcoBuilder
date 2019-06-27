using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Text;

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
            #if !UNITY_WEBGL
                Screen.fullScreen = true;
            #endif

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

        // TODO: this is pretty ugly
        [SerializeField] UI.Level LevelPrefab;
        public UI.Level GetNewLevel()
        {
           return Instantiate(LevelPrefab);
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
        public void SavePlayedLevel(int numStars, float score)
        {
            if (numStars < 1 || numStars > 3)
                throw new Exception("cannot pass with less than 0 or more than 3 stars");

            if (PlayedLevel != null)
            {
                if (numStars > PlayedLevel.Details.numStars)
                    PlayedLevel.Details.numStars = numStars;

                if (score > PlayedLevel.Details.highScore)
                    PlayedLevel.Details.highScore = score;

                // unlock next level if not unlocked
                if (PlayedLevel.NextLevel.Details.numStars == -1)
                {
                    print("TODO: animation here!");
                    PlayedLevel.NextLevel.Details.numStars = 0;
                    PlayedLevel.NextLevel.SaveToFile();
                    PlayedLevel.NextLevel.Unlock();
                }


                PlayedLevel.SaveToFile();
            }
            else
            {
                print("HOW DID YOU GET HERE");
            }
        }
        public void SaveFoodWebToCSV(
            List<int> speciesIdxs, List<int> randomSeeds,
            List<float> sizes, List<float> greeds,
            List<int> resources, List<int> consumers,
            List<string> paramNames, List<double> paramValues)
        {
            var sb = new StringBuilder();
            int n = speciesIdxs.Count;
            if (n != randomSeeds.Count || n != sizes.Count || n != greeds.Count)
                throw new Exception("length of traits not equal to length of idxs");

            int m = resources.Count;
            if (m != consumers.Count)
                throw new Exception("length of sources not equal to length of targets");

            int p = paramNames.Count;
            if (p != paramValues.Count)
                throw new Exception("length of param names not equal to values");

            sb.Append("Index,");
            for (int i=0; i<n-1; i++)
            {
                sb.Append(speciesIdxs[i]).Append(",");
            }
            sb.Append(speciesIdxs[n-1]).Append("\nRandom Seed,");
            for (int i=0; i<n-1; i++)
            {
                sb.Append(randomSeeds[i]).Append(",");
            }
            sb.Append(randomSeeds[n-1]).Append("\nBody Size,");
            for (int i=0; i<n-1; i++)
            {
                sb.Append(sizes[i]).Append(",");
            }
            sb.Append(sizes[n-1]).Append("\nGreediness,");
            for (int i=0; i<n-1; i++)
            {
                sb.Append(greeds[i]).Append(",");
            }
            sb.Append(greeds[n-1]).Append("\nResource,");
            for (int ij=0; ij<m-1; ij++)
            {
                sb.Append(resources[ij]).Append(",");
            }
            sb.Append(resources[m-1]).Append("\nConsumer,");
            for (int ij=0; ij<m-1; ij++)
            {
                sb.Append(consumers[ij]).Append(",");
            }
            sb.Append(consumers[m-1]).Append("\nParameter,");

            for (int i=0; i<p-1; i++)
            {
                sb.Append(paramNames[i]).Append(",");
            }
            sb.Append(paramNames[p-1]).Append("\nValue,");
            for (int i=0; i<p-1; i++)
            {
                sb.Append(paramValues[i]).Append(",");
            }
            sb.Append(paramValues[p-1]);

            print(sb.ToString());
        }
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

        public float NormaliseScore(float input)
        {
            if (input <= 0)
                return 0;

            float normalised = Mathf.Log10(input * 1e10f) * 25;
            return Mathf.Max(normalised, 1f);
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