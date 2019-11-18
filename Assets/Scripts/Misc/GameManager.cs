using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

// for load/save progress
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

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

        void Start()
        {
            // TODO: set this to the size of the screen
            // Screen.SetResolution(576, 1024, false);
            // #if !UNITY_WEBGL
            //     Screen.fullScreen = true;
            // #endif

            // PlayerPrefs.DeleteAll();
            if (!PlayerPrefs.HasKey("Has Played"))
            {
                PlayerPrefs.SetString("Has Played", "yes");
                PlayerPrefs.Save();

                InitNewPlayer();
                SavePlayerDetailsLocal();
                // ShowSurvey();
            }
            else
            {
                bool loaded = LoadPlayerDetailsLocal();
                if (!loaded)
                {
                    InitNewPlayer();
                    SavePlayerDetailsLocal();
                }
            }
            if (SceneManager.sceneCount == 1)
            {
                SceneManager.LoadSceneAsync("Menu", LoadSceneMode.Additive);
                // SceneManager.LoadSceneAsync("Play", LoadSceneMode.Additive);
                earth.SetTrigger("Grow");
            }
            SavePlayerDetailsLocal();
        }


        /////////////////////
        // data collection //
        /////////////////////

        [Serializable]
        public class PlayerDetails
        {
            // TODO: store this locally and on server
            public string email;
            public string password;
            public int team;

            public int age;
            public int gender;
            public int education;
            
            public List<int> highScores;
        }
        [SerializeField] PlayerDetails player;
        public void Login(string email, string password)
        {
            StartCoroutine(Http());
        }
        public void Logout()
        {
            // TODO: keep track of playthroughs, and send if ever log in?
        }

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
        public void Quit()
        {
            // TODO: send data if possible
            Application.Quit();
        }
        public void ResetSaveData()
        {
            PlayerPrefs.DeleteKey("Has Played");
            StartCoroutine(UnloadSceneThenLoad("Menu", "Menu"));
        }
        private void InitNewPlayer()
        {
            player.highScores = new List<int>();
            player.highScores.Add(0);
            for (int i=1; i<levelPrefabs.Count; i++)
            {
                player.highScores.Add(-1);
                // player.highScores.Add(0);
            }
        }
        private bool SavePlayerDetailsLocal()
        {
            BinaryFormatter bf = new BinaryFormatter();
            try
            {
                FileStream file = File.Create(Application.persistentDataPath+"/player.data");
                bf.Serialize(file, player);
                file.Close();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("error: " + e.Message);
                return false;
            }
        }
        private bool LoadPlayerDetailsLocal()
        {
            BinaryFormatter bf = new BinaryFormatter();
            try
            {
                FileStream file = File.Open(Application.persistentDataPath+"/player.data", FileMode.Open);
                player = (PlayerDetails)bf.Deserialize(file);
                file.Close();

                return true;
            }
            catch (Exception e)
            {
                print("handled exception: " + e.Message);
                return false;
            }
        }




        ////////////////////////////////////
        // storing information for levels //
        ////////////////////////////////////

        [SerializeField] List<Levels.Level> levelPrefabs; // each level is a prefab
        public IEnumerable<Levels.Level> GetLevelPrefabs()
        {
            return levelPrefabs;
        }

        [SerializeField] Canvas canvas;
        [SerializeField] RectTransform cardParent, playParent, navParent;
        [SerializeField] Animator earth;
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

            if (toPlay.Tutorial != null)
            {
                Teacher = Instantiate(toPlay.Tutorial, canvas.transform);
            }
            earth.SetTrigger("Shrink");
        }
        // This is necessary because you cannot change prefabs from script when compiled
        // Ideally we would keep this inside Levels.Level, but that is not possible
        public int GetLevelHighScore(int levelIdx)
        {
            return player.highScores[levelIdx];
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


        ///////////////////
        // scene loading //
        ///////////////////

        // TODO: loading screen or loading events here
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
            // UnloadSceneThenLoadAnother("Play", "Menu");
            StartCoroutine(UnloadSceneThenLoad("Play", "Menu"));
            earth.SetTrigger("Grow");
        }
    }
}