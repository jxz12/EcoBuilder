using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using System;
using System.Linq;
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
        public enum SpeciesType { Producer=0, Consumer, Apex, Specialist };
        public enum SpeciesEdit { General=0, None, SizeOnly, GreedOnly };
        [SerializeField] int numInitSpecies;
        [SerializeField] List<SpeciesEdit> edits;
        [SerializeField] List<SpeciesType> types;
        [SerializeField] List<int> sizes;
        [SerializeField] List<int> greeds;

        [SerializeField] int numInitInteractions;
        [SerializeField] List<int> sources;
        [SerializeField] List<int> targets;

        // score
        public enum ScoreMetric { None, Standard, Richness, Chain, Loop }
        [SerializeField] ScoreMetric metric;
        [SerializeField] float mainMultiplier;
        [SerializeField] float altMultiplier;
        [SerializeField] int twoStarScore;
        [SerializeField] int threeStarScore;

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
        public IReadOnlyList<SpeciesEdit> Edits { get { return edits; } }
        public IReadOnlyList<SpeciesType> Types { get { return types; } }
        public IReadOnlyList<int> Sizes { get { return sizes; } }
        public IReadOnlyList<int> Greeds { get { return greeds; } }

        public int NumInitInteractions { get { return numInitInteractions; } }
        public IReadOnlyList<int> Sources { get { return sources; } }
        public IReadOnlyList<int> Targets { get { return targets; } }

        public ScoreMetric Metric { get { return metric; } }
        public float MainMultiplier { get { return mainMultiplier; } }
        public float AltMultiplier { get { return altMultiplier; } }
        public int TwoStarScore { get { return twoStarScore; } }
        public int ThreeStarScore { get { return threeStarScore; } }

        public void SetEcosystem(List<int> randomSeeds, List<bool> plants, List<int> sizes, List<int> greeds, List<int> sources, List<int> targets)
        {
            numInitSpecies = plants.Count;
            this.edits = new List<SpeciesEdit>(Enumerable.Repeat(SpeciesEdit.General, numInitSpecies));
            this.types = new List<SpeciesType>(plants.Select(b=> b? SpeciesType.Producer : SpeciesType.Consumer)); // 
            this.sizes = sizes;
            this.greeds = greeds;
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
        // [SerializeField] Tutorials.Tutorial tutorialPrefab;
        [SerializeField] GameObject tutorialPrefab;

        public event Action OnThumbnailed, OnCarded, OnFinished;

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

        [SerializeField] UI.Effect fireworksPrefab, confettiPrefab;

        void Awake()
        {
            int n = details.NumInitSpecies;
            int m = details.NumInitInteractions;
            Assert.IsFalse(n!=details.Sizes.Count || n!=details.Greeds.Count || n!=details.Types.Count || n!=details.Edits.Count, $"num species and sizes or greeds do not match in {name}");
            Assert.IsFalse(m!=details.Sources.Count || m!=details.Targets.Count, $"num edge sources and targets do not match in {name}");

            titleText.text = details.Title;
            descriptionText.text = details.Description;

            SetScoreTexts();
        }
        public void SetScoreTexts()
        {
            target1.text = details.TwoStarScore.ToString();
            target2.text = details.ThreeStarScore.ToString();

            int score = GameManager.Instance.GetHighScoreLocal(details.Idx);
            highScore.text = score<0? "0" : score.ToString();
            int numStars = 0;
            if (score > 0) {
                numStars += 1;
            }
            if (score >= details.TwoStarScore)
            {
                numStars += 1;
                target1.color = new Color(1,1,1,.3f);
            }
            if (score >= details.ThreeStarScore)
            {
                numStars += 1;
                target2.color = new Color(1,1,1,.3f);
            }
            starsImage.sprite = starSprites[numStars];
        }

        //////////////////////////////
        // animations states

        enum State { Thumbnail=0, Card=1, FinishFlag=2 }

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
            Assert.IsFalse(GameManager.Instance.CardAnchor.childCount > 1, "more than one card on cardparent?");

            if (GameManager.Instance.CardAnchor.childCount == 1) {
                GameManager.Instance.CardAnchor.GetComponentInChildren<Level>().ShowThumbnail();
            }
            thumbnailedParent = transform.parent.GetComponent<RectTransform>();
            StartCoroutine(TweenToZeroPosFrom(.5f, GameManager.Instance.CardAnchor));

            GetComponent<Animator>().SetInteger("State", (int)State.Card);
            OnCarded?.Invoke();
        }

        public void ShowFinishFlag()
        {
            Instantiate(fireworksPrefab, transform);
            thumbnailedParent = transform.parent.GetComponent<RectTransform>();
            GetComponent<Animator>().SetInteger("State", (int)State.FinishFlag);

        }


        // a hack to keep the card on top of the other thumbnails
        Canvas canvas;
        GraphicRaycaster gRaycaster;
        public void RenderOnTop(int sortOrder)
        {
            canvas = gameObject.AddComponent<Canvas>();
            gRaycaster = gameObject.AddComponent<GraphicRaycaster>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortOrder;
        }
        public void RenderBelow()
        {
            Destroy(gRaycaster);
            Destroy(canvas);
        }

        ///////////////////////
        // Scene changing

        public void Play() // for external attaching
        {
            GameManager.Instance.LoadLevelScene(this);
        }

        // Tutorials.Tutorial teacher;
        GameObject teacher;
        public void BeginPlay()
        {
            Assert.IsTrue(GameManager.Instance.PlayedLevelDetails == details, "wrong level beginning");

            StartCoroutine(WaitOneFrameThenBeginPlay());
        }
        private IEnumerator WaitOneFrameThenBeginPlay()
        {
            yield return null;
            thumbnailedParent = GameManager.Instance.PlayAnchor; // detach from possibly the menu

            ShowThumbnail(1.5f);

            if (tutorialPrefab != null) {
                teacher = Instantiate(tutorialPrefab, GameManager.Instance.TutCanvas.transform);
            }
            GameManager.Instance.HelpText.ResetPosition();
            GameManager.Instance.HelpText.DelayThenShow(2, details.Introduction);

            // necessary because we do not have a separate animator state for playing
            playButton.interactable = false;
            yield return new WaitForSeconds(1f);

            playButton.gameObject.SetActive(false);
            quitButton.gameObject.SetActive(true);
            replayButton.gameObject.SetActive(true);
        }

        public void FinishLevel() // called on finish flag pressed
        {
            Instantiate(confettiPrefab, GameManager.Instance.CardAnchor);
            GetComponent<Animator>().SetInteger("State", (int)State.Thumbnail);

            OnFinished?.Invoke();
            StartCoroutine(EnableQuitReplayThenShowResults());
        }
        IEnumerator EnableQuitReplayThenShowResults()
        {
            playButton.interactable = true;
            playButton.gameObject.SetActive(true);
            quitButton.gameObject.SetActive(false);
            replayButton.gameObject.SetActive(false);
            yield return new WaitForSeconds(.5f);

            GameManager.Instance.ShowResultsScreen();
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
        public void Quit()
        {
            GameManager.Instance.ReturnToMenu();
        }

#if UNITY_EDITOR
        public static Level DefaultPrefab {
            get {
                return UnityEditor.AssetDatabase.LoadAssetAtPath<Level>("Assets/Prefabs/Levels/Learning Loop 1.prefab");
                // return UnityEditor.AssetDatabase.LoadAssetAtPath<Level>("Assets/Prefabs/Levels/Level Base.prefab");
            }
        }
        public static bool SaveAsNewPrefab(List<int> seeds, List<bool> plants, List<int> sizes, List<int> greeds, List<int> sources, List<int> targets, string name)
        {
            Assert.IsTrue(seeds.Count==plants.Count && plants.Count==sizes.Count && sizes.Count==greeds.Count);
            Assert.IsTrue(sources.Count==targets.Count);

            var newPrefab = (Level)UnityEditor.PrefabUtility.InstantiatePrefab(DefaultPrefab);
            newPrefab.details.SetEcosystem(seeds, plants, sizes, greeds, sources, targets);

            bool success;
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(newPrefab.gameObject, $"Assets/Prefabs/Levels/{name}.prefab", out success);
            Destroy(newPrefab.gameObject);
            return success;
        }
#endif
    }
}