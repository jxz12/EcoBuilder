using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

namespace EcoBuilder.UI
{
    public class Menu : MonoBehaviour
    {
        [SerializeField] GridLayoutGroup learningLevels;
        [SerializeField] VerticalLayoutGroup researchLevels;
        [SerializeField] Coin wolfLion;
        [SerializeField] Registration form;
        [SerializeField] Toggle reverseDrag;

        [SerializeField] Button researchWorld;
        [SerializeField] Image researchLock;

        void Start()
        {
            if (GameManager.Instance.AskForLogin) {
                StartRegistration();
            } else {
                StartMainMenu();
            }
        }

        void StartRegistration()
        {
            form.gameObject.SetActive(true);
            form.OnFinished += RegisteredHandler;
        }

        [SerializeField] Levels.Leaderboard leaderboardPrefab;
        void RegisteredHandler(object sender, EventArgs e)
        {
            form.OnFinished -= RegisteredHandler;
            var team = GameManager.Instance.PlayerTeam;
            if (team == GameManager.PlayerDetails.Team.Lion)
            {
                wolfLion.InitializeFlipped(true);
                StartMainMenu();
            }
            else if (team == GameManager.PlayerDetails.Team.Wolf)
            {
                wolfLion.InitializeFlipped(false);
                StartMainMenu();
            }
            else
            {
                wolfLion.Begin();
                wolfLion.OnLanded += ChooseTeam;
            }
        }
        void ChooseTeam(bool heads)
        {
            wolfLion.OnLanded -= ChooseTeam;

            var team = heads? GameManager.PlayerDetails.Team.Lion : GameManager.PlayerDetails.Team.Wolf;
            GameManager.Instance.SetTeamLocal(team);
            GameManager.Instance.SetTeamRemote(s=>print(s));
            print("TODO: error if not connected");
            wolfLion.OnFinished += EndRegistration;
        }
        void EndRegistration()
        {
            wolfLion.OnFinished -= EndRegistration;
            StartMainMenu();
        }
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
            if (GameManager.Instance.IsLearningFinished)
            {
                researchWorld.interactable = true;
                researchWorld.GetComponentInChildren<TMPro.TextMeshProUGUI>().color = Color.white;
                researchLock.enabled = false; // remove lock
                foreach (var prefab in GameManager.Instance.GetResearchLevelPrefabs())
                {
                    var leaderboard = Instantiate(leaderboardPrefab, researchLevels.transform);
                    var level = leaderboard.GiveLevelPrefab(prefab);

                    var scores = GameManager.Instance.GetTop3ScoresRemote(level.Details.idx);
                    leaderboard.SetScores(scores.Item1, scores.Item2, scores.Item3);
                }
            }
            reverseDrag.isOn = GameManager.Instance.ReverseDragDirection;
            GetComponent<Animator>().SetTrigger("Reveal");
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
            GameManager.Instance.SetDragDirectionLocal(reversed);
            GameManager.Instance.SetDragDirectionRemote(b=>print("TODO: set sync=false"));
        }
    }
}
