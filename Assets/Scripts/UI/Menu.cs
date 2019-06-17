using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

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
            // LoadSceneLevels(); // uncomment for building levels
        }


        // -1 is locked, 0,1,2,3 unlocked plus number of stars
        void LoadFileLevels()
        {
            foreach (string file in Directory.GetFiles(Application.persistentDataPath))
            {
                Level newLevel = Instantiate(levelPrefab, levelGrid.transform);
                bool successful = newLevel.LoadFromFile(file);
                if (successful)
                {
                    levels.Add(newLevel);
                }
                else
                {
                    Destroy(newLevel);
                }
            }
            UnlockLevels();
        }
        void LoadSceneLevels()
        {
            // first load scene levels
            foreach (Level level in levelGrid.transform.GetComponentsInChildren<Level>())
            {
                level.LoadFromScene(Application.persistentDataPath, ".gd");
                level.SaveToFile();
                levels.Add(level);
            }
            UnlockLevels();
        }

        void UnlockLevels()
        {
            bool anyUnlocked = false;
            for (int i=0; i<levels.Count; i++)
            {
                if (levels[i].Details.NumStars == -1)
                {
                    levels[i].Lock();
                }
                else
                {
                    anyUnlocked = true;
                    levels[i].Unlock();
                    levels[i].SetStarsSprite(starImages[levels[i].Details.NumStars]);
                }
            }
            if (!anyUnlocked)
            {
                print(name);
                if (levels.Count > 0)
                    levels[0].Unlock();
            }
            else
            {
                // unlock new level if possible
                for (int i=0; i<levels.Count-1; i++)
                {
                    if (levels[i].Details.NumStars > 0 && levels[i+1].Details.NumStars == -1)
                    {
                        levels[i+1].Unlock();
                        break;
                    }
                }
            }
        }
    }
}
