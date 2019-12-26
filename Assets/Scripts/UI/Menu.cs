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
        [SerializeField] GridLayoutGroup learningLevels;
        [SerializeField] VerticalLayoutGroup researchLevels;
        [SerializeField] Coin wolfLion;
        [SerializeField] RegistrationForm form;
        [SerializeField] Toggle reverseDrag;

        [SerializeField] Button researchWorld;
        [SerializeField] Image researchLock;

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

        [SerializeField] Levels.Leaderboard leaderboardPrefab;
        Dictionary<int, Levels.Leaderboard> leaderboards;
        void StartMainMenu()
        {
            StartCoroutine(WaitThenShowLogo(.7f));
            foreach (var prefab in GameManager.Instance.GetLearningLevelPrefabs())
            {
                var parent = new GameObject().AddComponent<RectTransform>();
                parent.SetParent(learningLevels.transform);
                parent.localScale = Vector3.one;
                var level = Instantiate(prefab, parent);
                parent.name = level.Details.idx.ToString();
            }
            if (GameManager.Instance.IsLearningFinished())
            {
                researchWorld.interactable = true;
                researchWorld.GetComponentInChildren<TMPro.TextMeshProUGUI>().color = Color.white;
                researchLock.enabled = false; // remove lock
                leaderboards = new Dictionary<int, Levels.Leaderboard>();
                foreach (var prefab in GameManager.Instance.GetResearchLevelPrefabs())
                {
                    var leaderboard = Instantiate(leaderboardPrefab, researchLevels.transform);
                    var level = leaderboard.GiveLevelPrefab(prefab);
                    leaderboards[level.Details.idx] = leaderboard;
                    leaderboard.name = level.Details.idx.ToString();
                    // TODO: fetch best scores from server
                }
            }
            reverseDrag.isOn = GameManager.Instance.ReverseDragDirection;
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

        [SerializeField] GameObject logo;
        IEnumerator WaitThenShowLogo(float waitSeconds)
        {
            yield return new WaitForSeconds(waitSeconds);
            logo.SetActive(true);
        }
        public void QuitGame()
        {
            GameManager.Instance.Quit();
        }
        public void SetReverseDrag(bool reversed)
        {
            GameManager.Instance.SetDragDirection(reversed);
        }
    }
}
