﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
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
        public void GoToSurvey()
        {
            GetComponent<Animator>().SetInteger("Menu Choice", 3);
        }
        public void QuitGame()
        {
            Application.Quit();
        }
        List<Level> levels;
        void Start()
        {
            PlayerPrefs.DeleteKey("Has Played"); // uncomment for building levels
            if (!PlayerPrefs.HasKey("Has Played"))
            {
                SaveSceneLevels();
                // GoToSurvey();
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
            foreach (Level level in levels)
            {
                level.enabled = false;
            }
            yield return null;
            foreach (Level level in levels)
            {
                // print(level.transform.position);
                level.SetNewThumbnailParent(levelGrid.GetComponent<RectTransform>(), level.transform.localPosition);
                level.enabled = true;
            }
            levelGrid.enabled = false;
        }

        public void ResetSaveData()
        {
            PlayerPrefs.DeleteKey("Has Played"); // uncomment for building levels
            GameManager.Instance.UnloadScene("Menu");
            GameManager.Instance.LoadScene("Menu");
        }
        public void UnlockAllLevels()
        {
            foreach (Level l in levels)
            {
                if (l.Details.numStars == -1)
                {
                    l.Details.numStars = 0;
                    l.SaveToFile();
                }
            }
            GameManager.Instance.UnloadScene("Menu");
            GameManager.Instance.LoadScene("Menu");
        }


        // destroys scene levels
        void LoadFileLevels(bool destroyLoaded=true)
        {
            if (destroyLoaded)
            {
                foreach (Level level in levelGrid.transform.GetComponentsInChildren<Level>())
                {
                    Destroy(level.gameObject);
                }
            }

            levels = new List<Level>();
            foreach (string filepath in Directory.GetFiles(Application.persistentDataPath)
                                                 .Where(s=> s.EndsWith(".gd", StringComparison.OrdinalIgnoreCase)))
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
                levels[i].transform.SetAsLastSibling();
                // levels[i].Details.nextLevelPath = levels[i+1].Details.savefilePath;
            }
            levels[levels.Count-1].transform.SetParent(levelGrid.transform, false);
        }
        void SaveSceneLevels()
        {
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


        public void SetPlayerAge(int age)
        {
            GameManager.Instance.SetAge(age);
        }
        public void SetPlayerGender(int gender)
        {
            GameManager.Instance.SetGender(gender);
        }
        public void SetPlayerEducation(int education)
        {
            GameManager.Instance.SetEducation(education);
        }
    }
}
