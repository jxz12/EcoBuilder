using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EcoBuilder.UI
{
    public class Menu : MonoBehaviour
    {
        [SerializeField] List<Sprite> starImages;
        [SerializeField] GridLayoutGroup levelGrid;
        [SerializeField] Level levelPrefab;

        public void GoToOptions()
        {
            GetComponent<Animator>().SetInteger("Menu Choice", 0);
        }
        public void GoToMain()
        {
            GetComponent<Animator>().SetInteger("Menu Choice", 1);
        }
        public void GoToLevels()
        {
            GetComponent<Animator>().SetInteger("Menu Choice", 2);
        }
        List<Level> levels;
        void Start()
        {
            levels = new List<Level>();

            // PlayerPrefs.DeleteAll(); // uncomment for building levels
            if (!PlayerPrefs.HasKey("Has Played"))
            {
                print("saved");
                SaveSceneLevels();
                PlayerPrefs.SetString("Has Played", "yes");
                PlayerPrefs.Save();
            }
            LoadFileLevels();

            // let the grid do the layout first
            StartCoroutine(EnableGridOneFrame());
        }
        IEnumerator EnableGridOneFrame()
        {
            levelGrid.enabled = true;
            yield return null;
            levelGrid.enabled = false;
        }


        // destroys scene levels
        void LoadFileLevels()
        {
            foreach (Level level in levelGrid.transform.GetComponentsInChildren<Level>())
            {
                Destroy(level.gameObject);
            }

            foreach (string filepath in Directory.GetFiles(Application.persistentDataPath))
            {
                Level newLevel = Instantiate(levelPrefab);
                bool successful = newLevel.LoadFromFile(filepath);
                if (successful)
                {
                    levels.Add(newLevel);
                }
                else
                {
                    Destroy(newLevel.gameObject);
                }
            }
            levels = new List<Level>(levels.OrderBy(x=>x.Details.idx));
            for (int i=0; i<levels.Count-1; i++)
            {
                levels[i].transform.SetParent(levelGrid.transform, false);
                // levels[i].Details.nextLevelPath = levels[i+1].Details.savefilePath;
            }
            levels[levels.Count-1].transform.SetParent(levelGrid.transform, false);
        }
        void SaveSceneLevels()
        {
            // first load scene levels
            levels = new List<Level>(levelGrid.transform.GetComponentsInChildren<Level>().OrderBy(x=>x.Details.idx));
            for (int i=0; i<levels.Count; i++)
            {
                levels[i].Details.savefilePath = Application.persistentDataPath + "/" + levels[i].Details.idx + ".gd";
            }
            for (int i=0; i<levels.Count-1; i++)
            {
                levels[i].Details.nextLevelPath = levels[i+1].Details.savefilePath;
                levels[i].SaveToFile();
            }
            levels[levels.Count-1].SaveToFile();
        }
    }
}
