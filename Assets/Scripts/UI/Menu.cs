using UnityEngine;
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
                ShowMainMenu();
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
            ShowMainMenu();
        }

        [SerializeField] Levels.Leaderboard leaderboardPrefab;
        [SerializeField] List<Levels.Level> learningLevelPrefabs;
        [SerializeField] List<Levels.Level> researchLevelPrefabs;
        void ShowMainMenu()
        {
            var unlockedIdxs = new HashSet<int>();
            unlockedIdxs.Add(learningLevelPrefabs[0].Details.idx); // always unlock first level
            Action<Levels.Level> CheckUnlocked = (l)=> {
                if (GameManager.Instance.GetHighScoreLocal(l.Details.idx) > 0)
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

            StartCoroutine(WaitThenShowLogo(.7f));
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
            GameManager.Instance.SetDragDirectionRemote(null);
        }
        public void OpenPrivacyPolicy()
        {
            GameManager.Instance.OpenPrivacyPolicyInBrowser();
        }
    }
}
