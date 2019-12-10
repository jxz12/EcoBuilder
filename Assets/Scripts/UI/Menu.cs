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
        [SerializeField] GameObject logo;
        [SerializeField] Coin wolfLion;
        [SerializeField] RegistrationForm form;
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
                int team = GameManager.Instance.PlayerTeam;
                if (team == 1)
                {
                    wolfLion.gameObject.SetActive(true);
                    wolfLion.InitializeFlipped(true);
                }
                else if (team == -1)
                {
                    wolfLion.gameObject.SetActive(true);
                    wolfLion.InitializeFlipped(false);
                }
                StartMainMenu();
            }
        }
        void StartRegistration()
        {
            form.gameObject.SetActive(true);
            form.OnFinished += ShowCoin;
        }
        public void SkipRegistration()
        {
            form.OnFinished -= ShowCoin;
            StartMainMenu();
        }
        void ShowCoin()
        {
            form.OnFinished -= ShowCoin;
            wolfLion.gameObject.SetActive(true);
            wolfLion.OnLanded += ChooseTeam;
        }
        void ChooseTeam(bool heads)
        {
            wolfLion.OnLanded -= ChooseTeam;
            GameManager.Instance.SetTeam(heads? 1:-1);
            wolfLion.OnFinished += EndRegistration;
        }
        void EndRegistration()
        {
            wolfLion.OnFinished -= EndRegistration;
            GameManager.Instance.SavePlayerDetails();
            StartMainMenu();
        }

        void StartMainMenu()
        {
            StartCoroutine(WaitThenShowLogo(.7f));
            GetComponent<Animator>().SetTrigger("Reveal");
        }
        IEnumerator WaitThenShowLogo(float waitSeconds)
        {
            yield return new WaitForSeconds(waitSeconds);
            logo.SetActive(true);
        }
    }
}
