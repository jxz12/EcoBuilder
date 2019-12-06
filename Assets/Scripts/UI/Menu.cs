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
            if (GameManager.Instance.FirstPlay)
            {
                StartRegistration();
            }
            else
            {
                RevealMenu();
            }
        }
        [SerializeField] Coin wolfLion;
        [SerializeField] RegistrationForm form;
        void StartRegistration()
        {
            form.gameObject.SetActive(true);
            form.OnFinished += ShowCoin;
        }
        void ShowCoin()
        {
            wolfLion.gameObject.SetActive(true);
            wolfLion.OnLanded += h=> ChooseTeam(h);
        }
        void ChooseTeam(bool team)
        {

        }
        void RevealMenu()
        {
            // TODO: change settings to show the team you're on
            GetComponent<Animator>().SetTrigger("Reveal");
        }
    }
}
