using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.UI
{
    public class Score : MonoBehaviour
    {
        public event Action<bool> OnProducersAvailable;
        public event Action<bool> OnConsumersAvailable;
        public event Action OnLevelCompleted;
        public event Action OnLevelCompletabled, OnLevelIncompletabled;

        [SerializeField] Animator star1, star2, star3;
        [SerializeField] Constraints constraints;
        [SerializeField] ReportCard report;

        [SerializeField] Image scoreCurrentImage, scoreTargetImage;
        [SerializeField] Sprite targetSprite1, targetSprite2;
        [SerializeField] TMPro.TextMeshProUGUI scoreText, scoreTargetText; //, abundanceText;

        void Start()
        {
            var level = GameManager.Instance.PlayedLevel;
            if (level == null)
            {
                constraints.Unconstrain("Leaf");
                constraints.Unconstrain("Paw");
                constraints.Unconstrain("Count");
                constraints.Unconstrain("Chain");
                constraints.Unconstrain("Loop");
            }
            else
            {
                constraints.Constrain("Leaf", level.Details.numProducers);
                constraints.Constrain("Paw", level.Details.numConsumers);
                constraints.Constrain("Count", level.Details.minEdges);
                constraints.Constrain("Chain", level.Details.minChain);
                constraints.Constrain("Loop", level.Details.minLoop);
            }
            feasibleScoreCol = scoreText.color;
            infeasibleScoreCol = new Color(.7f,.7f,.7f,.8f); // TODO: magic number
            scoreText.color = infeasibleScoreCol;

            if (level == null) // only for test
            {
                DisableFinish();
                return;
            }
            target1 = level.Details.targetScore1;
            target2 = level.Details.targetScore2;

            // TODO: check if this needs to be unattached in OnDestroy()
            GameManager.Instance.PlayedLevel.OnFinished += CompleteLevel;
        }
        // void OnDestroy()
        // {
        //     if (GameManager.Instance != null && GameManager.Instance.PlayedLevel != null)
        //         GameManager.Instance.PlayedLevel.OnFinished -= CompleteLevel; // not sure if necessary?
        // }

        int target1, target2;
        Color feasibleScoreCol, infeasibleScoreCol;

        Func<bool> CanFinish = ()=>false;
        public void AllowLevelFinishWhen(Func<bool> Allowed)
        {
            CanFinish = ()=> Allowed() && !finishDisabled;
        }

        HashSet<int> producers = new HashSet<int>();
        HashSet<int> consumers = new HashSet<int>();
        public void AddType(int idx, bool isProducer)
        {
            if (isProducer)
            {
                // this will have to be changed if we want to let species switch type
                producers.Add(idx);

                constraints.Display("Leaf", producers.Count);
                if (constraints.GetThreshold("Leaf") > 0 && constraints.IsSatisfied("Leaf"))
                    OnProducersAvailable.Invoke(false);
            }
            else
            {
                consumers.Add(idx);

                constraints.Display("Paw", consumers.Count);
                if (constraints.GetThreshold("Paw") > 0 && constraints.IsSatisfied("Paw"))
                    OnConsumersAvailable.Invoke(false);
            }
        }
        public void RemoveIdx(int idx)
        {
            if (producers.Contains(idx))
            {
                producers.Remove(idx);

                if (constraints.GetThreshold("Leaf") > 0 && constraints.IsSatisfied("Leaf"))
                    OnProducersAvailable.Invoke(true);
                constraints.Display("Leaf", producers.Count);
            }
            else
            {
                consumers.Remove(idx);

                if (constraints.GetThreshold("Paw") > 0 && constraints.IsSatisfied("Paw"))
                    OnConsumersAvailable.Invoke(true);
                constraints.Display("Paw", consumers.Count);

            }
        }
        public void DisplayDisjoint(bool isDisjoint)
        {
			constraints.Disjoint = isDisjoint;
        }
        public void DisplayNumEdges(int numEdges)
        {
            constraints.Display("Count", numEdges);
        }
        public void DisplayMaxChain(int lenChain)
        {
            constraints.Display("Chain", lenChain);
        }
        public void DisplayMaxLoop(int lenLoop)
        {
            constraints.Display("Loop", lenLoop);
        }
        public void DisplayFeastability(bool isFeasible, bool isStable)
        {
            constraints.Feasible = isFeasible;
			constraints.Stable = isStable;
        }

		//////////////////////
		// score calculation

        int modelScore, realisedScore;
        int numStars;
        string scoreExplanation = null;
        public void DisplayScore(float score, string explanation)
        {
            modelScore = (int)score;
            scoreText.text = modelScore.ToString();
            report.SetMessage(explanation);
        }
        void Update()
        {
            if (modelScore > realisedScore)
                scoreText.color = Color.green;
            else if (modelScore < realisedScore)
                scoreText.color = Color.red;

            realisedScore = modelScore;

            int newNumStars = 0;
            if (!constraints.Disjoint && constraints.Feasible && // stable &&
                constraints.IsSatisfied("Leaf") &&
                constraints.IsSatisfied("Paw") &&
                constraints.IsSatisfied("Count") &&
                constraints.IsSatisfied("Chain") &&
                constraints.IsSatisfied("Loop"))
            {
                newNumStars += 1;

                if (modelScore >= target1)
                {
                    newNumStars += 1;
                    if (modelScore >= target2)
                    {
                        newNumStars += 1;
                    }
                }
                scoreText.color = Color.Lerp(scoreText.color, feasibleScoreCol, .01f);
            }
            else
            {
                scoreText.color = Color.Lerp(scoreText.color, infeasibleScoreCol, .3f);
            }
            star1.SetBool("Filled", newNumStars>=1);
            star2.SetBool("Filled", newNumStars>=2);
            star3.SetBool("Filled", newNumStars>=3);

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


            if (!CanFinish()) // only throw events if nothing else is busy
                return;

            GameManager.Instance.PlayedLevel.CurrentScore = realisedScore;
            if (numStars == 0 && newNumStars > 0)
            {
                // scoreText.color = Color.green;
                OnLevelCompletabled?.Invoke();
                GameManager.Instance.PlayedLevel.ShowFinishFlag();
            }
            else if (numStars > 0 && newNumStars == 0)
            {
                // scoreText.color = infeasibleScoreCol;
                OnLevelIncompletabled?.Invoke();
                GameManager.Instance.PlayedLevel.ShowThumbnail();
            }
            numStars = newNumStars;
        }

        private void CompleteLevel()
        {
            if (numStars < 1 || numStars > 3)
                throw new Exception("cannot pass with less than 0 or more than 3 stars");

            GetComponent<Animator>().SetBool("Visible", false);
            star1.SetTrigger("Confetti");
            star2.SetTrigger("Confetti");
            star3.SetTrigger("Confetti");

            OnLevelCompleted.Invoke();
        }


        ///////////////////////
        // stuff for tutorials

        public void HideScore(bool hidden=true)
        {
            GetComponent<Animator>().enabled = !hidden;
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