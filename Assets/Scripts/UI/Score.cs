using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace EcoBuilder.UI
{
    public class Score : MonoBehaviour
    {
        public event Action OnOneStarAchieved;
        public event Action OnThreeStarsAchieved;
        public event Action OnHighestScoreBroken; // this is used for requesting stats

        [SerializeField] Animator star1, star2, star3;

        [SerializeField] Image scoreCurrentImage, scoreTargetImage;
        [SerializeField] Sprite targetSprite1, targetSprite2;
        [SerializeField] Sprite researchWorldBackground;
        [SerializeField] TMPro.TextMeshProUGUI scoreText, scoreTargetText, statsText;

        Color defaultScoreCol, displayedScoreCol;
        RectTransform scoreRT;
        [SerializeField] Color unsatisfiedScoreCol, reminderScoreCol, higherScoreCol, lowerScoreCol;
        void Awake()
        {
            defaultScoreCol = scoreText.color;
            displayedScoreCol = unsatisfiedScoreCol;
            scoreIcon.enabled = false;
            scoreRT = scoreText.GetComponent<RectTransform>();
        }
        void Start()
        {
            Hide(false);
        }
        long target1=0, target2=0;
        long prevHighScore=0;
        public void SetStarThresholds(long? prevHighScore, long target1, long target2)
        {
            statsObject.SetActive(false);
            starsObject.SetActive(true);
            this.target1 = target1;
            this.target2 = target2;

            this.prevHighScore = prevHighScore ?? 0; // this means highscore is viewable regardless of finish flag
        }
        [SerializeField] GameObject starsObject, statsObject;
        public void EnableStatsText(long? prevHighScore)
        {
            // Assert.IsTrue(rankCheckDelay > 1, "trying to check too often");
            GetComponent<Image>().sprite = researchWorldBackground;
            starsObject.SetActive(false);
            statsObject.SetActive(true);

            target1 = target2 = this.prevHighScore = prevHighScore ?? 0; // this means highscore is viewable regardless of finish flag

            LastStatsRank = null;
            OnHighestScoreBroken?.Invoke(); // request a rank at first
        }
        public string LastStatsRank { get; private set; }
        [SerializeField] float rankCheckDelay;
        float lastHighScoreTime = 0;
        public void SetStatsText(string rank, long? median)
        {
            Assert.IsTrue(statsObject.activeSelf);
            var sb = new StringBuilder();
            if (rank != null) {
                sb.Append($"Your Rank: {rank}");
            } else {
                sb.Append($"Currently offline");
            }
            if (median != null) {
                sb.Append($"\nAverage: {((long)median).ToString("N0")}");
            }
            statsText.text = sb.ToString();
            LastStatsRank = rank;
        }

        //////////////////////
        // score calculation

        // to fetch the score
        List<Func<double>> AttachedSources = new List<Func<double>>();
        List<Func<double,string>> AttachedDescriptions = new List<Func<double,string>>();
        public void AttachScoreSource(Func<double> Source, Func<double,string> Description)
        {
            Assert.IsNotNull(Source);
            AttachedSources.Add(Source);
            AttachedDescriptions.Add(Description);
        }
        // to check whether constraints have been met
        Func<bool> AttachedSatisfied = ()=>false;
        public void AttachConstraintsSatisfied(Func<bool> IsSatisfied)
        {
            AttachedSatisfied = IsSatisfied;
        }
        // this needs to be here to ensure that the calculated components
        // are synced before moving every frame
        // we want the score to update even if the model is calculating
        // but no events triggered in case of false positive due to being out of sync
        Func<bool> AttachedValidity = ()=>false;
        public void AttachScoreValidity(Func<bool> IsValid)
        {
            AttachedValidity = IsValid;
        }
        public string GetDescription()
        {
            Assert.IsTrue(AttachedSources.Count == AttachedDescriptions.Count);
            if (AttachedSources.Count == 0) {
                return null;
            }
            var sb = new StringBuilder();
            sb.Append("Your score was comprised of ").Append(AttachedDescriptions[0].Invoke(AttachedSources[0].Invoke()));
            for (int i=1; i<AttachedSources.Count; i++)
            {
                sb.Append(", plus ").Append(AttachedDescriptions[i].Invoke(AttachedSources[i].Invoke()));
            }
            sb.Append(".");
            return sb.ToString();
        }

        public long HighestScore { get; private set; } = 0;
        long displayedScore = 0;
        public int HighestStars { get; private set; } = 0;
        int displayedStars = 0;
        double latestValidScore = 0;

        public void Update()
        {
            Assert.IsFalse(starsObject.activeSelf && statsObject.activeSelf);
            double score = 0;
            foreach (var Source in AttachedSources) {
                score += Source.Invoke();
            }
            Assert.IsTrue(score >= 0, "cannot have negative score");

            if (score > latestValidScore) {
                displayedScoreCol = higherScoreCol;
                ResetRemindCycle();
            } else if (score < latestValidScore) {
                displayedScoreCol = lowerScoreCol;
                ResetRemindCycle();
            }

            displayedScore = (long)score;

            //////////////////////////////////
            // only continue if allowed
            if (starsDisabled || (!AttachedValidity?.Invoke() ?? false)) {
                return;
            }
            latestValidScore = score;
            displayedStars = 0;
            if (AttachedSatisfied?.Invoke() ?? false)
            {
                displayedStars += 1;
                if (displayedScore >= target1) {
                    displayedStars += 1;
                    if (displayedScore >= target2) {
                        displayedStars += 1;
                    }
                }

                int prevHighestStars = HighestStars;
                HighestStars = Math.Max(HighestStars, displayedStars);
                long prevHighestScore = HighestScore;
                HighestScore = Math.Max(HighestScore, displayedScore);
                // do not set off events yet if finish is disabled
                if (prevHighestStars == 0 && HighestStars > 0) {
                    OnOneStarAchieved?.Invoke();
                }
                // always call OnThreeStarsAchieved second
                if (prevHighestStars <= 2 && HighestStars == 3) {
                    OnThreeStarsAchieved?.Invoke();
                }
                if (HighestScore > prevHighestScore)
                {
                    OnHighestScoreBroken?.Invoke();
                    lastHighScoreTime = Time.time;
                }
                displayedScoreCol = Color.Lerp(displayedScoreCol, defaultScoreCol, .5f*Time.deltaTime);
            }
            else
            {
                displayedScore = 0;
                displayedScoreCol = Color.Lerp(displayedScoreCol, unsatisfiedScoreCol, 5f*Time.deltaTime);
            }
            if (displayedStars < 2)
            {
                scoreTargetText.text = target1.ToString("N0");
                scoreTargetImage.sprite = targetSprite1;
            }
            else
            {
                scoreTargetText.text = target2.ToString("N0");
                scoreTargetImage.sprite = targetSprite2;
            }
            star1.SetBool("Filled", displayedStars>=1);
            star2.SetBool("Filled", displayedStars>=2);
            star3.SetBool("Filled", displayedStars==3);

            // periodically flash the highest score if needed
            if ((displayedStars==0 && HighestStars>0) || (displayedStars>0 && displayedScore<HighestScore)) {
                StartReminding();
            } else {
                EndReminding();
            }
            // periodically request a new rank + median after long enough wait
            if (Time.time > lastHighScoreTime+rankCheckDelay)
            {
                OnHighestScoreBroken?.Invoke();
                lastHighScoreTime = Time.time;
            }
        }
        [SerializeField] Image scoreIcon;
        [SerializeField] float remindPeriod, remindDuration;
        void StartReminding()
        {
            if (remindRoutine == null) {
                StartCoroutine(remindRoutine = RemindHighest());
            }
        }
        void EndReminding()
        {
            if (remindRoutine != null) {
                StopCoroutine(remindRoutine);
                remindRoutine = null;
            }
            reminding = false;
        }
        IEnumerator remindRoutine;
        bool reminding = false;
        float tRemindStart;
        IEnumerator RemindHighest()
        {
            Assert.IsTrue(remindDuration <= remindPeriod);
            tRemindStart = Time.time;
            while (true)
            {
                reminding = ((Time.time-tRemindStart) % remindPeriod) > (remindPeriod-remindDuration);
                yield return null;
            }
        }
        void ResetRemindCycle()
        {
            tRemindStart = Time.time;
        }
        void LateUpdate()
        {
            if (remindRoutine != null)
            {
                bool hovering = RectTransformUtility.RectangleContainsScreenPoint(scoreRT, Input.mousePosition);
                scoreText.color = reminding||hovering? reminderScoreCol : displayedScoreCol;
                FormatAndDisplayScore(reminding||hovering? HighestScore : displayedScore);

                scoreIcon.enabled = reminding||hovering;
                star1.SetBool("Reminding", reminding||hovering);
                star2.SetBool("Reminding", reminding||hovering);
                star3.SetBool("Reminding", reminding||hovering);

                if (hovering) {
                    ResetRemindCycle();
                }
            }
            else
            {
                scoreText.color = displayedScoreCol;
                FormatAndDisplayScore(displayedScore);
                scoreIcon.enabled = HighestStars > 0;
            }
        }
        void FormatAndDisplayScore(long value)
        {
            scoreText.text = value.ToString("N0");
        }

        bool starsDisabled;
        public void DisableStarCalculation(bool disabled=true)
        {
            // this is separate from Hide() because of the first level lol
            starsDisabled = disabled;
        }

        public void Finish()
        {
            Assert.IsFalse(HighestStars < 1 || HighestStars > 3, "cannot pass with less than 1 or more than 3 stars");

            StartCoroutine(Tweens.Pivot(GetComponent<RectTransform>(), new Vector2(0,1), new Vector2(0,0), 1, ()=>GetComponent<Canvas>().enabled=false));
        }



        ///////////////////////
        // stuff for tutorials

        public void Hide(bool hidden=true)
        {
            GetComponent<Canvas>().enabled = !hidden;
            if (!hidden) {
                StartCoroutine(Tweens.Pivot(GetComponent<RectTransform>(), new Vector2(0,0), new Vector2(0,1)));
            }
        }
    }
}