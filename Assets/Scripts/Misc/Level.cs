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
        [SerializeField] string introduction;
        [SerializeField] string completedMessage;
        [SerializeField] string threeStarsMessage;

        // constraints
        [SerializeField] int numProducers;
        [SerializeField] int numConsumers;
        [SerializeField] int minEdges;
        [SerializeField] int minChain;
        [SerializeField] int minLoop;

        // score
        public enum ScoreMetric { None, Standard, Producers, Consumers, Chain, Loop }
        [SerializeField] ScoreMetric metric;
        [SerializeField] bool researchMode;
        [SerializeField] double mainMultiplier;
        [SerializeField] double altMultiplier;
        [SerializeField] long twoStarScore;
        [SerializeField] long threeStarScore;

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

        public int Idx { get { return idx; } }
        public string Title { get { return title; } }
        public string Introduction { get { return introduction; } }
        public string CompletedMessage { get { return completedMessage; } }
        public string ThreeStarsMessage { get { return threeStarsMessage; } }

        public int NumProducers { get { return numProducers; } }
        public int NumConsumers { get { return numConsumers; } }
        public int MinEdges { get { return minEdges; } }
        public int MinChain { get { return minChain; } }
        public int MinLoop { get { return minLoop; } }

        public ScoreMetric Metric { get { return metric; } }
        public bool ResearchMode { get { return researchMode; } }
        public double MainMultiplier { get { return mainMultiplier; } }
        public double AltMultiplier { get { return altMultiplier; } }
        public long TwoStarScore { get { return twoStarScore; } }
        public long ThreeStarScore { get { return threeStarScore; } }

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
        [SerializeField] GameObject tutorialPrefab;

        public event Action OnThumbnailed, OnCarded, OnFinished;

        // thumbnail
        [SerializeField] TMPro.TextMeshProUGUI indexText;
        [SerializeField] Image starsImage;
        [SerializeField] Sprite[] starSprites;

        // card
        [SerializeField] TMPro.TextMeshProUGUI titleText;
        [SerializeField] TMPro.TextMeshProUGUI target1;
        [SerializeField] TMPro.TextMeshProUGUI target2;
        [SerializeField] TMPro.TextMeshProUGUI highScore;
        [SerializeField] Button playButton;
        [SerializeField] Button quitButton;
        [SerializeField] Button replayButton;

        [SerializeField] Effect fireworksPrefab;

        void Awake()
        {
            int n = details.NumInitSpecies;
            int m = details.NumInitInteractions;
            Assert.IsFalse(n!=details.Sizes.Count || n!=details.Greeds.Count || n!=details.Types.Count || n!=details.Edits.Count, $"num species and sizes or greeds do not match in {name}");
            Assert.IsFalse(m!=details.Sources.Count || m!=details.Targets.Count, $"num edge sources and targets do not match in {name}");

            titleText.text = details.Title;

            indexText.text = ((details.Idx) % 100).ToString(); // a little smelly and probably unecessary

            target1.text = details.TwoStarScore.ToString("N0");
            target2.text = details.ThreeStarScore.ToString("N0");

            long? score = GameManager.Instance.GetHighScoreLocal(details.Idx);
            highScore.text = (score??0).ToString("N0");
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

        // enum State { Thumbnail=0, Card=1, FinishFlag=2, Leave=3 }

        [SerializeField] Image padlock;
        public void Unlock()
        {
            // TODO: nice animation?
            padlock.enabled = false;
            indexText.enabled = true;
            thumbnailGroup.interactable = true;
            if (Details.Metric != LevelDetails.ScoreMetric.None && !Details.ResearchMode) {
                starsImage.enabled = true;
            }
        }

        public void ShowCard(float duration=.5f)
        {
            Assert.IsFalse(GameManager.Instance.CardAnchor.childCount > 0, "card already on cardparent?");

            thumbnailedParent = transform.parent.GetComponent<RectTransform>();
            TweenToZeroPos(duration, GameManager.Instance.CardAnchor);
            TweenToCard(.5f);
            OnCarded?.Invoke();
        }

        Transform thumbnailedParent;
        private void HideCard(float duration=.5f)
        {
            TweenToZeroPos(duration, thumbnailedParent);
            TweenFromCard(.5f);
            OnThumbnailed?.Invoke();
        }

        public void ShowFinishFlag(float period=3f)
        {
            Instantiate(fireworksPrefab, transform);
            thumbnailedParent = transform.parent.GetComponent<RectTransform>();
            TweenToFlag(period);
        }

        public void HideFinishFlag(float duration=.5f) // called on finish flag pressed
        {
            if (teacher != null) {
                Destroy(teacher);
            }
            TweenFromFlag(duration);
            OnFinished?.Invoke();
        }

        /////////////////
        // ANIMATION

        bool tweeningPos;
        void TweenToZeroPos(float duration, Transform newParent)
        {
            IEnumerator Tween()
            {
                while (tweeningPos) {
                    yield return null;
                }
                tweeningPos = true;
                transform.SetParent(newParent, true);
                transform.localScale = Vector3.one;
                Vector2 startPos = transform.localPosition;
                float startTime = Time.time;
                while (Time.time < startTime+duration)
                {
                    float t = Tweens.QuadraticInOut((Time.time-startTime)/duration);
                    transform.localPosition = Vector2.Lerp(startPos, Vector2.zero, t);
                    yield return null;
                }
                transform.localPosition = Vector2.zero;
                tweeningPos = false;
            }
            StartCoroutine(Tween());
        }



        //////////////////////////
        // animation

        [SerializeField] Canvas cardCanvas;
        [SerializeField] CanvasGroup thumbnailGroup, cardGroup;
        [SerializeField] Image shade, back;
        bool tweeningCard;

        // a hack to keep the card on top of the other thumbnails
        Canvas extraCanvas;
        GraphicRaycaster extraRaycaster;
        void TweenToCard(float duration)
        {
            StartCoroutine(Tween());
            IEnumerator Tween()
            {
                while (tweeningCard == true) {
                    yield return null;
                }
                RenderOnTop(4);
                tweeningCard = true;
                var rt = GetComponent<RectTransform>();
                var startSize = rt.sizeDelta;
                var endSize = new Vector2(400,400);
                float aShade = shade.color.a;
                float aThumb = thumbnailGroup.alpha;
                float aCard = cardGroup.alpha;
                cardCanvas.enabled = true;
                cardGroup.interactable = true;
                cardGroup.blocksRaycasts = true;
                thumbnailGroup.interactable = false;
                thumbnailGroup.blocksRaycasts = false;
                shade.enabled = true;
                shade.raycastTarget = true;
                float startTime = Time.time;
                while (Time.time < startTime+duration)
                {
                    float t = Tweens.QuadraticInOut((Time.time-startTime)/duration);
                    rt.sizeDelta = Vector2.Lerp(startSize, endSize, t);
                    shade.color = new Color(0,0,0, Mathf.Lerp(aShade,.3f,t));
                    thumbnailGroup.alpha = Mathf.Lerp(aThumb,0,t);
                    cardGroup.alpha = Mathf.Lerp(aCard,1,t);
                    yield return null;
                }
                rt.sizeDelta = endSize;
                shade.color = new Color(0,0,0,.3f);
                thumbnailGroup.alpha = 0;
                cardGroup.alpha = 1;
                tweeningCard = false;
            }
            void RenderOnTop(int sortOrder)
            {
                extraCanvas = gameObject.AddComponent<Canvas>();
                extraRaycaster = gameObject.AddComponent<GraphicRaycaster>();
                extraCanvas.overrideSorting = true;
                extraCanvas.sortingOrder = sortOrder;
            }
        }
        void TweenFromCard(float duration)
        {
            StartCoroutine(Tween());
            IEnumerator Tween()
            {
                while (tweeningCard == true) {
                    yield return null;
                }
                tweeningCard = true;
                var rt = GetComponent<RectTransform>();
                var startSize = rt.sizeDelta;
                var endSize = new Vector2(100,100);
                float aShade = shade.color.a;
                float aThumb = thumbnailGroup.alpha;
                float aCard = cardGroup.alpha;
                cardGroup.interactable = false;
                cardGroup.blocksRaycasts = false;
                thumbnailGroup.interactable = true;
                thumbnailGroup.blocksRaycasts = true;
                shade.raycastTarget = false;
                float startTime = Time.time;
                while (Time.time < startTime+duration)
                {
                    float t = Tweens.QuadraticInOut((Time.time-startTime)/duration);
                    rt.sizeDelta = Vector2.Lerp(startSize, endSize, t);
                    shade.color = new Color(0,0,0, Mathf.Lerp(aShade,0,t));
                    thumbnailGroup.alpha = Mathf.Lerp(aThumb,1,t);
                    cardGroup.alpha = Mathf.Lerp(aCard,0,t);
                    yield return null;
                }
                rt.sizeDelta = endSize;
                shade.enabled = false;
                shade.color = new Color(0,0,0,0);
                thumbnailGroup.alpha = 1;
                cardGroup.alpha = 0;
                cardCanvas.enabled = false;
                tweeningCard = false;

                RenderBelow();
            }
            void RenderBelow()
            {
#if UNITY_EDITOR
                // for play mode opened from the start
                // so card was never displayed in the first place
                if (extraCanvas == null) {
                    return;
                }
#endif
                extraCanvas.overrideSorting = false; // RectMask2D breaks otherwise
                Destroy(extraRaycaster);
                Destroy(extraCanvas);
            }
        }
        [SerializeField] Image flagBack, flagIcon;
        bool flagging;
        void TweenToFlag(float period)
        {
            IEnumerator Oscillate()
            {
                flagBack.enabled = flagIcon.enabled = true;
                var rt = GetComponent<RectTransform>();
                float startTime = Time.time;
                flagging = true;
                while (flagging)
                {
                    float t = (Time.time - startTime) / period;
                    float size = 105 + 5*Mathf.Sin(2*Mathf.PI*t);
                    rt.sizeDelta = new Vector2(size, size);
                    yield return null;
                }
            }
            StartCoroutine(Oscillate());
        }
        void TweenFromFlag(float duration)
        {
            flagging = false;

            StartCoroutine(Spin());
            IEnumerator Spin()
            {
                var rt = GetComponent<RectTransform>();
                float startTime = Time.time;
                while (Time.time < startTime + duration)
                {
                    float t = Tweens.QuadraticInOut((Time.time-startTime) / duration);
                    float size = Mathf.Lerp(1, 0, t);
                    float rot = Mathf.Lerp(0,720, t);
                    rt.localScale = new Vector3(size, size, 1);
                    rt.localRotation = Quaternion.Euler(0,0,rot);
                    yield return null;
                }
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;

                flagBack.enabled = flagIcon.enabled = false;
                playButton.interactable = true;
                playButton.gameObject.SetActive(true);
                quitButton.gameObject.SetActive(false);
                replayButton.gameObject.SetActive(false);

                yield return null;
                GameManager.Instance.ShowReportCard();
            }
        }


        ///////////////////////
        // Scene changing

        public void Play() // attached to 'GO' button
        {
            GameManager.Instance.LoadLevelScene(this);
        }
        public void Replay() // for button
        {
            if (teacher != null) {
                Destroy(teacher.gameObject);
            }
            GameManager.Instance.ReloadLevelScene(this);
        }

        GameObject teacher;
        public void BeginPlay()
        {
            Assert.IsTrue(GameManager.Instance.PlayedLevelDetails == details, "wrong level being started");

            // wait one frame to avoid lag spike
            StartCoroutine(WaitOneFrameThenBeginPlay());
            IEnumerator WaitOneFrameThenBeginPlay()
            {
                yield return null;
                thumbnailedParent = GameManager.Instance.PlayAnchor; // detach from possibly the menu

                HideCard(1.5f);

                if (tutorialPrefab != null) {
                    teacher = Instantiate(tutorialPrefab, GameManager.Instance.TutCanvas.transform);
                }
                GameManager.Instance.HelpText.ResetLevelPosition();
                GameManager.Instance.HelpText.DelayThenShow(2, details.Introduction);

                playButton.interactable = false;
                yield return new WaitForSeconds(1f);

                playButton.gameObject.SetActive(false);
                quitButton.gameObject.SetActive(true);
                replayButton.gameObject.SetActive(true);
            }
        }
        private void Quit()
        {
            GameManager.Instance.ReturnToMenu(ClearLeaks, ()=> StartCoroutine(LeaveThenDestroyFromNextFrame(-1000, 1)));

            void ClearLeaks()
            {
                if (teacher != null) {
                    Destroy(teacher.gameObject);
                }
                StopAllCoroutines();
                gameObject.AddComponent<CanvasGroup>().interactable = false; // make sure not interactable
            }
            IEnumerator LeaveThenDestroyFromNextFrame(float targetY, float duration)
            {
                // wait one frame to avoid lag spike
                yield return null;
                
                shade.enabled = false;
                float startY = back.transform.localPosition.y;
                float startTime = Time.time;
                while (Time.time < startTime+duration)
                {
                    float t = Tweens.QuadraticInOut((Time.time-startTime)/duration);

                    float y = Mathf.Lerp(startY, targetY, t);
                    back.transform.localPosition = new Vector2(transform.localPosition.x, y);
                    yield return null;
                }
                Destroy(gameObject);
            }
        }
        void OnDestroy()
        {
            if (teacher != null) {
                Destroy(teacher.gameObject);
            }
        }

#if UNITY_EDITOR
        public static bool SaveAsNewPrefab(List<int> seeds, List<bool> plants, List<int> sizes, List<int> greeds, List<int> sources, List<int> targets, string name)
        {
            Assert.IsTrue(seeds.Count==plants.Count && plants.Count==sizes.Count && sizes.Count==greeds.Count);
            Assert.IsTrue(sources.Count==targets.Count);

            var basePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<Level>($"Assets/Prefabs/Levels/Base Level.prefab");
            var newPrefab = (Level)UnityEditor.PrefabUtility.InstantiatePrefab(basePrefab);
            newPrefab.details.SetEcosystem(seeds, plants, sizes, greeds, sources, targets);

            bool success;
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(newPrefab.gameObject, $"Assets/Prefabs/Levels/{name}.prefab", out success);
            Destroy(newPrefab.gameObject);
            return success;
        }
#endif
    }
}