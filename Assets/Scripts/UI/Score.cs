using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace EcoBuilder.UI
{
    public class Score : MonoBehaviour
    {
        public event Action OnLevelCompletabled;
        public event Action OnThreeStarsAchieved;

        [SerializeField] Animator star1, star2, star3;
        [SerializeField] ReportCard report;

        [SerializeField] Image scoreCurrentImage, scoreTargetImage;
        [SerializeField] Sprite targetSprite1, targetSprite2;
        [SerializeField] TMPro.TextMeshProUGUI scoreText, scoreTargetText;

        [SerializeField] TMPro.TextMeshProUGUI resultsPlay, resultsTop, resultsWorldAvg;

        int target1, target2;
        [SerializeField] Color feasibleScoreCol, infeasibleScoreCol;
        void Awake()
        {
            feasibleScoreCol = scoreText.color;
            scoreText.color = infeasibleScoreCol;
        }
        public void SetStarThresholds(LevelDetails.ScoreMetric metric, int target1, int target2)
        {
            this.target1 = target1;
            this.target2 = target2;
        }

		//////////////////////
		// score calculation

        public int HighestScore { get; private set; } = 0;
        public int HighestStars { get; private set; } = 0;
        float totalScore = 0;
        int realisedScore = 0;

        [SerializeField] float scoreMultiplier, supplementMultiplier;

        public void UpdateScore(float score, float supplement)//, string explanation, string explanationSupplement)
        {
            Assert.IsTrue(score >= 0, "cannot have negative score");
            float newTotalScore = score*scoreMultiplier + supplement*supplementMultiplier;

            if (newTotalScore > totalScore) {
                scoreText.color = new Color(.2f,.8f,.2f);
            } else if (newTotalScore < totalScore) {
                scoreText.color = new Color(.9f,.1f,.1f);
            }
            totalScore = newTotalScore;

            realisedScore = (int)totalScore;
            scoreText.text = realisedScore.ToString();
            // print("TODO: score explanation and highest score on tap/hover");
            // report.SetMessage($"{explanation} + {explanationSupplement} = {totalScore}");
        }

        public void UpdateStars(bool scoreValid)
        {
            if (starsDisabled) {
                return;
            }
            int newNumStars = 0;
            if (scoreValid)
            {
                newNumStars += 1;

                if (realisedScore >= target1)
                {
                    newNumStars += 1;
                    if (realisedScore >= target2) {
                        newNumStars += 1;
                    }
                }
                scoreText.color = Color.Lerp(scoreText.color, feasibleScoreCol, .3f*Time.deltaTime);
            }
            else
            {
                scoreText.color = Color.Lerp(scoreText.color, infeasibleScoreCol, 3f*Time.deltaTime);
            }
            if (newNumStars < 2)
            {
                scoreTargetText.text = target1.ToString();
                scoreTargetImage.sprite = targetSprite1;
            }
            else
            {
                scoreTargetText.text = target2.ToString();
                scoreTargetImage.sprite = targetSprite2;
            }
            star1.SetBool("Filled", newNumStars>=1);
            star2.SetBool("Filled", newNumStars>=2);
            star3.SetBool("Filled", newNumStars==3);

            // do not set off events yet if finish is disabled
            if (HighestStars == 0 && newNumStars > 0) {
                OnLevelCompletabled?.Invoke();
            }
            // always call onthreestarsachieved second
            if (HighestStars <= 2 && newNumStars == 3) {
                OnThreeStarsAchieved?.Invoke();
            }
            HighestStars = Math.Max(HighestStars, newNumStars);
            HighestScore = Math.Max(HighestScore, realisedScore);
        }

        bool starsDisabled;
        public void DisableStarCalculation(bool disabled)
        {
            starsDisabled = disabled;
        }

        public void Finish()
        {
            Assert.IsFalse(HighestStars < 1 || HighestStars > 3, "cannot pass with less than 1 or more than 3 stars");

            GetComponent<Animator>().SetBool("Visible", false);
            star1.SetTrigger("Confetti");
            star2.SetTrigger("Confetti");
            star3.SetTrigger("Confetti");
        }


        ///////////////////////
        // stuff for tutorials

        public void Hide(bool hidden=true)
        {
            GetComponent<Animator>().enabled = !hidden;
            if (hidden) {
                transform.localPosition = new Vector2(0,1000); // hack
            }
        }
    }
}