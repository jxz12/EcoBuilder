﻿using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

namespace EcoBuilder.UI
{
    public class Menu : MonoBehaviour
    {
        [SerializeField] GridLayoutGroup learningLevels;
        [SerializeField] VerticalLayoutGroup researchLevels;
        // [SerializeField] Coin wolfLion;
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
            form.OnFinished += StartMainMenu;
        }
        void StartMainMenu()
        {
            form.OnFinished -= StartMainMenu;
            var team = GameManager.Instance.PlayerTeam;
            if (team == GameManager.PlayerDetails.Team.None) {
                ChooseTeam();
            }
            ShowMainMenu();
        }
        void ChooseTeam()
        {
            // this was previously done by coin, but will now be hidden to the user
            bool heads = UnityEngine.Random.Range(0, 2) == 0;
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

            var unlockedIdxs = new HashSet<int>();
            Action<Levels.Level> CheckUnlocked = (l)=> {
                if (GameManager.Instance.GetHighScoreLocal(l.Details.idx) >= 0)
                {
                    unlockedIdxs.Add(l.Details.idx);
                    if (l.NextLevelPrefab!=null) {
                        unlockedIdxs.Add(l.NextLevelPrefab.Details.idx);
                    }
                }
            };
            var instantiated = new Dictionary<int, Levels.Level>();
            foreach (var prefab in learningLevelPrefabs)
            {
                var parent = new GameObject().AddComponent<RectTransform>();
                parent.SetParent(learningLevels.transform);
                parent.localScale = Vector3.one;
                parent.name = prefab.Details.idx.ToString();
                
                CheckUnlocked(prefab);
                var level = Instantiate(prefab, parent);
                instantiated[level.Details.idx] = level;
            }
            if (IsLearningFinished())
            {
                researchWorld.interactable = true;
                researchWorld.GetComponentInChildren<TMPro.TextMeshProUGUI>().color = Color.white;
                researchLock.enabled = false;
                foreach (var prefab in researchLevelPrefabs)
                {
                    var scores = GameManager.Instance.GetTop3ScoresRemote(prefab.Details.idx);
                    var leaderboard = Instantiate(leaderboardPrefab, researchLevels.transform);
                    leaderboard.SetScores(scores.Item1, scores.Item2, scores.Item3);

                    CheckUnlocked(prefab);
                    var level = leaderboard.GiveLevelPrefab(prefab);
                    instantiated[level.Details.idx] = level;
                }
            }
            foreach (var idx in unlockedIdxs) {
                instantiated[idx].Unlock();
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
        public void OpenPrivacyPolicy()
        {
            GameManager.Instance.OpenPrivacyPolicyInBrowser();
        }
    }
}
