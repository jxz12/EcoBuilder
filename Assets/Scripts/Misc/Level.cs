using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

namespace EcoBuilder
{
    [Serializable]
    public class LevelDetails
    {
        // metadata
        [SerializeField] int idx;
        [SerializeField] string title;
        [SerializeField] string description;
        [SerializeField] string introduction;
        [SerializeField] string completedMessage;
        [SerializeField] string threeStarsMessage;

        // constraints
        [SerializeField] int numProducers;
        [SerializeField] int numConsumers;
        [SerializeField] int minEdges;
        [SerializeField] int minChain;
        [SerializeField] int minLoop;

        // gameplay
        [SerializeField] bool sizeSliderHidden;
        [SerializeField] bool greedSliderHidden;
        [SerializeField] bool conflictsAllowed;
        [SerializeField] bool superfocusAllowed;

        // vertices and edges
        [SerializeField] int numInitSpecies;
        [SerializeField] List<bool> plants;
        [SerializeField] List<int> randomSeeds;
        [SerializeField] List<int> sizes;
        [SerializeField] List<int> greeds;
        [SerializeField] List<bool> editables;

        [SerializeField] int numInitInteractions;
        [SerializeField] List<int> sources;
        [SerializeField] List<int> targets;

        // score
        public enum ScoreMetric { None, Standard, Richness, Chain, Loop }
        [SerializeField] ScoreMetric metric;
        [SerializeField] float mainMultiplier;
        [SerializeField] float altMultiplier;
        [SerializeField] int targetScore1;
        [SerializeField] int targetScore2;

        public int Idx { get { return idx; } }
        public string Title { get { return title; } }
        public string Description { get { return description; } }
        public string Introduction { get { return introduction; } }
        public string CompletedMessage { get { return completedMessage; } }
        public string ThreeStarsMessage { get { return threeStarsMessage; } }

        public int NumProducers { get { return numProducers; } }
        public int NumConsumers { get { return numConsumers; } }
        public int MinEdges { get { return minEdges; } }
        public int MinChain { get { return minChain; } }
        public int MinLoop { get { return minLoop; } }

        public bool SizeSliderHidden { get { return sizeSliderHidden; } }
        public bool GreedSliderHidden { get { return greedSliderHidden; } }
        public bool ConflictsAllowed { get { return conflictsAllowed; } }
        public bool SuperfocusAllowed { get { return superfocusAllowed; } }

        public int NumInitSpecies { get { return numInitSpecies; } }
        public IReadOnlyList<bool> Plants { get { return plants; } }
        public IReadOnlyList<int> RandomSeeds { get { return randomSeeds; } }
        public IReadOnlyList<int> Sizes { get { return sizes; } }
        public IReadOnlyList<int> Greeds { get { return greeds; } }
        public IReadOnlyList<bool> Editables { get { return editables; } }

        public int NumInitInteractions { get { return numInitInteractions; } }
        public IReadOnlyList<int> Sources { get { return sources; } }
        public IReadOnlyList<int> Targets { get { return targets; } }

        public ScoreMetric Metric { get { return metric; } }
        public float MainMultiplier { get { return mainMultiplier; } }
        public float AltMultiplier { get { return altMultiplier; } }
        public int TargetScore1 { get { return targetScore1; } }
        public int TargetScore2 { get { return targetScore2; } }

        public void SetEcosystem(List<int> randomSeeds, List<bool> plants, List<int> sizes, List<int> greeds, List<bool> editables, List<int> sources, List<int> targets)
        {
            numInitSpecies = plants.Count;
            this.randomSeeds = randomSeeds;
            this.plants = plants;
            this.sizes = sizes;
            this.greeds = greeds;
            this.editables = editables;
            numInitInteractions = sources.Count;
            this.sources = sources;
            this.targets = targets;
        }
    }
    public class Level : MonoBehaviour
    {
        [SerializeField] LevelDetails details;
        public LevelDetails Details { get { return details; } }
        [SerializeField] Level nextLevelPrefab;
        public Level NextLevelPrefab { get { return nextLevelPrefab; } }
        [SerializeField] Tutorials.Tutorial tutorialPrefab;

        public event Action OnThumbnailed, OnCarded;

        // thumbnail
        [SerializeField] Image starsImage;
        [SerializeField] Sprite[] starSprites;

        // card
        [SerializeField] TMPro.TextMeshProUGUI titleText;
        [SerializeField] TMPro.TextMeshProUGUI descriptionText;
        [SerializeField] TMPro.TextMeshProUGUI target1;
        [SerializeField] TMPro.TextMeshProUGUI target2;
        [SerializeField] TMPro.TextMeshProUGUI highScore;
        [SerializeField] Button playButton;
        [SerializeField] Button quitButton;
        [SerializeField] Button replayButton;

        // finish
        [SerializeField] Button finishFlag;
        [SerializeField] RectTransform nextLevelParent;

        [SerializeField] Canvas canvas;
        [SerializeField] UI.Effect fireworksPrefab, confettiPrefab;

        void Awake()
        {
            int n = details.NumInitSpecies;
            int m = details.NumInitInteractions;
            Assert.IsFalse(n!=details.RandomSeeds.Count || n!=details.Sizes.Count || n!=details.Greeds.Count, "num species and sizes or greeds do not match");
            Assert.IsFalse(m!=details.Sources.Count || m!=details.Targets.Count, "num edge sources and targets do not match");

            titleText.text = details.Title;
            descriptionText.text = details.Description;

            target1.text = details.TargetScore1.ToString();
            target2.text = details.TargetScore2.ToString();

            int score = GameManager.Instance.GetHighScoreLocal(details.Idx);
            highScore.text = score<0? "0" : score.ToString();
            int numStars = 0;
            if (score > 0) {
                numStars += 1;
            }
            if (score >= details.TargetScore1)
            {
                numStars += 1;
                target1.color = new Color(1,1,1,.3f);
            }
            if (score >= details.TargetScore2)
            {
                numStars += 1;
                target2.color = new Color(1,1,1,.3f);
            }
            starsImage.sprite = starSprites[numStars];
        }

        //////////////////////////////
        // animations states

        enum State { Thumbnail=0, Card=1, FinishFlag=2, Navigation=3, Leaving=4 }

        IEnumerator tweenRoutine;
        IEnumerator TweenToZeroPosFrom(float duration, Transform newParent)
        {
            while (tweenRoutine != null) {
                yield return null;
            }
            transform.SetParent(newParent, true);
            transform.localScale = Vector3.one;

            tweenRoutine = TweenToZeroPos(duration);
            yield return tweenRoutine;
            tweenRoutine = null;
        }
        IEnumerator TweenToZeroPos(float duration)
        {
            Vector3 startPos = transform.localPosition;
            float startTime = Time.time;
            while (Time.time < startTime+duration)
            {
                float t = (Time.time-startTime)/duration;
                // quadratic ease in-out
                if (t < .5f) {
                    t = 2*t*t;
                } else {
                    t = -1 + (4-2*t)*t;
                }
                transform.localPosition = Vector3.Lerp(startPos, Vector3.zero, t);
                yield return null;
            }
            transform.localPosition = Vector3.zero;
        }

        public void Unlock()
        {
            GetComponent<Animator>().SetTrigger("Unlock");
        }

        Transform thumbnailedParent;
        public void ShowThumbnail()
        {
            ShowThumbnail(.5f);
        }
        public void ShowThumbnail(float tweenTime)
        {
            StartCoroutine(TweenToZeroPosFrom(tweenTime, thumbnailedParent));

            GetComponent<Animator>().SetInteger("State", (int)State.Thumbnail);
            OnThumbnailed?.Invoke();
        }
        public void ShowCard()
        {
            Assert.IsFalse(GameManager.Instance.CardParent.childCount > 1, "more than one card on cardparent?");

            if (GameManager.Instance.CardParent.childCount == 1) {
                GameManager.Instance.CardParent.GetComponentInChildren<Level>().ShowThumbnail();
            }
            thumbnailedParent = transform.parent.GetComponent<RectTransform>();
            StartCoroutine(TweenToZeroPosFrom(.5f, GameManager.Instance.CardParent));

            GetComponent<Animator>().SetInteger("State", (int)State.Card);
            OnCarded?.Invoke();
        }

        public void ShowFinishFlag()
        {
            Instantiate(fireworksPrefab, transform);
            thumbnailedParent = transform.parent.GetComponent<RectTransform>();
            GetComponent<Animator>().SetInteger("State", (int)State.FinishFlag);
        }
        public void FinishLevel() // called on finish flag pressed
        {
            Instantiate(confettiPrefab, GameManager.Instance.CardParent);
            GetComponent<Animator>().SetInteger("State", (int)State.Navigation);

            GameManager.Instance.FinishLevel(this);
        }

        // a hack to keep the card on top of the other thumbnails
        public void RenderOnTop(int sortOrder)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortOrder;
        }
        public void RenderBelow()
        {
            canvas.overrideSorting = false;
        }

        ///////////////////////
        // Scene changing

        public void Play() // for external attaching
        {
            GameManager.Instance.LoadLevelScene(this);
        }

        Tutorials.Tutorial teacher;
        public void BeginPlay()
        {
            Assert.IsTrue(GameManager.Instance.PlayedLevelDetails == details, "wrong level beginning");

            StartCoroutine(WaitOneFrameThenBegin());
        }
        private IEnumerator WaitOneFrameThenBegin()
        {
            yield return null;
            thumbnailedParent = GameManager.Instance.PlayParent; // detach from possibly the menu

            ShowThumbnail(1.5f);
            StartCoroutine(WaitThenEnableQuitReplay(1.5f));

            if (tutorialPrefab != null) {
                teacher = Instantiate(tutorialPrefab, GameManager.Instance.TutParent);
            }
            GameManager.Instance.HelpText.ResetPosition();
            GameManager.Instance.HelpText.DelayThenShow(2, details.Introduction);
        }
        void OnDestroy()
        {
            if (teacher != null) {
                Destroy(teacher.gameObject);
            }
        }
        public void Replay()
        {
            if (teacher != null) {
                Destroy(teacher.gameObject);
            }
            Play();
        }
        // necessary because there is no separate 'playing' state
        // but the card requires different buttons
        IEnumerator WaitThenEnableQuitReplay(float waitTime)
        {
            playButton.interactable = false;
            yield return new WaitForSeconds(waitTime);
            EnableQuitReplay();
        }
        public void EnableQuitReplay() // only public because of stupidness
        {
            playButton.gameObject.SetActive(false);
            quitButton.gameObject.SetActive(true);
            replayButton.gameObject.SetActive(true);
        }

        public void Quit()
        {
            GameManager.Instance.ReturnToMenu();
            GameManager.Instance.HelpText.Showing = false;
            GameManager.Instance.OnSceneLoaded += DestroyWhenMenuLoads;
        }
        void DestroyWhenMenuLoads(string sceneName)
        {
            Assert.IsTrue(sceneName == "Menu", "Menu scene not loaded when expected");

            GameManager.Instance.OnSceneLoaded -= DestroyWhenMenuLoads;
            Destroy(gameObject);
        }

#if UNITY_EDITOR
        public static Level DefaultPrefab {
            get {
                return UnityEditor.AssetDatabase.LoadAssetAtPath<Level>("Assets/Prefabs/Levels/Learning Chain.prefab");
                // return UnityEditor.AssetDatabase.LoadAssetAtPath<Level>("Assets/Prefabs/Levels/Level Base.prefab");
            }
        }
        public static bool SaveAsNewPrefab(List<int> seeds, List<bool> plants, List<int> sizes, List<int> greeds, List<bool> editables, List<int> sources, List<int> targets, string name)
        {
            var newPrefab = (Level)UnityEditor.PrefabUtility.InstantiatePrefab(DefaultPrefab);
            newPrefab.details.SetEcosystem(seeds, plants, sizes, greeds, editables, sources, targets);

            bool success;
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(newPrefab.gameObject, $"Assets/Prefabs/Levels/{name}.prefab", out success);
            Destroy(newPrefab.gameObject);
            return success;
        }
#endif
    }
}