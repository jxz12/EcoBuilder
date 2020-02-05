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
        [SerializeField] Registration form;
        [SerializeField] Toggle reverseDrag;

        [SerializeField] Button researchWorld;
        [SerializeField] Image researchLock;

        [SerializeField] Button quit, logout, createAccount, deleteAccount;

        void Start()
        {
            if (GameManager.Instance.AskForRegistration) {
                StartRegistration();
            } else {
                ShowMainMenu();
            }
#if UNITY_WEBGL
            quit.gameObject.SetActive(false);
#endif
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
            Action<Levels.Level> CheckUnlocked = (l)=> {
                if (GameManager.Instance.GetHighScoreLocal(l.Details.idx) > 0)
                {
                    unlockedIdxs.Add(l.Details.idx);
                    if (l.NextLevelPrefab!=null) {
                        unlockedIdxs.Add(l.NextLevelPrefab.Details.idx);
                    }
                }
            };
            unlockedIdxs.Add(learningLevelPrefabs[0].Details.idx); // always unlock first level
            var instantiated = new Dictionary<int, Levels.Level>();
            foreach (var prefab in learningLevelPrefabs)
            {
                var parent = new GameObject().AddComponent<RectTransform>();
                parent.SetParent(learningLevels.transform);
                parent.localScale = Vector3.one;
                parent.name = prefab.Details.idx.ToString();

                CheckUnlocked.Invoke(prefab);
                var level = Instantiate(prefab, parent);
                instantiated[level.Details.idx] = level;
            }
            Action SetResearchLeaderboards = null;
            if (IsLearningFinished())
            {
                unlockedIdxs.Add(researchLevelPrefabs[0].Details.idx); // always unlock first level
                researchWorld.interactable = true;
                researchWorld.GetComponentInChildren<TMPro.TextMeshProUGUI>().color = Color.white;
                researchLock.enabled = false;
                foreach (var prefab in researchLevelPrefabs)
                {
                    var leaderboard = Instantiate(leaderboardPrefab, researchLevels.transform);
                    CheckUnlocked.Invoke(prefab);
                    var level = leaderboard.GiveLevelPrefab(prefab);
                    instantiated[level.Details.idx] = level;
                    SetResearchLeaderboards += leaderboard.SetFromGameManagerCache;
                }
            }
            foreach (var idx in unlockedIdxs) {
                instantiated[idx].Unlock();
            }

            print("TODO: more than just top 3 scores on scroll");
            GameManager.Instance.CacheLeaderboardsRemote(3, SetResearchLeaderboards);

            reverseDrag.isOn = GameManager.Instance.ReverseDragDirection;
            GetComponent<Animator>().SetTrigger("Reveal");

            StartCoroutine(WaitThenShowLogo(.7f));
            createAccount.gameObject.SetActive(!GameManager.Instance.LoggedIn);
            logout.gameObject.SetActive(GameManager.Instance.LoggedIn);
            deleteAccount.gameObject.SetActive(GameManager.Instance.LoggedIn);
        }
        bool IsLearningFinished()
        {
#if UNITY_EDITOR
            return true;
#endif
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
        public void DeleteAccount()
        {
            GameManager.Instance.DeleteAccountRemote((b,s)=> print(s));
        }
        public void CreateAccount()
        {
            GameManager.Instance.CreateAccount();
        }
        public void LogOut()
        {
            GameManager.Instance.LogOut();
        }
        public void SetReverseDrag(bool reversed)
        {
            GameManager.Instance.SetDragDirectionLocal(reversed);
            if (GameManager.Instance.LoggedIn) {
                GameManager.Instance.SetDragDirectionRemote();
            }
        }
        public void OpenPrivacyPolicy()
        {
            GameManager.Instance.OpenPrivacyPolicyInBrowser();
        }
    }
}
