using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace EcoBuilder.Menu
{
    public class LevelSelector : MonoBehaviour
    {
        [SerializeField] List<Level> levels;
        [SerializeField] List<Sprite> starImages;
        // Start is called before the first frame update
        void Start()
        {
            SetProgress();
        }

        void SetProgress()
        {
            GameManager.Instance.InitNumLevels(levels.Count);
            for (int i=0; i<GameManager.Instance.Progress.Count; i++)
            {
                if (GameManager.Instance.Progress[i] == -1)
                {
                    levels[i].Lock();
                }
                else
                {
                    levels[i].Unlock();
                    levels[i].SetStars(starImages[GameManager.Instance.Progress[i]]);
                }
            }
            for (int i=GameManager.Instance.Progress.Count; i<levels.Count; i++)
            {
                levels[i].Lock();
            }
        }
    }
}