using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

namespace EcoBuilder
{
    public class GameManager : MonoBehaviour
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
        }




        //////////////////////////////////////
        // data collection for demographics //
        //////////////////////////////////////

        private string email, password;
        public void SetEmailAndPassword(string email, string password)
        {
            // TODO: send these to the server to store it 
            //       check if it exists in the database,
        }
        // TODO: send these to a server with email as the key
        public void SetTeam(int team)
        {
        }
        public void SetAge(int age)
        {
        }
        public void SetGender(int gender)
        {
        }
        public void SetEducation(int education)
        {
        }
        void DoWebThing()
        {
            StartCoroutine(Http());
        }
        IEnumerator Http()
        {
            using (var p = UnityWebRequest.Get("http://localhost/ecobuilder/bar.php"))
            {
                yield return p.SendWebRequest();
                if (p.isNetworkError || p.isHttpError)
                {
                    print(p.error);
                }
                else
                {
                    print(p.downloadHandler.text);
                }
            }

            var form = new WWWForm();
            form.AddField("foo", "barr");
            using (var p = UnityWebRequest.Post("http://localhost/ecobuilder/foo.php", form))
            {
                yield return p.SendWebRequest();
                if (p.isNetworkError || p.isHttpError)
                {
                    print(p.error);
                }
                else
                {
                    print(p.downloadHandler.text);
                }
            }
        }




        ////////////////////////////////////
        // storing information for levels //
        ////////////////////////////////////

        [SerializeField] List<Levels.Level> levels; // each level is a prefab
        List<int> highScores; // TODO: store this locally and on server

        void Start()
        {
            // TODO: set this to the size of the screen
            // Screen.SetResolution(576, 1024, false);
            // #if !UNITY_WEBGL
            //     Screen.fullScreen = true;
            // #endif

            if (!PlayerPrefs.HasKey("Has Played"))
            {
                PlayerPrefs.SetString("Has Played", "yes");
                PlayerPrefs.Save();

                // TODO: teams and stuff here
                // ShowSurvey();

                // TODO: initialise levels
                // InitHighScores();
            }
            if (SceneManager.sceneCount == 1)
            {
                SceneManager.LoadSceneAsync("Menu", LoadSceneMode.Additive);
                // SceneManager.LoadSceneAsync("Play", LoadSceneMode.Additive);
            }
        }
        public void ResetSaveData()
        {
            PlayerPrefs.DeleteKey("Has Played"); // uncomment for building levels
            UnloadSceneThenLoadAnother("Menu", "Menu");
        }
        

        private void UnloadSceneThenLoadAnother(string toUnload, string another)
        {
            StartCoroutine(UnloadSceneThenLoad(toUnload, another));
        }
        event Action OnNewSceneLoaded;
        private IEnumerator UnloadSceneThenLoad(string toUnload, string toLoad)
        {
            var unloading = SceneManager.UnloadSceneAsync(toUnload, UnloadSceneOptions.None);
            while (!unloading.isDone)
            {
                yield return null;
            }
            var loading = SceneManager.LoadSceneAsync(toLoad, LoadSceneMode.Additive);
            while (!loading.isDone)
            {
                yield return null;
            }
            OnNewSceneLoaded?.Invoke();
        }

        [SerializeField] Canvas canvas;
        [SerializeField] RectTransform cardParent, playParent, navParent;
        public RectTransform CardParent { get { return cardParent; } }
        public RectTransform PlayParent { get { return playParent; } }
        public RectTransform NavParent { get { return navParent; } }

        public Levels.Level PlayedLevel { get; private set; }
        public Levels.Tutorial Teacher { get; private set; }
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
                if (Teacher != null)
                {
                    Destroy(Teacher.gameObject);
                }
                UnloadSceneThenLoadAnother("Play", "Play");
            }
            else
            {
                // play from menu
                PlayedLevel = toPlay;
                UnloadSceneThenLoadAnother("Menu", "Play");
            }
            if (toPlay.Tutorial != null)
            {
                Teacher = Instantiate(toPlay.Tutorial, canvas.transform);
            }
        }

        // public void SavePlayedLevel(int numStars, int score)
        // {
        //     if (numStars > PlayedLevel.Details.numStars)
        //         PlayedLevel.Details.numStars = numStars;

        //     if (score > PlayedLevel.Details.highScore)
        //         PlayedLevel.Details.highScore = score; // TODO: animation

        //     // unlock next level if not unlocked
        //     if (PlayedLevel.NextLevel != null &&
        //         PlayedLevel.NextLevel.Details.numStars == -1)
        //     {
        //         // TODO: animation
        //         PlayedLevel.NextLevel.Details.numStars = 0;
        //         PlayedLevel.NextLevel.SaveToFile();
        //         PlayedLevel.NextLevel.Unlock();
        //     }
        //     PlayedLevel.SaveToFile();
        // }

        [SerializeField] GameObject[] landscapes;
        public GameObject RandomLandscape()
        {
            return Instantiate(landscapes[UnityEngine.Random.Range(0, landscapes.Length)]);
        }
        public void ReturnToMenu()
        {
            if (Teacher != null)
            {
                Destroy(Teacher.gameObject);
            }
            if (PlayedLevel != null)
            {
                Destroy(PlayedLevel.gameObject);
                PlayedLevel = null;
            }
            UnloadSceneThenLoadAnother("Play", "Menu");
        }
    }
}