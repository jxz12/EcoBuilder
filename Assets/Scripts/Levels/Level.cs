using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

// for load/save progress
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;


namespace EcoBuilder.Levels
{
    public class Level : MonoBehaviour
    {
        [Serializable]
        public class LevelDetails
        {
            // metadata
            public int idx;
            public string title;
            public string description;
            public string introduction;
            public string congratulation;

            // constraints
            public int numProducers;
            public int numConsumers;
            public int minEdges;
            public int minChain;
            public int minLoop;
            // public int maxEdges;
            // public int maxChain;
            // public int maxLoop;

            // vertices and edges
            public int numSpecies;
            public List<bool> plants;
            public List<int> randomSeeds;
            public List<float> sizes;
            public List<float> greeds;
            public List<bool> sizeEditables;
            public List<bool> greedEditables;
            public int numInteractions;
            public List<int> resources;
            public List<int> consumers;

            public int targetScore1;
            public int targetScore2;
        }
        [SerializeField] LevelDetails details;
        [SerializeField] Tutorial tutorial;
        [SerializeField] GameObject landscape;

        public LevelDetails Details { get { return details; } }
        public Tutorial Tutorial { get { return tutorial; } }
        public GameObject Landscape { get { return landscape; } }

        // thumbnail
        [SerializeField] Text numberText;
        [SerializeField] Image starsImage;
        [SerializeField] Sprite[] starSprites;

        // card
        [SerializeField] Text title;
        [SerializeField] Text description;
        [SerializeField] ScrollRect descriptionArea;
        [SerializeField] Text producers;
        [SerializeField] Text consumers;
        [SerializeField] Text target1;
        [SerializeField] Text target2;
        [SerializeField] Text highScore;
        [SerializeField] Button playButton;
        [SerializeField] Button quitButton;
        [SerializeField] Button replayButton;

        // finish
        [SerializeField] Button finishFlag;
        // navigation
        [SerializeField] RectTransform nextLevelParent;

        void Awake()
        {
            int n = Details.numSpecies;
            if (n != Details.randomSeeds.Count || n != Details.sizes.Count || n != Details.greeds.Count)
                throw new Exception("num species and sizes or greeds do not match");

            int m = Details.numInteractions;
            if (m != Details.consumers.Count)
                throw new Exception("num edge sources and targets do not match");

            numberText.text = (Details.idx+1).ToString();
            title.text = Details.title;
            description.text = Details.description;

            producers.text = Details.numProducers.ToString();
            consumers.text = Details.numConsumers.ToString();

            target1.text = Details.targetScore1.ToString();
            target2.text = Details.targetScore2.ToString();
            // if (thumbnailedParent == null)
            //     thumbnailedParent = transform.parent.GetComponent<RectTransform>();
            targetSize = GetComponent<RectTransform>().sizeDelta;
        }

        // -1 is locked, 0,1,2,3 unlocked plus number of stars
        public void SetHighScore(int score)
        {
            highScore.text = score.ToString();

            int numStars = 0;
            if (score >= 1)
            {
                numStars += 1;
                Unlock();
            }
            if (score >= details.targetScore1)
            {
                numStars += 1;
                target1.color = Color.grey;
            }
            if (score >= details.targetScore2)
            {
                numStars += 1;
                target2.color = Color.grey;
            }

            starsImage.sprite = starSprites[numStars];
        }

        // public bool LoadFromFile(string loadPath)
        // {
        //     if (loadPath == null)
        //         return false;

        //     Details.savefilePath = loadPath;
        //     BinaryFormatter bf = new BinaryFormatter();
        //     try
        //     {
        //         FileStream file = File.Open(loadPath, FileMode.Open);
        //         Details = (LevelDetails)bf.Deserialize(file);
        //         name = Details.idx.ToString();
        //         file.Close();

        //         playButton.gameObject.SetActive(true);
        //         quitButton.gameObject.SetActive(false);
        //         replayButton.gameObject.SetActive(false);

        //         return true;
        //     }
        //     catch (ArgumentException ae)
        //     {
        //         print("handled exception: " + ae.Message);
        //         return false;
        //     }
        //     catch (SerializationException se)
        //     {
        //         print("handled exception: " + se.Message);
        //         return false;
        //     }
        //     catch (InvalidCastException ice)
        //     {
        //         print("handled exception: " + ice.Message);
        //         return false;
        //     }
        //     catch (IOException ioe)
        //     {
        //         print("handled exception: " + ioe.Message);
        //         return false;
        //     }
        // }

        // public void SaveToFile()
        // {
        //     BinaryFormatter bf = new BinaryFormatter();
        //     try
        //     {
        //         FileStream file = File.Create(Details.savefilePath);
        //         bf.Serialize(file, Details);
        //         file.Close();
        //     }
        //     catch (DirectoryNotFoundException dnfe)
        //     {
        //         print("no directory: " + dnfe.Message);
        //     }
        //     catch (ArgumentException ae)
        //     {
        //         print("file not found: " + ae.Message);
        //     }
        // }










        //////////////////////////////
        // animations and supplements

        // public void Lock()
        // {
            
        // }
        public event Action OnFinished;
        public event Action OnCarded, OnThumbnailed;

        public void Unlock()
        {
            GetComponent<Animator>().SetInteger("State", (int)State.Thumbnail);
        }
        IEnumerator WaitThenEnableQuitReplay()
        {
            playButton.gameObject.SetActive(false);
            yield return new WaitForSeconds(.5f);
            quitButton.gameObject.SetActive(true);
            replayButton.gameObject.SetActive(true);
        }
        public void StartGame()
        {
            SetNewThumbnailParent(GameManager.Instance.PlayParent, Vector2.zero);
            ShowThumbnail();
            GameManager.Instance.PlayLevel(this);
            StartCoroutine(WaitThenEnableQuitReplay());
        }
        public void BackToMenu()
        {
            // TODO: 'are you sure' option
            GameManager.Instance.ReturnToMenu();
        }
        public Level NextLevel { get; private set; }
        public void FinishLevel()
        {
            // var next = GameManager.Instance.LoadLevel(Details.nextLevelPath);
            // if (next != null)
            // {
            //     next.SetNewThumbnailParent(nextLevelParent, Vector2.zero, false);
            //     NextLevel = next;
            // }
            // else
            // {
            //     print("TODO: credits?");
            // }
            ShowNavigation();
            OnFinished?.Invoke();
        }


        public void Replay()
        {
            GameManager.Instance.PlayLevel(this);
        }

        Vector2 velocity, targetPos;
        Vector2 sizocity, targetSize;
        float smoothTime = .15f;
        void FixedUpdate()
        {
            transform.localPosition = Vector2.SmoothDamp(transform.localPosition, targetPos, ref velocity, smoothTime);
            GetComponent<RectTransform>().sizeDelta = Vector2.SmoothDamp(GetComponent<RectTransform>().sizeDelta, targetSize, ref sizocity, smoothTime);
        }

        enum State { Locked=-1, Thumbnail=0, Card=1, FinishFlag=2, Navigation=3 }
        RectTransform thumbnailedParent;
        Vector2 thumbnailedPos;
        public void ShowThumbnail()
        {
            GetComponent<Animator>().SetInteger("State", (int)State.Thumbnail);

            transform.SetParent(thumbnailedParent, true);
            transform.localScale = Vector3.one;

            targetPos = thumbnailedPos;
            targetSize = new Vector2(100,100);
            smoothTime = .2f;

            OnThumbnailed?.Invoke();
        }
        public void SetNewThumbnailParent(RectTransform newParent, Vector2 newPos, bool tween=true)
        {
            thumbnailedParent = newParent;
            transform.SetParent(thumbnailedParent, true);
            transform.localScale = Vector3.one;

            thumbnailedPos = newPos;
            if (!tween)
                transform.localPosition = newPos;

            targetPos = thumbnailedPos;
        }
        public void ShowCard()
        {
            if (GameManager.Instance.CardParent.childCount > 0)
                return;

            transform.SetParent(GameManager.Instance.CardParent, true);
            GetComponent<Animator>().SetInteger("State", (int)State.Card);
            thumbnailedPos = transform.localPosition;
            transform.localScale = Vector3.one;

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.RepaintHierarchyWindow();
            #endif

            targetPos = Vector3.zero;
            targetSize = new Vector2(450, 850);
            smoothTime = .2f;

            OnCarded?.Invoke();
        }
        public void ShowFinishFlag()
        {
            GetComponent<Animator>().SetInteger("State", (int)State.FinishFlag);
            targetSize = new Vector2(110, 110);
        }
        // called when game is ended
        public void ShowNavigation()
        {
            if (GameManager.Instance.NavParent.transform.childCount > 0)
                throw new Exception("more than one navigation?");

            thumbnailedPos = transform.localPosition;
            transform.SetParent(GameManager.Instance.NavParent.transform, true);
            transform.localScale = Vector3.one;

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.RepaintHierarchyWindow();
            #endif

            GetComponent<Animator>().SetInteger("State", (int)State.Navigation);
            targetSize = new Vector2(350, 100);
            smoothTime = .6f;
        }
    }
}