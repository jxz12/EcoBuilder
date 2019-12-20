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
        public void QuitGame()
        {
            GameManager.Instance.Quit();
        }

        [SerializeField] GridLayoutGroup levelGrid;
        [SerializeField] GameObject logo;
        [SerializeField] Coin wolfLion;
        [SerializeField] RegistrationForm form;
        List<RectTransform> levelParents;
        void Start()
        {
            if (GameManager.Instance.AskForLogin)
            {
                StartRegistration();
            }
            else
            {
                var team = GameManager.Instance.PlayerTeam;
                if (team == GameManager.PlayerDetails.Team.Lion)
                {
                    // wolfLion.Begin();
                    wolfLion.InitializeFlipped(true);
                }
                else if (team == GameManager.PlayerDetails.Team.Wolf)
                {
                    // wolfLion.Begin();
                    wolfLion.InitializeFlipped(false);
                }
                // else {} // do not init coin otherwise

                StartMainMenu();
            }
        }
        void StartMainMenu()
        {
            StartCoroutine(WaitThenShowLogo(.7f));
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
            GetComponent<Animator>().SetTrigger("Reveal");
        }
        void StartRegistration()
        {
            form.gameObject.SetActive(true);
            form.OnFinished += ShowCoin;
        }
        void ShowCoin(bool show)
        {
            form.OnFinished -= ShowCoin;
            if (show)
            {
                wolfLion.Begin();
                wolfLion.OnLanded += ChooseTeam;
            }
            else
            {
                // team already chosen
                StartMainMenu();
            }
        }
        void ChooseTeam(bool heads)
        {
            wolfLion.OnLanded -= ChooseTeam;
            GameManager.Instance.SetTeam(heads? GameManager.PlayerDetails.Team.Lion : GameManager.PlayerDetails.Team.Wolf);
            wolfLion.OnFinished += EndRegistration;
        }
        void EndRegistration()
        {
            wolfLion.OnFinished -= EndRegistration;
            StartMainMenu();
        }

        IEnumerator WaitThenShowLogo(float waitSeconds)
        {
            yield return new WaitForSeconds(waitSeconds);
            logo.SetActive(true);
        }
    }
}
