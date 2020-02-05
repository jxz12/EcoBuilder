using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.UI
{
    public class Score : MonoBehaviour
    {
        public event Action OnLevelCompletabled;
        public event Action OnThreeStarsAchieved;

        [SerializeField] Animator star1, star2, star3;
        [SerializeField] Constraints constraints;
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
        public void SetStarThresholds(int target1, int target2)
        {
            this.target1 = target1;
            this.target2 = target2;
        }
        public void Finish()
        {
            if (HighestStars < 1 || HighestStars > 3) {
                throw new Exception("cannot pass with less than 0 or more than 3 stars");
            }

            GetComponent<Animator>().SetBool("Visible", false);
            star1.SetTrigger("Confetti");
            star2.SetTrigger("Confetti");
            star3.SetTrigger("Confetti");
            print("TODO: another confetti for constraint score instead");

            constraints.Show(false);
        }


		//////////////////////
		// score calculation

        public int HighestScore { get; private set; } = 0;
        public int HighestStars { get; private set; } = 0;
        int currentScore;
        float normalisedScore;
        public void UpdateScore(float newNormalisedScore, string explanation)
        {
            if (newNormalisedScore > normalisedScore) {
                scoreText.color = new Color(.2f,.8f,.2f);
            } else if (newNormalisedScore < normalisedScore) {
                scoreText.color = new Color(.9f,.1f,.1f);
            }
            normalisedScore = newNormalisedScore;

            print("TODO: deal with possible score of 0?");
            currentScore = (int)(normalisedScore * 1000);
            scoreText.text = currentScore.ToString();
            report.SetMessage(explanation);
        }
        public void UpdateStars()
        {
            if (starsDisabled) {
                return;
            }
            int newNumStars = 0;
            if (!constraints.Disjoint &&
                constraints.Feasible &&
                constraints.IsSatisfied("Leaf") &&
                constraints.IsSatisfied("Paw") &&
                constraints.IsSatisfied("Count") &&
                constraints.IsSatisfied("Chain") &&
                constraints.IsSatisfied("Loop"))
            {
                newNumStars += 1;

                if (currentScore >= target1)
                {
                    newNumStars += 1;
                    if (currentScore >= target2) {
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

            // do not set off events yet if finish is disabled
            if (HighestStars == 0 && newNumStars > 0) {
                OnLevelCompletabled?.Invoke();
            }
            // always call onthreestarsachieved second
            if (HighestStars <= 2 && newNumStars == 3) {
                OnThreeStarsAchieved?.Invoke();
            }
            HighestStars = Math.Max(HighestStars, newNumStars);
            HighestScore = Math.Max(HighestScore, currentScore);
            star1.SetBool("Filled", HighestStars>=1);
            star2.SetBool("Filled", HighestStars>=2);
            star3.SetBool("Filled", HighestStars==3);
        }
        public void UseConstraintAsScoreInstead(string constraintName)
        {
            constraints.GetValue(constraintName);
            print("TODO: this");
        }

        bool starsDisabled;
        public void DisableStarCalculation(bool disabled)
        {
            starsDisabled = disabled;
        }

        ///////////////////////
        // stuff for tutorials

        public void HideScore(bool hidden=true)
        {
            GetComponent<Animator>().enabled = !hidden;
            if (hidden) {
                transform.localPosition = new Vector2(0,1000); // hack
            }
        }
        public void HideConstraints(bool hidden=true)
        {
            constraints.gameObject.SetActive(!hidden);
        }
    }
}