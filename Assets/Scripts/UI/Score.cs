using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.UI
{
    public class Score : MonoBehaviour
    {
        public event Action OnLevelCompletabled, OnLevelIncompletabled;

        [SerializeField] Animator star1, star2, star3;
        [SerializeField] Constraints constraints;
        [SerializeField] ReportCard report;

        [SerializeField] Image scoreCurrentImage, scoreTargetImage;
        [SerializeField] Sprite targetSprite1, targetSprite2;
        [SerializeField] TMPro.TextMeshProUGUI scoreText, scoreTargetText;


        int target1, target2;
        Color feasibleScoreCol, infeasibleScoreCol;
        void Awake()
        {
            feasibleScoreCol = scoreText.color;
            infeasibleScoreCol = new Color(.7f,.7f,.7f,.8f); // TODO: magic numbers
            scoreText.color = infeasibleScoreCol;
        }
        public void SetScoreThresholds(int target1, int target2)
        {
            this.target1 = target1;
            this.target2 = target2;
        }
        public void CompleteLevel()
        {
            if (numStars < 1 || numStars > 3)
                throw new Exception("cannot pass with less than 0 or more than 3 stars");

            GetComponent<Animator>().SetBool("Visible", false);
            star1.SetTrigger("Confetti");
            star2.SetTrigger("Confetti");
            star3.SetTrigger("Confetti");

            constraints.Show(false);
        }


		//////////////////////
		// score calculation

        public int NormalisedScore { get; private set; }
        float modelScore;
        int numStars, newNumStars;
        string scoreExplanation = null;
        public void DisplayScore(float newModelScore, string explanation)
        {
            report.SetMessage(explanation);

            if (newModelScore > modelScore)
                scoreText.color = Color.green;
            else if (newModelScore < modelScore)
                scoreText.color = Color.red;

            modelScore = newModelScore;
            NormalisedScore = (int)modelScore; // TODO: better normalisation plz
            scoreText.text = NormalisedScore.ToString();

        }
        void Update()
        {
            newNumStars = 0;
            if (!constraints.Disjoint &&
                constraints.Feasible &&
                constraints.IsSatisfied("Leaf") &&
                constraints.IsSatisfied("Paw") &&
                constraints.IsSatisfied("Count") &&
                constraints.IsSatisfied("Chain") &&
                constraints.IsSatisfied("Loop"))
            {
                newNumStars += 1;

                if (NormalisedScore >= target1)
                {
                    newNumStars += 1;
                    if (NormalisedScore >= target2)
                    {
                        newNumStars += 1;
                    }
                }
                scoreText.color = Color.Lerp(scoreText.color, feasibleScoreCol, .1f*Time.deltaTime);
            }
            else
            {
                scoreText.color = Color.Lerp(scoreText.color, infeasibleScoreCol, 3f*Time.deltaTime);
            }
            star1.SetBool("Filled", newNumStars>=1);
            star2.SetBool("Filled", newNumStars>=2);
            star3.SetBool("Filled", newNumStars==3);

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
        }
        public void UpdateScore() // should only be called from outside when ready
        {
            if (numStars == 0 && newNumStars > 0)
            {
                OnLevelCompletabled?.Invoke();
            }
            else if (numStars > 0 && newNumStars == 0)
            {
                OnLevelIncompletabled?.Invoke();
            }
            numStars = newNumStars;
        }


        ///////////////////////
        // stuff for tutorials

        public void HideScore(bool hidden=true)
        {
            GetComponent<Animator>().enabled = !hidden;
            if (hidden)
                transform.localPosition = new Vector2(0,1000); // hack
            // else
            //     transform.localPosition = Vector2.zero;
        }
        public void HideConstraints(bool hidden=true)
        {
            constraints.gameObject.SetActive(!hidden);
        }
        bool finishDisabled = false;
        public void DisableFinish(bool disabled=true)
        {
            finishDisabled = disabled;
        }
    }
}