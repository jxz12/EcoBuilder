using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

namespace EcoBuilder.Levels
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
        public int numInitSpecies;
        public List<bool> plants;
        public List<int> randomSeeds;
        public List<float> sizes;
        public List<float> greeds;
        public int numInteractions;
        public List<int> resources;
        public List<int> consumers;

        // traits
        public bool sizeSliderHidden;
        public bool greedSliderHidden;
        public bool conflictsAllowed;
        public bool superfocusAllowed;
        public List<bool> sizeEditables;
        public List<bool> greedEditables;

        // score
        public int targetScore1;
        public int targetScore2;
        public string alternateScore;
    }
    public class Level : MonoBehaviour
    {
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
            int n = Details.numInitSpecies;
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

            int score = GameManager.Instance.GetPlayerHighScore(Details.idx);
            highScore.text = score.ToString();

            if (score >= 0)
            {
                GetComponent<Animator>().SetInteger("State", (int)State.Thumbnail);
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

        }

        //////////////////////////////
        // animations states

        enum State { Locked=-1, Thumbnail=0, Card=1, FinishFlag=2, Navigation=3, Leaving=4 }

        IEnumerator tweenRoutine;
        IEnumerator TweenToZeroPosFrom(float duration, Transform newParent)
        {
            while (tweenRoutine != null)
            {
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
                if (t < .5f)
                    t = 2*t*t;
                else
                    t = -1 + (4-2*t)*t;

                transform.localPosition = Vector3.Lerp(startPos, Vector3.zero, t);
                yield return null;
            }
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
            if (GameManager.Instance.CardParent.childCount > 1)
                throw new Exception("more than one card?");
            if (GameManager.Instance.CardParent.childCount == 1)
                GameManager.Instance.CardParent.GetComponentInChildren<Level>().ShowThumbnail();

            thumbnailedParent = transform.parent.GetComponent<RectTransform>();
            StartCoroutine(TweenToZeroPosFrom(.5f, GameManager.Instance.CardParent));

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

            StartCoroutine(TweenToZeroPosFrom(1f, GameManager.Instance.NavParent));

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
            GameManager.Instance.PlayLevel(this);
            GameManager.Instance.OnLoaded.AddListener(MoveToCornerOnPlay);
            // TODO: loading message!
        }
        void MoveToCornerOnPlay(string sceneName)
        {
            if (sceneName != "Play")
                throw new Exception("Play scene not loaded when expected");

            GameManager.Instance.OnLoaded.RemoveListener(MoveToCornerOnPlay);
            thumbnailedParent = GameManager.Instance.PlayParent; // move to corner
            GameManager.Instance.ShowHelpText(2f, details.introduction); // show intro

            ShowThumbnail(1.5f);
            StartCoroutine(WaitThenEnableQuitReplay(1.5f));
        }
        // necessary because there is no separate 'playing' state
        // but the card requires something different
        IEnumerator WaitThenEnableQuitReplay(float waitTime)
        {
            playButton.gameObject.SetActive(false);
            yield return new WaitForSeconds(waitTime);
            quitButton.gameObject.SetActive(true);
            replayButton.gameObject.SetActive(true);
        }

        public void StartTutorialIfAvailable()
        {
            if (tutorial != null)
                teacher = Instantiate(tutorial, GameManager.Instance.TutParent);
        }
        void OnDestroy()
        {
            if (teacher != null)
                teacher.DestroyMe();
        }

        public void BackToMenu()
        {
            // TODO: 'are you sure' option
            GameManager.Instance.ReturnToMenu();
            GameManager.Instance.OnLoaded.AddListener(DestroyWhenMenuLoads);
        }
        void DestroyWhenMenuLoads(string sceneName)
        {
            if (sceneName != "Menu")
                throw new Exception("Menu scene not loaded when expected");

            GameManager.Instance.OnLoaded.RemoveListener(DestroyWhenMenuLoads);
            Destroy(gameObject);
        }


        public void FinishLevel()
        {
            if (GameManager.Instance.PlayedLevel != this)
                throw new Exception("Played level different from one being finished?");

            if (nextLevel != null)
            {
                NextLevel = Instantiate(nextLevel, nextLevelParent);
            }
            else
            {
                print("TODO: credits? reduce width of navigation?");
            }
            ShowNavigation();
            // TODO: make the thing show at right height here
            GameManager.Instance.ShowHelpText(2f, details.congratulation);
            OnFinished?.Invoke();
        }
        public void SaveLevel(int score)
        {
            if (score > GameManager.Instance.GetPlayerHighScore(details.idx))
            {
                print("TODO: congratulation message for getting a high score");
            }
            GameManager.Instance.SavePlayedLevelHighScore(score); // updates score and should unlock next level
        }


        public void Replay()
        {
            if (NextLevel != null) // if replay from finish
            {
                Destroy(NextLevel);
            }
            if (teacher != null)
            {
                teacher.DestroyMe();
            }
            // thumbnailedParent = GameManager.Instance.PlayParent;
            // ShowThumbnail(1.5f);
            // GameManager.Instance.PlayLevel(this);
            // StartCoroutine(WaitThenEnableQuitReplay());
            Play();
        }

        public static void SaveAsNewPrefab(LevelDetails detail, string name)
        {
            #if !UNITY_EDITOR
            throw new Exception("cannot save level outside editor");
            #endif

            var level = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Levels/Level.prefab"));
            // var level = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Levels/Level.prefab"));
            level.GetComponent<Level>().details = detail;
            bool success;
            PrefabUtility.SaveAsPrefabAsset(level, "Assets/Prefabs/Levels/"+name+".prefab", out success);
            Destroy(level);
        }
        public static Level GetDefaultLevel()
        {
            // return AssetDatabase.LoadAssetAtPath<Level>("Assets/Prefabs/Levels/Level.prefab");
            return AssetDatabase.LoadAssetAtPath<Level>("Assets/Prefabs/Levels/Learning 8.prefab");
        }
    }
}