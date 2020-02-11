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
        [SerializeField] List<float> sizes;
        [SerializeField] List<float> greeds;
        [SerializeField] List<bool> editables;

        [SerializeField] int numInitInteractions;
        [SerializeField] List<int> sources;
        [SerializeField] List<int> targets;

        // score
        public enum ScoreMetric { None, Standard, Richness, Chain, Loop }
        [SerializeField] ScoreMetric metric;
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
        public List<bool> Plants { get { return plants; } }
        public List<int> RandomSeeds { get { return randomSeeds; } }
        public List<float> Sizes { get { return sizes; } }
        public List<float> Greeds { get { return greeds; } }
        public List<bool> Editables { get { return editables; } }

        public int NumInitInteractions { get { return numInitInteractions; } }
        public List<int> Sources { get { return sources; } }
        public List<int> Targets { get { return targets; } }

        public ScoreMetric Metric { get { return metric; } }
        public int TargetScore1 { get { return targetScore1; } }
        public int TargetScore2 { get { return targetScore2; } }
    }
    public class Level : MonoBehaviour
    {
        [SerializeField] LevelDetails details;
        public LevelDetails Details { get { return details; } }
        [SerializeField] Level nextLevelPrefab;
        public Level NextLevelPrefab { get { return nextLevelPrefab; } }

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

        [SerializeField] UI.Effect fireworksPrefab, confettiPrefab;
        [SerializeField] Tutorials.Tutorial tutorialPrefab;

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
            highScore.text = score.ToString();
            int numStars = 0;
            if (score > 0) {
                numStars += 1;
            }
            if (score >= details.TargetScore1)
            {
                numStars += 1;
                target1.color = Color.grey;
            }
            if (score >= details.TargetScore2)
            {
                numStars += 1;
                target2.color = Color.grey;
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
            if (nextLevelPrefab != null) {
                NextLevelInstantiated = Instantiate(nextLevelPrefab, nextLevelParent);
            } else {
                print("TODO: credits? reduce width of navigation?");
            }
            Instantiate(confettiPrefab, GameManager.Instance.CardParent);
            GetComponent<Animator>().SetInteger("State", (int)State.Navigation);

            GameManager.Instance.FinishLevel(this);
        }
        public Level NextLevelInstantiated { get; private set; }
        public void UnlockNextLevel() // because of silly animator gameobject active stuff
        {
            Assert.IsFalse(GameManager.Instance.NavParent.transform.childCount > 0, "more than one level on navigation?");

            StartCoroutine(TweenToZeroPosFrom(0f, GameManager.Instance.NavParent));
            if (NextLevelInstantiated != null) {
                NextLevelInstantiated.Unlock();
            }
            print("TODO: make navigation pop to below screen then rise");
        }


        // a hack to keep the card on top of the other thumbnails
        Canvas onTop;
        GraphicRaycaster raycaster;
        public void RenderOnTop(int sortOrder)
        {
            onTop = gameObject.AddComponent<Canvas>();
            onTop.overrideSorting = true;
            onTop.sortingOrder = sortOrder;
            raycaster = gameObject.AddComponent<GraphicRaycaster>();
        }
        public void RenderBelow()
        {
            Destroy(raycaster);
            Destroy(onTop);
        }

        ///////////////////////
        // Scene changing

        public void Play()
        {
            GameManager.Instance.PlayLevel(this);
            GameManager.Instance.OnLoaded.AddListener(LevelSceneLoadedCallback);
        }
        void LevelSceneLoadedCallback(string sceneName)
        {
            Assert.IsTrue(sceneName == "Play", "Play scene not loaded when expected");

            GameManager.Instance.OnLoaded.RemoveListener(LevelSceneLoadedCallback);
            thumbnailedParent = GameManager.Instance.PlayParent; // move to corner

            ShowThumbnail(1.5f);
            StartCoroutine(WaitThenEnableQuitReplay(1.5f));

            StartTutorialIfAvailable();
        }
        Tutorials.Tutorial teacher;
        void StartTutorialIfAvailable()
        {
            if (tutorialPrefab != null) {
                teacher = Instantiate(tutorialPrefab, GameManager.Instance.TutParent);
            }
        }
        void OnDestroy()
        {
            if (teacher != null) {
                Destroy(teacher.gameObject);
            }
        }
        public void Replay()
        {
            if (NextLevelInstantiated != null) { // if replay from finish
                Destroy(NextLevelInstantiated);
            }
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
            GameManager.Instance.OnLoaded.AddListener(DestroyWhenMenuLoads);
        }
        void DestroyWhenMenuLoads(string sceneName)
        {
            Assert.IsTrue(sceneName == "Menu", "Menu scene not loaded when expected");

            GameManager.Instance.OnLoaded.RemoveListener(DestroyWhenMenuLoads);
            Destroy(gameObject);
        }
// #if UNITY_EDITOR
//         public void SetInitialEcosystem(
//             List<bool> plants,
//             List<int> randomSeeds,
//             List<float> sizes,
//             List<float> greeds,
//             List<bool> editables)
//         {
//             this.plants = plants;
//             this.randomSeeds = randomSeeds;
//             this.sizes = sizes;
//             this.greeds = greeds;
//             this.editables = editables;
//         }
//         public static void SaveToNewPrefab(
//             string prefabName,
//             List<bool> plants,
//             List<int> randomSeeds,
//             List<float> sizes,
//             List<float> greeds,
//             List<bool> editables)
//         {
//             var level = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Levels/Level.prefab"));
//             level.GetComponent<Level>().SetInitialEcosystem(plants, randomSeeds, sizes, greeds, editables);
//             bool success;
//             PrefabUtility.SaveAsPrefabAsset(level, "Assets/Prefabs/Levels/"+prefabName+".prefab", out success);
//             Destroy(level);
//         }
// #endif
    }
}