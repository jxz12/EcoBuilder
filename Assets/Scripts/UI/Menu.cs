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

            foreach (string file in Directory.GetFiles(Application.persistentDataPath))
            {
                Level newLevel = Instantiate(levelPrefab);
                bool successful = newLevel.LoadFromFile(file);
                if (successful)
                {
                    levels.Add(newLevel);
                }
                else
                {
                    Destroy(newLevel.gameObject);
                }
            }
            levels = new List<Level>(levels.OrderBy(x=>x.Details.Idx));
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
                level.SaveToFile();
                levels.Add(level);
            }
            levels = new List<Level>(levels.OrderBy(x=>x.Details.Idx));
            UnlockLevels();
        }

        void UnlockLevels()
        {
            for (int i=0; i<levels.Count; i++)
            {
                if (levels[i].Details.NumStars == -1)
                {
                    levels[i].Lock();
                }
                else
                {
                    levels[i].Unlock();
                    levels[i].SetStarsSprite(starImages[levels[i].Details.NumStars]);
                }
            }
            // unlock new level if possible
            // TODO: probably move this into gamemanager
            for (int i=0; i<levels.Count-1; i++)
            {
                if (levels[i].Details.NumStars > 0 && levels[i+1].Details.NumStars == -1)
                {
                    levels[i+1].Details.NumStars = 0;
                    levels[i+1].Unlock();
                    break;
                }
            }
        }
    }
}
