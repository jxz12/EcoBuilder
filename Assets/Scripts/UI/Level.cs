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

            // constraints
            public int numProducers;
            public int numConsumers;
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

            // -1 is locked, 0,1,2,3 unlocked plus number of stars
            public float targetFlux;
            public int numStars;
            public string savefilePath;
            public string nextLevelPath;
        }
        [SerializeField] LevelDetails details;
        public LevelDetails Details {
            get { return details; }
            private set { details = value; }
        }

        [SerializeField] Button thumbnail;

        // thumbnail
        [SerializeField] Text numberText;
        [SerializeField] Image starsImage;
        [SerializeField] Sprite[] starImages;
        [SerializeField] Image lockImage;

        // card
		[SerializeField] Text title;
		[SerializeField] Text description;
        [SerializeField] ScrollRect descriptionArea;
		[SerializeField] Text producers;
		[SerializeField] Text consumers;
        [SerializeField] Button playButton;
        [SerializeField] Button quitButton;
        [SerializeField] Button replayButton;

        // finish
        [SerializeField] Button finishFlag;

        // navigation
        [SerializeField] RectTransform nextLevelParent;

        void Start()
        {
            int n = Details.numSpecies;
            if (n != Details.randomSeeds.Count || n != Details.sizes.Count || n != Details.greeds.Count)
                throw new Exception("num species and sizes or greeds do not match");

            int m = Details.numInteractions;
            if (m != Details.consumers.Count)
                throw new Exception("num edge sources and targets do not match");

            // TODO: check that constraints are possible too

            numberText.text = Details.idx.ToString();
			title.text = Details.title;
			description.text = Details.description;
			producers.text = Details.numProducers.ToString();
			consumers.text = Details.numConsumers.ToString();

            if (Details.numStars == -1)
            {
                // TODO: animation
                Lock();
            }
            else
            {
                starsImage.sprite = starImages[Details.numStars];
                Unlock();
            }
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
                print ("handled exception: " + ice.Message);
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
                print(dnfe);
            }
        }










        //////////////////////////////
        // animations and supplements

        public void Lock()
        {
            // TODO: animation
            thumbnail.interactable = false;
            lockImage.enabled = true;
            numberText.enabled = false;
            starsImage.enabled = false;
        }
        public void Unlock()
        {
            thumbnail.interactable = true;
            lockImage.enabled = false;
            numberText.enabled = true;
            starsImage.enabled = true;
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

            NextLevel = GameManager.Instance.GetNewLevel();
            bool successful = NextLevel.LoadFromFile(Details.nextLevelPath);
            if (successful)
            {
                NextLevel.transform.SetParent(nextLevelParent, false);
            }
            else
            {
                print("TODO: credits?");
            }
		}
        public void BackToMenu()
        {
            // TODO: 'are you sure' option
            Destroy(gameObject);
            GameManager.Instance.ReturnToMenu();
        }
        public Button FinishButton { get { return finishFlag; } }
        public void FinishLevel()
        {
            ShowNavigation();
        }


        public void Replay()
        {
            // finishFlag.onClick.RemoveAllListeners();
            GameManager.Instance.PlayLevel(this);
        }

        IEnumerator LerpToPos(Vector2 goalPos, float duration)
        {
            Vector2 startPos = transform.localPosition;
            float startTime = Time.time;
            while (Time.time < startTime + duration)
            {
                transform.localPosition = Vector3.Slerp(startPos, goalPos, (Time.time-startTime)/duration);
                yield return null;
            }
            transform.localPosition = goalPos;
            descriptionArea.verticalNormalizedPosition = 1;
        }

        enum State { Thumbnail=0, Card=1, FinishFlag=2, Navigation=3 }
        Transform thumbnailedParent;
        Vector2 thumbnailedPos;
		public void ShowThumbnail()
		{
            GetComponent<Animator>().SetInteger("State", (int)State.Thumbnail);

            Vector2 prevPos = transform.position;
            transform.SetParent(thumbnailedParent, false);
            transform.position = prevPos;

            StartCoroutine(LerpToPos(thumbnailedPos, .5f));
		}
		public void ShowThumbnail(Transform newParent, Vector2 newPos)
        {
            thumbnailedParent = newParent;
            thumbnailedPos = newPos;
            ShowThumbnail();
        }
		public void ShowCard()
		{
            GetComponent<Animator>().SetInteger("State", (int)State.Card);

            // thumbnailedParent = GetComponent<RectTransform>().parent;
            // thumbnailedPos = GetComponent<RectTransform>().anchoredPosition;
            thumbnailedParent = transform.parent;
            thumbnailedPos = transform.localPosition;

            Vector2 prevPos = transform.position;
            transform.SetParent(GameManager.Instance.Overlay.transform, false);
            transform.position = prevPos;

            StartCoroutine(LerpToPos(Vector2.zero, .5f));
		}
        public void ShowFinishFlag()
        {
            // thumbnailedParent = GetComponent<RectTransform>().parent;
            // thumbnailedPos = GetComponent<RectTransform>().anchoredPosition;
            thumbnailedParent = transform.parent;
            thumbnailedPos = transform.localPosition;

            GetComponent<Animator>().SetInteger("State", (int)State.FinishFlag);
        }
        // called when game is ended
        public void ShowNavigation()
        {
            GetComponent<Animator>().SetInteger("State", (int)State.Navigation);
        }
    }
}