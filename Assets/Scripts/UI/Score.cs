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

        [SerializeField] Image scoreCurrentImage, scoreTargetImage;
        [SerializeField] Sprite targetSprite1, targetSprite2;
        [SerializeField] TMPro.TextMeshProUGUI scoreText, scoreTargetText; //, abundanceText;

        void Start()
        {
            var level = GameManager.Instance.PlayedLevel;
            if (level == null || level.Details.title == "Sandbox") // FIXME:
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

        Func<bool> CanUpdate;
        public void AllowUpdateWhen(Func<bool> Allowed)
        {
            CanUpdate = Allowed;
        }
        public void HideScore(bool hidden=true)
        {
            gameObject.SetActive(!hidden);
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


        // float targetAbundance, abundance;
        int modelScore, realisedScore;
        string scoreExplanation = null;
        public void DisplayScore(float score, string explanation)
        {
            // modelScore = (int)Math.Truncate(score * 100);
            modelScore = (int)(score * 10);
            scoreExplanation = explanation;
        }
        public void CycleScoreSprite()
        {
            scoreCurrentImage.transform.Rotate(new Vector3(0,0,-90));
        }

        // public int NumStars { get; private set; }
        int NumStars { get; set; }
		void Update()
		{
            if (modelScore > realisedScore)
                scoreText.color = Color.green;
            else if (modelScore < realisedScore)
                scoreText.color = Color.red;

            realisedScore = modelScore;
            scoreText.text = realisedScore.ToString();

            star1.SetBool("Filled", false);
            star2.SetBool("Filled", false);
            star3.SetBool("Filled", false);

            int newNumStars = 0;
            if (!constraints.Disjoint && constraints.Feasible && // stable &&
                constraints.IsSatisfied("Leaf") &&
                constraints.IsSatisfied("Paw") &&
                constraints.IsSatisfied("Count") &&
                constraints.IsSatisfied("Chain") &&
                constraints.IsSatisfied("Loop"))
            {
                newNumStars += 1;
                star1.SetBool("Filled", true);

                if (modelScore >= target1)
                {
                    newNumStars += 1;
                    star2.SetBool("Filled", true);

                    if (modelScore >= target2)
                    {
                        newNumStars += 1;
                        star3.SetBool("Filled", true);
                    }
                }
                scoreText.color = Color.Lerp(scoreText.color, feasibleScoreCol, .01f);
            }
            else
            {
                scoreText.color = infeasibleScoreCol;
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

            // don't calculate 
            if (!CanUpdate())
                return;

            if (NumStars == 0 && newNumStars > 0)
            {
                scoreText.color = Color.green;
                OnLevelCompletabled?.Invoke();
                // TODO: setting this should not be here
                if (!finishDisabled)
                    GameManager.Instance.PlayedLevel.ShowFinishFlag();
            }
            else if (NumStars > 0 && newNumStars == 0)
            {
                scoreText.color = infeasibleScoreCol;
                OnLevelIncompletabled?.Invoke();
                if (!finishDisabled)
                    GameManager.Instance.PlayedLevel.ShowThumbnail();
            }
            NumStars = newNumStars;
		}
        private void CompleteLevel()
        {
            if (NumStars < 1 || NumStars > 3)
                throw new Exception("cannot pass with less than 0 or more than 3 stars");

            // GameManager.Instance.SavePlayedLevel(NumStars, realisedScore);

            GetComponent<Animator>().SetBool("Visible", false);
            star1.SetTrigger("Confetti");
            star2.SetTrigger("Confetti");
            star3.SetTrigger("Confetti");

            OnLevelCompleted.Invoke();
        }

        public void ToggleReportCard()
        {
            print(scoreExplanation);
        }
    }
}