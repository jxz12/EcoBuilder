using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;

namespace EcoBuilder
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager gameManager;
        public static GameManager Instance {
            get {
                if (gameManager == null)
                {
                    // Debug.LogError("No active GameManager");
                    Debug.LogWarning("No active GameManager");
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
            // TODO: set this to the size of the screen
            // Screen.SetResolution(576, 1024, false);
            // #if !UNITY_WEBGL
            //     Screen.fullScreen = true;
            // #endif

            // StartCoroutine(Http());

            if (SceneManager.sceneCount == 1)
            {
                // SceneManager.LoadSceneAsync("Menu", LoadSceneMode.Additive);
                SceneManager.LoadSceneAsync("Play", LoadSceneMode.Additive);
            }
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
        
        public void UnloadSceneThenLoadAnother(string toUnload, string another)
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


        [SerializeField] Canvas canvas;

        // TODO: move this back into two
        [SerializeField] RectTransform cardParent, navParent, playParent;
        public RectTransform CardParent { get { return cardParent; } }
        public RectTransform NavParent { get { return navParent; } }

        [SerializeField] Level levelPrefab;
        public Level LoadLevel(string path=null)
        {
            var level = Instantiate(levelPrefab);
            bool successful = level.LoadFromFile(path);
            if (successful)
            {
                return level;
            }
            else
            {
                Destroy(level);
                return null;
            }
        }
        public Level GetDefaultLevel() // only for testing
        {
            return Instantiate(levelPrefab);
        }

        public Level PlayedLevel { get; private set; }
        public void PlayLevel(Level level)
        {
            string toUnload = "";
            if (PlayedLevel != null)
            {
                if (PlayedLevel != level)
                {
                    Destroy(PlayedLevel.gameObject);
                    PlayedLevel = level;
                }
                else
                {
                    // replay level
                }
                if (teacher != null)
                {
                    Destroy(teacher.gameObject);
                }
                toUnload = "Play";
            }
            else
            {
                // play from menu
                PlayedLevel = level;
                toUnload = "Menu";
            }
            level.SetNewThumbnailParent(playParent, Vector2.zero);
            level.ShowThumbnail();
            UnloadSceneThenLoadAnother(toUnload, "Play");
        }
        // TODO: make this not so horrible
        [SerializeField] Tutorials.Tutorial[] tutorials;
        Tutorials.Tutorial teacher;
        public void LoadTutorialIfNeeded()
        {
            if (PlayedLevel.Details.idx < tutorials.Length)
            {
                teacher = Instantiate(tutorials[PlayedLevel.Details.idx], canvas.transform, false);
            }
        }

        public void SavePlayedLevel(int numStars, int score)
        {
            if (numStars > PlayedLevel.Details.numStars)
                PlayedLevel.Details.numStars = numStars;

            if (score > PlayedLevel.Details.highScore)
                PlayedLevel.Details.highScore = score; // TODO: animation

            // unlock next level if not unlocked
            if (PlayedLevel.NextLevel != null &&
                PlayedLevel.NextLevel.Details.numStars == -1)
            {
                // TODO: animation
                PlayedLevel.NextLevel.Details.numStars = 0;
                PlayedLevel.NextLevel.SaveToFile();
                PlayedLevel.NextLevel.Unlock();
            }
            PlayedLevel.SaveToFile();
        }

        [SerializeField] GameObject[] landscapes;
        public GameObject RandomLandscape()
        {
            return Instantiate(landscapes[UnityEngine.Random.Range(0, landscapes.Length)]);
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
        public void ReturnToMenu()
        {
            if (teacher != null)
            {
                Destroy(teacher.gameObject);
            }
            if (PlayedLevel != null)
            {
                Destroy(PlayedLevel.gameObject);
                PlayedLevel = null;
            }
            UnloadSceneThenLoadAnother("Play", "Menu");
        }
    }

    #if UNITY_EDITOR
    public class ReadOnlyAttribute : PropertyAttribute {}
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ShowOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
        {
            string valueStr;
    
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    valueStr = prop.intValue.ToString();
                    break;
                case SerializedPropertyType.Boolean:
                    valueStr = prop.boolValue.ToString();
                    break;
                case SerializedPropertyType.Float:
                    valueStr = prop.floatValue.ToString("e1");
                    break;
                case SerializedPropertyType.String:
                    valueStr = prop.stringValue;
                    break;
                default:
                    valueStr = "(not supported)";
                    break;
            }
    
            EditorGUI.LabelField(position,label.text, valueStr);
        }
    }
    #else
    // empty attribute
    public class ReadOnlyAttribute : PropertyAttribute {}
    #endif
}