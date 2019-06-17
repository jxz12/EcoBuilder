using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// for load/save progress
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;


namespace EcoBuilder.UI
{
    public class Level : MonoBehaviour
    {
        /*
        graph constraints:
            min/max connectance
            min/max chain length
            min/max cycle length

        model constraints:
            min/max flux
            size/greediness (e.g. only big species)
        */
        ////////////////////////////
        // data on the level itself

        [Serializable]
        public class LevelDetails
        {
            public int Number { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }

            // constraints
            public int NumProducers { get; set; }
            public int NumConsumers { get; set; }
            public int MinEdges { get; set; }
            public int MaxEdges { get; set; }
            public int MinChain { get; set; }
            public int MaxChain { get; set; }
            public int MinLoop { get; set; }
            public int MaxLoop { get; set; }

            // vertices
            public List<int> SpeciesIdxs { get; set; }
            public List<int> RandomSeeds { get; set; }
            public List<float> Sizes { get; set; }
            public List<float> Greeds { get; set; }
            // edges
            public List<int> Resources { get; set; }
            public List<int> Consumers { get; set; }

            // progress
            public int NumStars { get; set; }
        }

        //////////////////////
        // for Unity UI

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

        string savefilePath;
        public LevelDetails Details { get; private set; }

        public void LoadFromFile(string path)
        {
            savefilePath = path;

            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(path, FileMode.Open);
            // TODO: catch some exception here
            Details = (LevelDetails)bf.Deserialize(file);
            file.Close();

            numberText.text = Details.Number.ToString();
			title.text = Details.Title;
			description.text = Details.Description;
			producers.text = Details.NumProducers.ToString();
			consumers.text = Details.NumConsumers.ToString();

			GetComponent<Animator>().SetBool("Showing", true);
            goText.text = "Go!";
        }

        public void SaveToFile(int numStars)
        {
            if (numStars < 0 || numStars > 3)
                throw new Exception("cannot pass with less than 0 or more than 3 stars");

            if (numStars > Details.NumStars)
                Details.NumStars = numStars;

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
            lockImage.gameObject.SetActive(true);
            numberText.gameObject.SetActive(false);
            starsImage.gameObject.SetActive(false);
        }
        public void Unlock()
        {
            thumbnail.interactable = true;
            lockImage.gameObject.SetActive(false);
            numberText.gameObject.SetActive(true);
            starsImage.gameObject.SetActive(true);
        }
        public void SetStarsSprite(Sprite s)
        {
            starsImage.sprite = s;
        }
    }
}