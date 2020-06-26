using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

namespace EcoBuilder
{
    public partial class GameManager : MonoBehaviour
    {
        // singleton pattern
        private static GameManager _gameManager;
        public static GameManager Instance {
            get {
                if (_gameManager == null)
                {
                    _gameManager = FindObjectOfType<GameManager>();
                    Assert.IsNotNull(_gameManager, "no GameManager in any scene");
                }
                return _gameManager;
            }
        }
        void Awake()
        {
            Assert.IsFalse(_gameManager!=null && _gameManager!=this, "more than one GameManager in scene");
            if (_gameManager == null) {
                _gameManager = this;
            }
            InitPlayer();
            earth = Instantiate(earthPrefab);

            print($"{SystemInfo.deviceModel}\n{SystemInfo.deviceName}\n{SystemInfo.deviceType}\n{SystemInfo.deviceUniqueIdentifier}\n{Screen.width}x{Screen.height}\n");
        }

        void Start()
        {
#if !UNITY_EDITOR
            if (SceneManager.sceneCount == 1) {
                StartCoroutine(UnloadSceneThenLoad(null, "Menu"));
            }
#endif
            SetNormalizedMasterVolume(MasterVolume);
            SendUnsentPost();
        }
            
#if UNITY_EDITOR
        int screenshotIdx=0;
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return)) {
                ScreenCapture.CaptureScreenshot($"{Screen.width}x{Screen.height} {screenshotIdx++}.png");
            }
        }
#endif

        ///////////////////
        // scene loading //
        ///////////////////

        [SerializeField] UI.LoadingBar loadingBar;
        private IEnumerator UnloadSceneThenLoad(string toUnload, string toLoad, Action OnLoaded=null)
        {
            float tStart = Time.time;
            loadingBar.Show(true);
            loadingBar.SetProgress(0);
            yield return null;

            SendUnsentPost();
            if (toUnload != null)
            {
                var unloading = SceneManager.UnloadSceneAsync(toUnload, UnloadSceneOptions.None);
                while (unloading!=null && !unloading.isDone)
                {
                    loadingBar.SetProgress(unloading.progress * .333f);
                    yield return null;
                }
                loadingBar.SetProgress(.333f);
            }
#if UNITY_EDITOR
            // minimum loading time so they can read the flavour text hehe
            if (Time.time < tStart+.5f) {
                yield return new WaitForSeconds(.5f-(Time.time-tStart));
            }
#endif
            loadingBar.SetProgress(.333f);
            if (toLoad != null)
            {
                var loading = SceneManager.LoadSceneAsync(toLoad, LoadSceneMode.Additive);
                while (loading!=null && !loading.isDone)
                {
                    loadingBar.SetProgress(.333f + .667f*loading.progress);
                    yield return null;
                }
                loadingBar.SetProgress(1);
            }
            loadingBar.Show(false);
            OnLoaded?.Invoke();
        }


        ////////////////////////////////////////////////
        // functions to persist levels through scenes //
        ////////////////////////////////////////////////

        private Level playedLevel;
        public event Action OnPlayedLevelFinished; // listened by playmanager
        public void BeginPlayedLevel() // called by playmanager
        {
            playedLevel.BeginPlay();
        }
        public void MakePlayedLevelFinishable() // called by playmanager
        {
            playedLevel.ShowFinishFlag();
        }

#if UNITY_EDITOR
        [SerializeField] Level defaultLevelPrefab;
#endif
        public LevelDetails PlayedLevelDetails {
            get {
#if UNITY_EDITOR
                // for convenience in editor
                if (playedLevel == null)
                {
                    playedLevel = Instantiate(defaultLevelPrefab);
                    playedLevel.OnFinished += ()=>OnPlayedLevelFinished.Invoke();

                    playedLevel.Unlock();
                }
#endif
                Assert.IsNotNull(playedLevel, "no level being played");
                return playedLevel.Details;
            }
        }

        public void LoadLevelScene(Level toPlay)
        {
            earth.ResetParent(); 
            report.ClearNavigation(toPlay);
            if (playedLevel != null) // if playing already
            {
                if (playedLevel != toPlay)
                {
                    Destroy(playedLevel.gameObject);
                    playedLevel = toPlay;
                    playedLevel.OnFinished += ()=>OnPlayedLevelFinished.Invoke();
                } else {
                    // replay level so no need to destroy
                }
                StartCoroutine(UnloadSceneThenLoad("Play", "Play", ()=>report.Hide()));
            }
            else
            {
                // play from menu or report card
                playedLevel = toPlay;
                playedLevel.OnFinished += ()=>OnPlayedLevelFinished.Invoke();
                StartCoroutine(UnloadSceneThenLoad("Menu", "Play", ()=>report.Hide()));
            }
        }
        public void ReloadLevelScene(Level toPlay)
        {
            alert.GiveChoice(()=>LoadLevelScene(toPlay), "Are you sure you want to restart the level?");
        }




        // for levels to attach to in order to persist across scenes
        [SerializeField] RectTransform cardAnchor, playAnchor;
        public RectTransform CardAnchor { get { return cardAnchor; } }
        public RectTransform PlayAnchor { get { return playAnchor; } }
        [SerializeField] Canvas tutorialCanvas;
        public Canvas TutCanvas { get { return tutorialCanvas; } }

        [SerializeField] UI.Alert alert;
        public void ReturnToMenu(Action OnConfirm, Action OnMenuLoaded)
        {
            alert.GiveChoice(BackToMenu, "Are you sure you want to return to the main menu?");
            void BackToMenu()
            {
                OnConfirm.Invoke(); 
                HelpText.ResetMenuPosition(false);
                earth.ResetParent(); 

                // Destroy(playedLevel.gameObject) // playedLevel should destroy itself with own coroutine if necessary
                report.ClearNavigation(playedLevel); // this is probably unnecessary, but better safe than sorry
                StartCoroutine(UnloadSceneThenLoad("Play", "Menu", ()=>{ OnMenuLoaded?.Invoke(); report.Hide(); earth.TweenToRestPositionFromNextFrame(2); })); 
                // wait until next frame to avoid the frame spike caused by Awake and Start()
            }
        }

        public void Quit()
        {
            alert.GiveChoice(CloseGameFully, "Are you sure you want to quit?");
        }
        public void ShowAlert(string message)
        {
            alert.ShowInfo(message);
        }
        public void CloseGameFully()
        {
            SendUnsentPost();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        [SerializeField] AudioMixer skrillex;
        private void SetNormalizedMasterVolume(float normalisedVolume)
        {
            float clamped = Mathf.Clamp(normalisedVolume, .0001f, 1f);
            skrillex.SetFloat("Master Volume", Mathf.Log10(clamped) * 20);
        }
        private void SetNormalizedEffectsVolume(float normalisedVolume)
        {
            float clamped = Mathf.Clamp(normalisedVolume, .0001f, 1f);
            skrillex.SetFloat("Effects Volume", Mathf.Log10(clamped) * 20);
        }
        public void FadeEffectsVolume()
        {
            // TODO: use the effects mixer to fade out, but take the maximum of all fade volumes so that other effects do not get cut off
            // TODO: set this effects mixer to zero at the start of play? so that the initial effects do not make a noise?
        }

        [SerializeField] Planet earthPrefab;
        Planet earth;
        public GameObject TakePlanet()
        {
            Assert.IsNotNull(earth, "earth was destroyed :(");
            earth.TweenToRestPositionFromNextFrame(2);
            return earth.gameObject;
        }

        ///////////////////////////
        // showing help messages //
        ///////////////////////////

        [SerializeField] UI.Help helpText;
        public UI.Help HelpText { get { return helpText; } }

        [SerializeField] UI.ReportCard report;

        // called by playmanager
        public void SetReportCard(int nStars, long score, string rank, string matrix, string actions, string scoreInfo)
        {
            int idx = playedLevel.Details.Idx;

            long? prevScore = GetHighScoreLocal(idx);
            SaveHighScoreLocal(idx, score);

            if (LoggedIn) {
                SavePlaythroughRemote(idx, score, matrix, actions);
            }

            if (playedLevel.Details.Metric == LevelDetails.ScoreMetric.None) {
                report.SetResults(null, null, null, null, null, null);
            } else if (playedLevel.Details.ResearchMode) {
                report.SetResults(null, score, prevScore, rank, GetCachedMedian(idx), scoreInfo);
            } else {
                report.SetResults(nStars, score, prevScore, null, GetCachedMedian(idx), scoreInfo);
            }
        }
        // called by level when it is ready
        public void ShowReportCard()
        {
            if (playedLevel.NextLevelPrefab != null) {
                report.GiveNavigation(playedLevel, Instantiate(playedLevel.NextLevelPrefab));
            } else {
                report.GiveNavigation(playedLevel, null);
            }
            report.ShowResults();
        }
    }
}