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
        [SerializeField] Vector2 navigationPos = new Vector2(0, -400);

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
		[SerializeField] Text goText;

        // finish flag

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
        Level levelPrefab;
        public void ProvideLevelPrefab(Level prefab)
        {
            levelPrefab = prefab;
        }

        public void SaveFromScene(string incompleteSavePath, string fileExtension)
        {
            // saves Idx as file name
            Details.savefilePath = incompleteSavePath + "/" + Details.idx + fileExtension;
            SaveToFile();
        }

        public bool LoadFromFile(string loadPath)
        {
            Details.savefilePath = loadPath;

            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(loadPath, FileMode.Open);
            try
            {
                Details = (LevelDetails)bf.Deserialize(file);
                name = Details.idx.ToString();
                file.Close();

                goText.text = "Go!";
                return true;
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

        public event Action OnFinishFlagPressed;
        public void FinishLevel()
        {
            NextLevel = Instantiate(levelPrefab);
            bool successful = NextLevel.LoadFromFile(Details.nextLevelPath);
            if (successful)
            {
                NextLevel.transform.SetParent(nextLevelParent, false);
            }
            else
            {
                print("TODO: credits?");
            }
            ShowNavigation();
            OnFinishFlagPressed.Invoke();
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
        public void BackToMenu()
        {
            // TODO: 'are you sure' option
            Destroy(gameObject);
            GameManager.Instance.ReturnToMenu();
        }

        public void Replay()
        {
            ShowThumbnail();
            GameManager.Instance.PlayLevel(this);
        }

        // IEnumerator LerpToSize(int numFrames, Vector2 goal)
        // {
        //     var rect = GetComponent<RectTransform>();
        //     Vector2 begin = rect.sizeDelta;
        //     for (int i=0; i<numFrames; i++)
        //     {
        //         rect.sizeDelta = Vector3.Lerp(begin, goal, (float)i/numFrames);
        //         yield return null;
        //     }
        //     rect.sizeDelta = goal;
        //     descriptionArea.verticalNormalizedPosition = 1;
        // }

        enum State { Thumbnail=0, Card=1, FinishFlag=2, Navigation=3 }
        Transform thumbnailedParent;
        Vector2 thumbnailedPos;
		public void ShowThumbnail()
		{
            GetComponent<Animator>().SetInteger("State", (int)State.Thumbnail);

            transform.SetParent(thumbnailedParent, false);
            GetComponent<RectTransform>().anchoredPosition = thumbnailedPos;
		}
		public void ShowCard()
		{
            GetComponent<Animator>().SetInteger("State", (int)State.Card);

            thumbnailedParent = GetComponent<RectTransform>().parent;
            thumbnailedPos = GetComponent<RectTransform>().anchoredPosition;

            transform.SetParent(GameManager.Instance.Overlay.transform, false);
            GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
		}
        public void ShowFinishFlag()
        {
            thumbnailedParent = GetComponent<RectTransform>().parent;
            thumbnailedPos = GetComponent<RectTransform>().anchoredPosition;

            GetComponent<Animator>().SetInteger("State", (int)State.FinishFlag);
        }
        // called when game is ended
        public void ShowNavigation()
        {
            GetComponent<Animator>().SetInteger("State", (int)State.Navigation);
            // TODO: magic numbers
            GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -800);

            transform.SetParent(GameManager.Instance.Overlay.transform, false);
            GetComponent<RectTransform>().anchoredPosition = navigationPos;
        }


        public Level NextLevel { get; private set; }
		public void StartEndGame()
		{
			// TODO: probably store a variable instead of this crap
			if (goText.text == "Go!")
			{
                // don't call ShowThumbnail() because we want it to stay attached to GamManager
                GetComponent<Animator>().SetInteger("State", (int)State.Thumbnail);
                goText.text = "Quit?";
				GameManager.Instance.PlayLevel(this);
			}
			else
			{
                BackToMenu();
			}
		}
    }
}