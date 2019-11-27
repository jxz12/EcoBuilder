using UnityEngine;
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
        public void QuitGame()
        {
            GameManager.Instance.Quit();
        }

        [SerializeField] GridLayoutGroup levelGrid;
        List<RectTransform> levelParents;
        void Start()
        {
            levelParents = new List<RectTransform>();
            foreach (var prefab in GameManager.Instance.GetLevelPrefabs())
            {
                var parent = new GameObject().AddComponent<RectTransform>();
                parent.SetParent(levelGrid.transform);
                parent.localScale = Vector3.one;
                levelParents.Add(parent);
                var level = Instantiate(prefab, parent);
                parent.name = level.Details.idx.ToString();
            }
        }
        // public void UnlockAllLevels()
        // {
        //     foreach (Level l in levels)
        //     {
        //         if (l.Details.numStars == -1)
        //         {
        //             l.Details.numStars = 0;
        //             l.SaveToFile();
        //         }
        //     }
        //     GameManager.Instance.UnloadSceneThenLoadAnother("Menu", "Menu");
        // }
    }
}
