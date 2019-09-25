using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

// for load/save progress
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;


namespace EcoBuilder.UI
{
    public class Level : MonoBehaviour
    {
        [Serializable]
        public class LevelDetails
        {
            public int idx;
            public string title;
            public string description;
            public string introduction;
            public string congratulation;

            // constraints
            public int numProducers;
            public int numConsumers;
            public bool sizeEditable; // TODO: maybe change this back to list again!!!
            public bool greedEditable;

            public int minEdges;
            public int maxEdges;
            public int minChain;
            public int maxChain;
            public int minLoop;
            public int maxLoop;

            // vertices
            public int numSpecies;
            public List<bool> plants;
            public List<float> sizes;
            public List<float> greeds;
            public List<int> randomSeeds;
            // edges
            public int numInteractions;
            public List<int> resources;
            public List<int> consumers;

            // high scores
            public int targetScore1;
            public int targetScore2;
            public int highScore;

            // -1 is locked, 0,1,2,3 unlocked plus number of stars
            public int numStars;
            public string savefilePath;
            public string nextLevelPath;
        }
        [SerializeField] LevelDetails details;
        public LevelDetails Details {
            get { return details; }
            private set { details = value; }
        } // stupid unity serializable grumble

        // thumbnail
        [SerializeField] Text numberText;
        [SerializeField] Image starsImage;
        [SerializeField] Sprite[] starImages;

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

        [SerializeField] GameObject landscape; // TODO:

        void Start()
        {
            int n = Details.numSpecies;
            if (n != Details.randomSeeds.Count || n != Details.sizes.Count || n != Details.greeds.Count)
                throw new Exception("num species and sizes or greeds do not match");

            int m = Details.numInteractions;
            if (m != Details.consumers.Count)
                throw new Exception("num edge sources and targets do not match");

            numberText.text = Details.idx.ToString();
            title.text = Details.title;
            description.text = Details.description;

            producers.text = Details.numProducers.ToString();
            consumers.text = Details.numConsumers.ToString();

            // target1.text = GameManager.Instance.NormaliseScore(Details.targetScore1).ToString("000");
            // target2.text = GameManager.Instance.NormaliseScore(Details.targetScore2).ToString("000");
            // highScore.text = GameManager.Instance.NormaliseScore(Details.highScore).ToString("000");
            target1.text = Details.targetScore1.ToString("000");
            target2.text = Details.targetScore2.ToString("000");
            highScore.text = Details.highScore.ToString("000");

            if (Details.highScore >= details.targetScore1)
                target1.color = Color.grey;
            if (Details.highScore >= details.targetScore2)
                target2.color = Color.grey;

            if (Details.numStars != -1)
            {
                starsImage.sprite = starImages[Details.numStars];
                Unlock();
            }
            finishFlag.onClick.AddListener(()=> OnFinishClicked.Invoke());
            if (thumbnailedParent == null)
                thumbnailedParent = transform.parent.GetComponent<RectTransform>();
        }
        public event Action OnFinishClicked;
        public void SetFinishable(bool finishable)
        {
            finishFlag.interactable = finishable;
        }

        public bool LoadFromFile(string loadPath)
        {
            Details.savefilePath = loadPath;

            BinaryFormatter bf = new BinaryFormatter();
            try
            {
                FileStream file = File.Open(loadPath, FileMode.Open);
                Details = (LevelDetails)bf.Deserialize(file);
                name = Details.idx.ToString();
                file.Close();

                playButton.gameObject.SetActive(true);
                quitButton.gameObject.SetActive(false);
                replayButton.gameObject.SetActive(false);


                return true;
            }
            catch (ArgumentException ae)
            {
                print("handled exception: " + ae.Message);
                return false;
            }
            catch (SerializationException se)
            {
                print("handled exception: " + se.Message);
                return false;
            }
            catch (InvalidCastException ice)
            {
                print("handled exception: " + ice.Message);
                return false;
            }
            catch (IOException ioe)
            {
                print("handled exception: " + ioe.Message);
                return false;
            }
        }

        public void SaveToFile()
        {
            BinaryFormatter bf = new BinaryFormatter();
            try
            {
                FileStream file = File.Create(Details.savefilePath);
                bf.Serialize(file, Details);
                file.Close();
            }
            catch (DirectoryNotFoundException dnfe)
            {
                print("no directory: " + dnfe.Message);
            }
        }










        //////////////////////////////
        // animations and supplements

        // public void Lock()
        // {
            
        // }
        public void Unlock()
        {
            // TODO: pretty animation
            GetComponent<Animator>().SetInteger("State", (int)State.Thumbnail);
        }
        IEnumerator WaitThenEnableQuitReplay()
        {
            yield return new WaitForSeconds(.5f);
            playButton.gameObject.SetActive(false);
            quitButton.gameObject.SetActive(true);
            replayButton.gameObject.SetActive(true);
        }
        public Level NextLevel { get; private set; }
        public void StartGame()
        {
            GameManager.Instance.PlayLevel(this);
            StartCoroutine(WaitThenEnableQuitReplay());
        }
        public void BackToMenu()
        {
            // TODO: 'are you sure' option
            Destroy(gameObject);
            GameManager.Instance.ReturnToMenu();
        }
        public void FinishLevel()
        {
            NextLevel = Instantiate(this);
            bool successful = NextLevel.LoadFromFile(Details.nextLevelPath);
            if (successful)
            {
                NextLevel.transform.SetParent(nextLevelParent, false);
            }
            else
            {
                Destroy(NextLevel);
                print("TODO: credits?");
            }

            ShowNavigation();
        }


        public void Replay()
        {
            GameManager.Instance.PlayLevel(this);
        }

        Vector2 velocity, targetPos;
        Vector2 sizocity, targetSize;
        void FixedUpdate()
        {
            transform.localPosition = Vector2.SmoothDamp(transform.localPosition, targetPos, ref velocity, .15f);
            GetComponent<RectTransform>().sizeDelta = Vector2.SmoothDamp(GetComponent<RectTransform>().sizeDelta, targetSize, ref sizocity, .15f);
        }

        enum State { Locked=-1, Thumbnail=0, Card=1, FinishFlag=2, Navigation=3 }
        RectTransform thumbnailedParent;
        Vector2 thumbnailedPos;
        public void ShowThumbnail()
        {
            GetComponent<Animator>().SetInteger("State", (int)State.Thumbnail);

            transform.SetParent(thumbnailedParent, true);
            transform.localScale = Vector3.one;
            UnityEditor.EditorApplication.RepaintHierarchyWindow();

            targetPos = thumbnailedPos;
            targetSize = new Vector2(100,100);
        }
        public void ShowThumbnailNewParent(RectTransform newParent, Vector2 newPos)
        {
            thumbnailedParent = newParent;
            thumbnailedPos = newPos;
            ShowThumbnail();
        }
        public void ShowCard()
        {
            if (GameManager.Instance.Overlay.transform.childCount > 0)
                return;

            GetComponent<Animator>().SetInteger("State", (int)State.Card);

            thumbnailedPos = transform.localPosition;
            transform.SetParent(GameManager.Instance.Overlay.transform, true);
            transform.localScale = Vector3.one;
            UnityEditor.EditorApplication.RepaintHierarchyWindow();

            targetPos = thumbnailedPos;
            targetSize = new Vector2(450, 850);
        }
        public void ShowFinishFlag()
        {
            GetComponent<Animator>().SetInteger("State", (int)State.FinishFlag);
            targetSize = new Vector2(110, 110);
        }
        // called when game is ended
        public void ShowNavigation()
        {
            GetComponent<Animator>().SetInteger("State", (int)State.Navigation);
            targetSize = new Vector2(350, 100);
        }
    }
}