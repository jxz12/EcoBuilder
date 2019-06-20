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
            LoadFileLevels();
            // SaveSceneLevels(); // uncomment for building levels

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
            foreach (Level level in levels)
            {
                level.transform.SetParent(levelGrid.transform, false);
            }
            UnlockLevels();
        }
        void SaveSceneLevels()
        {
            // first load scene levels
            foreach (Level level in levelGrid.transform.GetComponentsInChildren<Level>())
            {
                level.SaveFromScene(Application.persistentDataPath, ".gd");
                levels.Add(level);
            }
            levels = new List<Level>(levels.OrderBy(x=>x.Details.idx));
            // UnlockLevels();
        }

        void UnlockLevels()
        {
            for (int i=0; i<levels.Count; i++)
            {
                if (levels[i].Details.numStars == -1)
                {
                    levels[i].Lock();
                }
                else
                {
                    levels[i].Unlock();
                    levels[i].SetStarsSprite(starImages[levels[i].Details.numStars]);
                }
            }
            // unlock new level if possible
            bool unlocked = false;
            for (int i=0; i<levels.Count-1; i++)
            {
                if (!unlocked && levels[i].Details.numStars > 0 && levels[i+1].Details.numStars == -1)
                {
                    levels[i+1].Details.numStars = 0;
                    levels[i+1].Unlock();
                    unlocked = true;
                }
                levels[i].Details.nextLevelPath = levels[i+1].Details.savefilePath;
            }
        }
    }
}
