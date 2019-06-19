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
            public int numStars;
        }
        [SerializeField] LevelDetails details;
        public LevelDetails Details {
            get { return details; }
            private set { details = value; }
        }

        [SerializeField] Button showCard;

        // thumbnail
        [SerializeField] GameObject thumbnail;
        [SerializeField] Text numberText;
        [SerializeField] Image starsImage;
        [SerializeField] Image lockImage;

        // card
        [SerializeField] GameObject card;
		[SerializeField] Text title;
		[SerializeField] Text description;
        [SerializeField] ScrollRect descriptionArea;
		[SerializeField] Text producers;
		[SerializeField] Text consumers;
		[SerializeField] Button goButton;
		[SerializeField] Text goText;

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

            goText.text = "Go!";
        }

        [SerializeField] string savefilePath; // serialized just for me to see
        public void SaveFromScene(string incompleteSavePath, string fileExtension)
        {
            // saves Idx as file name
            savefilePath = incompleteSavePath + "/" + Details.idx + fileExtension;
            SaveToFile();
            // FillUI();
        }

        public bool LoadFromFile(string loadPath)
        {
            savefilePath = loadPath;

            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(loadPath, FileMode.Open);
            try
            {
                Details = (LevelDetails)bf.Deserialize(file);
                name = Details.idx.ToString();
                file.Close();

                // FillUI();
                return true;
            }
            catch (SerializationException se)
            {
                print("handled exception: " + se.Message);
                return false;
            }
        }

        public void SaveToFile()
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(savefilePath);
            bf.Serialize(file, Details);
            file.Close();
        }


        // TODO: tween these things
        Transform thumbnailedParent;
        Vector2 thumbnailedPosition;
		public void ShowCard()
		{
            thumbnail.SetActive(false);
            card.SetActive(true);
            showCard.interactable = false;

            thumbnailedPosition = GetComponent<RectTransform>().anchoredPosition;
            thumbnailedParent = GetComponent<RectTransform>().parent;

            transform.SetParent(GameManager.Instance.Overlay.transform, false);
            GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            // TODO: magic numbers
            GetComponent<RectTransform>().sizeDelta = new Vector2(400,750);

            descriptionArea.verticalNormalizedPosition = 1;
		}
		public void HideCard()
		{
            card.SetActive(false);
            thumbnail.SetActive(true);
            showCard.interactable = true;

            transform.SetParent(thumbnailedParent, false);
            GetComponent<RectTransform>().anchoredPosition = thumbnailedPosition;
            // TODO: magic numbers
            GetComponent<RectTransform>().sizeDelta = new Vector2(100,100);
		}

		public void StartEndGame()
		{
			// TODO: probably store a variable instead of this crap
			if (goText.text == "Go!")
			{
                card.SetActive(false);
                thumbnail.SetActive(true);
                showCard.interactable = true;

                // TODO: magic numbers
                GetComponent<RectTransform>().sizeDelta = new Vector2(100,100);
                goText.text = "Quit?";
				GameManager.Instance.PlayLevel(this);
			}
			else
			{
				// TODO: 'are you sure' option
				GameManager.Instance.ReturnToMenu();
                goText.text = "Go!";
			}
		}

        // TODO: make gamemanager clever enough to be able to go to next level
        public void Lock()
        {
            showCard.interactable = false;
            lockImage.enabled = true;
            numberText.enabled = false;
            starsImage.enabled = false;
        }
        public void Unlock()
        {
            showCard.interactable = true;
            lockImage.enabled = false;
            numberText.enabled = true;
            starsImage.enabled = true;
        }
        public void SetStarsSprite(Sprite s)
        {
            starsImage.sprite = s;
        }
    }
}