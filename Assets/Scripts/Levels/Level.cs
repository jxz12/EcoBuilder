using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;


namespace EcoBuilder.Levels
{
    public class Level : MonoBehaviour
    {
        [Serializable]
        public class LevelDetails
        {
            // metadata
            public int idx;
            public string title;
            public string description;
            public string introduction;
            public string congratulation;

            // constraints
            public int numProducers;
            public int numConsumers;
            public int minEdges;
            public int minChain;
            public int minLoop;

            // vertices and edges
            public int numSpecies;
            public List<bool> plants;
            public List<int> randomSeeds;
            public List<float> sizes;
            public List<float> greeds;
            public List<bool> sizeEditables;
            public List<bool> greedEditables;
            public int numInteractions;
            public List<int> resources;
            public List<int> consumers;

            // public int highScore;
            public int targetScore1;
            public int targetScore2;
        }
        [SerializeField] LevelDetails details;
        [SerializeField] Tutorial tutorial;
        [SerializeField] GameObject landscape;
        [SerializeField] Level nextLevel;

        public event Action OnThumbnailed, OnCarded, OnFinished;
        public LevelDetails Details { get { return details; } }
        public Tutorial Tutorial { get { return tutorial; } }
        public GameObject Landscape { get { return landscape; } }
        public Level NextLevel { get; private set; }

        // thumbnail
        [SerializeField] Image starsImage;
        [SerializeField] Sprite[] starSprites;

        // card
        [SerializeField] TMPro.TextMeshProUGUI title;
        [SerializeField] TMPro.TextMeshProUGUI description;
        [SerializeField] TMPro.TextMeshProUGUI target1;
        [SerializeField] TMPro.TextMeshProUGUI target2;
        [SerializeField] TMPro.TextMeshProUGUI highScore;
        [SerializeField] Button playButton;
        [SerializeField] Button quitButton;
        [SerializeField] Button replayButton;

        // finish
        [SerializeField] Button finishFlag;
        [SerializeField] UI.Effect fireworks;
        // navigation
        [SerializeField] RectTransform nextLevelParent;

        void Start()
        {
            int n = Details.numSpecies;
            if (n != Details.randomSeeds.Count || n != Details.sizes.Count || n != Details.greeds.Count)
                throw new Exception("num species and sizes or greeds do not match");

            int m = Details.numInteractions;
            if (m != Details.consumers.Count)
                throw new Exception("num edge sources and targets do not match");

            // numberText.text = (Details.idx+1).ToString();
            title.text = Details.title;
            description.text = Details.description;

            target1.text = Details.targetScore1.ToString();
            target2.text = Details.targetScore2.ToString();

            int score = GameManager.Instance.GetLevelHighScore(Details.idx);
            highScore.text = score.ToString();

            if (score >= 0)
            {
                Unlock();
            }
            int numStars = 0;
            if (score >= 1)
            {
                numStars += 1;
            }
            if (score >= details.targetScore1)
            {
                numStars += 1;
                target1.color = Color.grey;
            }
            if (score >= details.targetScore2)
            {
                numStars += 1;
                target2.color = Color.grey;
            }
            starsImage.sprite = starSprites[numStars];

            // targetSize = GetComponent<RectTransform>().sizeDelta;
            // if (thumbnailedParent == null)
            //     thumbnailedParent = transform.parent.GetComponent<RectTransform>();
        }

        //////////////////////////////
        // animations states

        enum State { Locked=-1, Thumbnail=0, Card=1, FinishFlag=2, Navigation=3 }

        // TODO: fun animation here to draw eye towards next level
        public void Unlock()
        {
            GetComponent<Animator>().SetInteger("State", (int)State.Thumbnail);
        }

        bool tweening =false;
        IEnumerator TweenToZeroPos(float duration)
        {
            while (tweening)
            {
                yield return null;
            }
            tweening = true;
            Vector3 startPos = transform.localPosition;
            float startTime = Time.time;
            while (Time.time < startTime+duration)
            {
                float t = (Time.time-startTime)/duration;
                if (t < .5f)
                {
                    t = 2*t*t;
                }
                else
                {
                    t = -1 + (4-2*t)*t;
                }
                transform.localPosition = Vector3.Lerp(startPos, Vector3.zero, t);
                yield return null;
            }
            tweening = false;
        }

        Transform thumbnailedParent;
        public void ShowThumbnail()
        {
            ShowThumbnail(.5f);
        }
        public void ShowThumbnail(float tweenTime)
        {
            transform.SetParent(thumbnailedParent, true);
            transform.localScale = Vector3.one;
            StartCoroutine(TweenToZeroPos(tweenTime));

            GetComponent<Animator>().SetInteger("State", (int)State.Thumbnail);
            OnThumbnailed?.Invoke();
        }
        public void ShowCard()
        {
            if (GameManager.Instance.CardParent.childCount > 1)
                throw new Exception("more than one card?");
            if (GameManager.Instance.CardParent.childCount == 1)
                GameManager.Instance.CardParent.GetComponentInChildren<Level>().ShowThumbnail();

            thumbnailedParent = transform.parent.GetComponent<RectTransform>();
            transform.SetParent(GameManager.Instance.CardParent, true);
            transform.localScale = Vector3.one;
            StartCoroutine(TweenToZeroPos(.5f));

            GetComponent<Animator>().SetInteger("State", (int)State.Card);
            OnCarded?.Invoke();
        }
        public void ShowFinishFlag()
        {
            GetComponent<Animator>().SetInteger("State", (int)State.FinishFlag);
            Instantiate(fireworks, transform);
        }
        // called when game is ended
        public void ShowNavigation()
        {
            if (GameManager.Instance.NavParent.transform.childCount > 0)
                throw new Exception("more than one navigation?");

            transform.SetParent(GameManager.Instance.NavParent, true);
            transform.localScale = Vector3.one;
            StartCoroutine(TweenToZeroPos(1f));

            GetComponent<Animator>().SetInteger("State", (int)State.Navigation);
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

        Tutorial teacher;
        public void Play()
        {
            thumbnailedParent = GameManager.Instance.PlayParent;
            ShowThumbnail(1.5f);
            GameManager.Instance.PlayLevel(this);
            StartCoroutine(WaitThenEnableQuitReplay());
        }
        public void StartTutorialIfAvailable()
        {
            if (tutorial != null)
                teacher = Instantiate(tutorial, GameManager.Instance.TutParent);
        }
        void OnDestroy()
        {
            if (teacher != null)
                Destroy(teacher.gameObject);
        }

        // necessary because there is no separate 'playing' state
        // but the card requires something different
        IEnumerator WaitThenEnableQuitReplay()
        {
            playButton.gameObject.SetActive(false);
            yield return new WaitForSeconds(.5f);
            quitButton.gameObject.SetActive(true);
            replayButton.gameObject.SetActive(true);
        }
        public void BackToMenu()
        {
            // TODO: 'are you sure' option
            GameManager.Instance.ReturnToMenu();
        }


        public int CurrentScore { private get; set; }
        public void FinishLevel()
        {
            if (GameManager.Instance.PlayedLevel != this)
                throw new Exception("Played level different from one being finished?");

            if (CurrentScore > GameManager.Instance.GetLevelHighScore(details.idx))
            {
                print("TODO: congratulation message for getting a high score");
            }
            GameManager.Instance.SavePlayedLevelHighScore(CurrentScore); // updates score and should unlock next level
            if (nextLevel != null)
            {
                NextLevel = Instantiate(nextLevel, nextLevelParent);
            }
            else
            {
                print("TODO: credits?");
            }
            ShowNavigation();
            OnFinished?.Invoke();
        }

        public void Replay()
        {
            if (NextLevel != null) // if replay from finish
            {
                Destroy(NextLevel);
            }
            if (teacher != null)
            {
                Destroy(teacher.gameObject);
            }
            // thumbnailedParent = GameManager.Instance.PlayParent;
            ShowThumbnail(1.5f);
            GameManager.Instance.PlayLevel(this);
            // StartCoroutine(WaitThenEnableQuitReplay());
        }
    }
}