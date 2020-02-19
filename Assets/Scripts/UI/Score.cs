using System;
using System.Collections.Generic;
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
        float currentScore = 0;
        int realisedScore = 0;

        // to fetch the score
        List<Func<float>> ScoreSources = new List<Func<float>>();
        public void AttachScoreSource(Func<float> Normalised, float multiplier)
        {
            ScoreSources.Add(()=> multiplier * Normalised());
        }
        // to check whether constraints have been met
        Func<bool> CheckSatisfied = ()=>false;
        public void AttachConstraintsSatisfied(Func<bool> IsSatisfied)
        {
            CheckSatisfied = IsSatisfied;
        }
        // this needs to be here to ensure that the calculated components
        // are synced before moving every frame
        // we want the score to update even if the model is calculating
        // but no events triggered in case of false positive due to being out of sync
        Func<bool> CheckValid = ()=>false;
        public void AttachScoreValidity(Func<bool> IsValid)
        {
            CheckValid = IsValid;
        }
        public void Update() //, string explanation, string explanationSupplement)
        {
            float newScore = 0;
            foreach (var Source in ScoreSources) {
                newScore += Source.Invoke();
            }
            Assert.IsTrue(newScore >= 0, "cannot have negative score");

            if (newScore > currentScore) {
                scoreText.color = new Color(.2f,.8f,.2f);
            } else if (newScore < currentScore) {
                scoreText.color = new Color(.9f,.1f,.1f);
            } 
            currentScore = newScore;

            realisedScore = (int)currentScore;
            scoreText.text = realisedScore.ToString();

            // only continue if allowed
            if (starsDisabled || !CheckValid.Invoke()) {
                return;
            }
            int newNumStars = 0;
            if (CheckSatisfied.Invoke())
            {
                newNumStars += 1;

                if (realisedScore >= target1)
                {
                    newNumStars += 1;
                    if (realisedScore >= target2) {
                        newNumStars += 1;
                    }
                }
                scoreText.color = Color.Lerp(scoreText.color, feasibleScoreCol, .5f*Time.deltaTime);
            }
            else
            {
                scoreText.color = Color.Lerp(scoreText.color, infeasibleScoreCol, 5f*Time.deltaTime);
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
        }

        public void ShowResults(int prevScore, int globalMedian)
        {
            report.ShowResults(HighestStars, HighestScore, prevScore, globalMedian);
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