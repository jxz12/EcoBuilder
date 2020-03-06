using System;
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

        [SerializeField] Animator star1, star2, star3;

        [SerializeField] Image scoreCurrentImage, scoreTargetImage;
        [SerializeField] Sprite targetSprite1, targetSprite2;
        [SerializeField] TMPro.TextMeshProUGUI scoreText, scoreTargetText;

        int target1, target2;
        Color defaultScoreCol, displayedScoreCol;
        [SerializeField] Color unsatisfiedScoreCol, reminderScoreCol, higherScoreCol, lowerScoreCol;
        void Awake()
        {
            defaultScoreCol = scoreText.color;
            displayedScoreCol = unsatisfiedScoreCol;
            scoreIcon.enabled = false;
        }
        void Start()
        {
            Hide(false);
        }
        public void SetStarThresholds(LevelDetails.ScoreMetric metric, int target1, int target2)
        {
            this.target1 = target1;
            this.target2 = target2;
        }

        //////////////////////
        // score calculation

        // to fetch the score
        List<Func<float>> AttachedSources = new List<Func<float>>();
        public void AttachScoreSource(Func<float> Source)
        {
            AttachedSources.Add(Source);
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


        public int HighestScore { get; private set; } = 0;
        int displayedScore = 0;
        public int HighestStars { get; private set; } = 0;
        int displayedStars = 0;
        float latestValidScore = 0;

        public void Update() //, string explanation, string explanationSupplement)
        {
            float score = 0;
            foreach (var Source in AttachedSources) {
                score += Source.Invoke();
            }
            Assert.IsTrue(score >= 0, "cannot have negative score");

            if (score > latestValidScore) {
                displayedScoreCol = higherScoreCol;
            } else if (score < latestValidScore) {
                displayedScoreCol = lowerScoreCol;
            }

            displayedScore = (int)score;
            scoreText.text = displayedScore.ToString();

            //////////////////////////////////
            // only continue if allowed
            if (starsDisabled || !AttachedValidity.Invoke()) {
                return;
            }
            latestValidScore = score;
            displayedStars = 0;
            if (AttachedSatisfied.Invoke())
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
                int prevHighestScore = HighestScore;
                HighestScore = Math.Max(HighestScore, displayedScore);
                // do not set off events yet if finish is disabled
                if (prevHighestStars == 0 && HighestStars > 0) {
                    OnOneStarAchieved?.Invoke();
                }
                // always call OnThreeStarsAchieved second
                if (prevHighestStars <= 2 && HighestStars == 3) {
                    OnThreeStarsAchieved?.Invoke();
                }

                displayedScoreCol = Color.Lerp(displayedScoreCol, defaultScoreCol, .5f*Time.deltaTime);
            }
            else
            {
                displayedScoreCol = Color.Lerp(displayedScoreCol, unsatisfiedScoreCol, 5f*Time.deltaTime);
            }
            if (displayedStars < 2)
            {
                scoreTargetText.text = target1.ToString();
                scoreTargetImage.sprite = targetSprite1;
            }
            else
            {
                scoreTargetText.text = target2.ToString();
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
            star1.SetBool("Reminding", false);
            star2.SetBool("Reminding", false);
            star3.SetBool("Reminding", false);
            scoreIcon.enabled = HighestStars>0;
        }
        IEnumerator remindRoutine;
        bool reminding = false;
        IEnumerator RemindHighest()
        {
            Assert.IsTrue(remindDuration <= remindPeriod);
            float tStart = Time.time;
            while (true)
            {
                reminding = ((Time.time-tStart) % remindPeriod) > (remindPeriod-remindDuration);
                if (reminding) {
                    scoreText.text = $"{HighestScore}";
                } else {
                    scoreText.text = $"{displayedScore}";
                }
                scoreIcon.enabled = reminding;
                star1.SetBool("Reminding", reminding);
                star2.SetBool("Reminding", reminding);
                star3.SetBool("Reminding", reminding);
                yield return null;
            }
        }
        void LateUpdate()
        {
            scoreText.color = reminding? reminderScoreCol : displayedScoreCol;
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

            StartCoroutine(Tweens.Pivot(GetComponent<RectTransform>(), new Vector2(0,1), new Vector2(0,0)));
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