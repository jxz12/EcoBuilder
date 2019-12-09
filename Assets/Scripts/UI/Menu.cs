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
                GetComponent<Animator>().SetTrigger("Reveal");
                // TODO: change settings to show the team you're on
                logo.SetActive(true);
            }
        }
        [SerializeField] GameObject logo;
        [SerializeField] Coin wolfLion;
        [SerializeField] Button flipButton;
        [SerializeField] RegistrationForm form;
        void StartRegistration()
        {
            form.gameObject.SetActive(true);
            form.OnFinished += ShowCoin;
        }
        void ShowCoin()
        {
            form.OnFinished -= ShowCoin;
            wolfLion.gameObject.SetActive(true);
            GetComponent<Animator>().SetTrigger("Coin");
        }
        bool flipped = false;
        public void FlipCoin()
        {
            if (!flipped)
            {
                wolfLion.Flip();
                wolfLion.OnLanded += ChooseTeam;
                GetComponent<Animator>().SetTrigger("Coin");
            }
            else
            {
                wolfLion.Exit();
                GetComponent<Animator>().SetTrigger("Reveal");
                logo.SetActive(true);
            }
            flipped = !flipped;
        }
        void ChooseTeam(bool team)
        {
            wolfLion.OnLanded -= ChooseTeam;
            GameManager.Instance.SetTeam(team? -1:1);
            GetComponent<Animator>().SetTrigger("Coin");
            flipButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Continue"; // TODO: boo
        }
    }
}
