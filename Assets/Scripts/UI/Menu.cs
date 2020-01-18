using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

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
            if (GameManager.Instance.AskForRegistration) {
                StartRegistration();
            } else {
                StartMainMenu();
            }
        }

        void StartRegistration()
        {
            form.gameObject.SetActive(true);
            form.Reveal(StartMainMenu);
        }
        void StartMainMenu()
        {
            var team = GameManager.Instance.PlayerTeam;
            wolfLion.gameObject.SetActive(true);
            if (team == GameManager.PlayerDetails.Team.Lion)
            {
                wolfLion.InitializeFlipped(true);
                ShowMainMenu();
            }
            else if (team == GameManager.PlayerDetails.Team.Wolf)
            {
                wolfLion.InitializeFlipped(false);
                ShowMainMenu();
            } else {
                wolfLion.Reveal(ChooseTeam, ShowMainMenu);
            }
        }
        void ChooseTeam(bool heads)
        {
            var team = heads? GameManager.PlayerDetails.Team.Lion : GameManager.PlayerDetails.Team.Wolf;
            GameManager.Instance.SetTeamLocal(team);
            GameManager.Instance.SetTeamRemote(s=>print(s));
        }

        [SerializeField] Levels.Leaderboard leaderboardPrefab;
        [SerializeField] List<Levels.Level> learningLevelPrefabs;
        [SerializeField] List<Levels.Level> researchLevelPrefabs;
        void ShowMainMenu()
        {
            StartCoroutine(WaitThenShowLogo(.7f));

            foreach (var prefab in learningLevelPrefabs)
            {
                var parent = new GameObject().AddComponent<RectTransform>();
                parent.SetParent(learningLevels.transform);
                parent.localScale = Vector3.one;
                var level = Instantiate(prefab, parent);
                parent.name = level.Details.idx.ToString();
            }
            if (IsLearningFinished())
            {
                researchWorld.interactable = true;
                researchWorld.GetComponentInChildren<TMPro.TextMeshProUGUI>().color = Color.white;
                researchLock.enabled = false;
                foreach (var prefab in researchLevelPrefabs)
                {
                    var leaderboard = Instantiate(leaderboardPrefab, researchLevels.transform);
                    var level = leaderboard.GiveLevelPrefab(prefab);

                    var scores = GameManager.Instance.GetTop3ScoresRemote(level.Details.idx);
                    leaderboard.SetScores(scores.Item1, scores.Item2, scores.Item3);
                }
            }
            reverseDrag.isOn = GameManager.Instance.ReverseDragDirection;
            reverseDrag.onValueChanged.AddListener(SetReverseDrag);
            GetComponent<Animator>().SetTrigger("Reveal");
        }
        bool IsLearningFinished()
        {
            foreach (var level in learningLevelPrefabs) {
                if (GameManager.Instance.GetHighScoreLocal(level.Details.idx) <= 0) {
                    return false;
                }
            }
            return true;
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
