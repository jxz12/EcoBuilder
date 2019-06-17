using UnityEngine;
using UnityEngine.UI;
using System;
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
            public int Idx;
            public string Title;
            public string Description;

            // constraints
            public int NumProducers;
            public int NumConsumers;
            public int MinEdges;
            public int MaxEdges;
            public int MinChain;
            public int MaxChain;
            public int MinLoop;
            public int MaxLoop;

            // vertices
            public List<int> SpeciesIdxs;
            public List<int> RandomSeeds;
            public List<float> Sizes;
            public List<float> Greeds;
            // edges
            public List<int> Resources;
            public List<int> Consumers;

            // progress
            public int NumStars;
        }
        [SerializeField] LevelDetails details;
        public LevelDetails Details {
            get { return details; }
            private set { details = value; }
        }

        // thumbnail
        [SerializeField] Button thumbnail;
        [SerializeField] Text numberText;
        [SerializeField] Image starsImage;
        [SerializeField] Image lockImage;

        // card
		[SerializeField] Text title;
		[SerializeField] Text description;
		[SerializeField] Text producers;
		[SerializeField] Text consumers;
		[SerializeField] Button goButton;
		[SerializeField] Text goText;

        void FillUI()
        {
            numberText.text = Details.Idx.ToString();
			title.text = Details.Title;
			description.text = Details.Description;
			producers.text = Details.NumProducers.ToString();
			consumers.text = Details.NumConsumers.ToString();

            goText.text = "Go!";
        }

        [SerializeField] string savefilePath; // serialized just for me to see
        public void LoadFromScene(string incompleteSavePath, string fileExtension)
        {
            // saves Idx as file name
            savefilePath = incompleteSavePath + "/" + Details.Idx + fileExtension;
            FillUI();
        }

        public bool LoadFromFile(string loadPath)
        {
            savefilePath = loadPath;

            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(loadPath, FileMode.Open);
            // TODO: catch some exception here if it doesn't deserialize a LevelDetails
            try
            {
                Details = (LevelDetails)bf.Deserialize(file);
                name = Details.Idx.ToString();
                file.Close();
                FillUI();
                return true;
            }
            catch (SerializationException se)
            {
                print(se.Message);
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

		public void ShowCard()
		{
			GetComponent<Animator>().SetBool("Showing", true);
		}


		public void StartEndGame()
		{
			// TODO: probably store a variable instead of this crap
			if (goText.text == "Go!")
			{
				GameManager.Instance.PlayGame(this);
                goText.text = "Quit?";
			}
			else
			{
				// TODO: 'are you sure' option
				GameManager.Instance.ReturnToMenu();
                goText.text = "Go!";
			}
			GetComponent<Animator>().SetBool("Showing", false);
		}
		public void Hide()
		{
			GetComponent<Animator>().SetBool("Showing", false);
		}

        void Start()
        {
            thumbnail.onClick.AddListener(()=> ShowCard());
        }

        // TODO: make this an animation to show the user where to play next?
        // TODO: make gamemanager clever enough to be able to go to next level
        public void Lock()
        {
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
        public void SetStarsSprite(Sprite s)
        {
            starsImage.sprite = s;
        }
    }
}