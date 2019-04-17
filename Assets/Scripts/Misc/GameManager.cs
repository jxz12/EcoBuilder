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



        [SerializeField] int boardSize = 50;
        public int BoardSize { get { return boardSize; } }

        public class Level
        {
            public List<bool> IsProducers { get; private set; }
            public Func<NodeLink.NodeLink, bool> GraphConstraints { get; private set; }
            public string Description { get; private set; }
            public string ConstraintNotMetMessage { get; private set; }
            public Level()
            {
                IsProducers = new List<bool>() { true, false, false, false, false };
                // GraphConstraints = g=> g.LoopExists(3);
                GraphConstraints = g=> true;
                Description = "one producer, 4 consumers! Must contain at least one loop.";
                ConstraintNotMetMessage = "NO LOOP!";
            }
        }
        public Level SelectedLevel { get; private set; }
        /*
        graph constraints:
            min/max chain length
            must contain a cycle of length n
            omnivory (coherence)
            min energy flow (flux)
        
        vertex constraints:
            min/max number of resources
            min/max size or greediness
        */

        void Start()
        {
            SelectedLevel = new Level();

            if (SceneManager.sceneCount == 1)
                LoadScene("Menu");
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

        [SerializeField] List<Mesh> numbers;
        public Mesh GetNumberMesh(int number)
        {
            if (number < 0 || number > 9)
                throw new Exception("number out of range");

            return numbers[number];
        }

        int landscapeNumber = 0;
		[SerializeField] List<GameObject> landscapes;
        public void SwitchLandscape(bool increment)
        {
            if (increment)
                landscapeNumber += 1;
            else
                landscapeNumber -= 1;

            if (landscapeNumber < 0)
                landscapeNumber = landscapes.Count - 1;
            if (landscapeNumber >= landscapes.Count)
                landscapeNumber = 0;
        }
        public GameObject SelectedLandscape { get { return landscapes[landscapeNumber]; } }
    }
}