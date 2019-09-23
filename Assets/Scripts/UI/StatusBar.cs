using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.UI
{
    public class StatusBar : MonoBehaviour
    {
        public event Action<bool> OnProducersAvailable;
        public event Action<bool> OnConsumersAvailable;
        public event Action OnLevelCompleted;


        [SerializeField] Animator star1, star2, star3;
        [SerializeField] Constraint heart, leaf, paw, edge, chain, loop;
        [SerializeField] Help help;
        [SerializeField] RectTransform levelParent;

        HashSet<int> producers = new HashSet<int>();
        HashSet<int> consumers = new HashSet<int>();

        Level constrainedFrom;
        public void ConstrainFromLevel(Level level)
        {
            leaf.Constrain(level.Details.numProducers);
            paw.Constrain(level.Details.numConsumers);

            edge.Constrain(level.Details.minEdges);
            chain.Constrain(level.Details.minChain);
            loop.Constrain(level.Details.minLoop);

            help.SetText(level.Details.introduction);

            constrainedFrom = level;
            constrainedFrom.ShowThumbnailNewParent(levelParent, Vector2.zero);
            constrainedFrom.FinishButton.onClick.AddListener(()=> OnLevelCompleted.Invoke());
        }
        void OnDestroy()
        {
            constrainedFrom.FinishButton.onClick.RemoveListener(()=> OnLevelCompleted.Invoke());
        }
        Func<bool> CanUpdate;
        public void AllowUpdateWhen(Func<bool> Allowed)
        {
            CanUpdate = Allowed;
        }
        public void ShowHelp(bool showing)
        {
            help.Show(showing);
        }
        [SerializeField] GameObject scoreParent, constraintsParent;
        public void HideScore()
        {
            scoreParent.gameObject.SetActive(false);
        }
        public void HideConstraints()
        {
            constraintsParent.gameObject.SetActive(false);
        }



        bool disjoint;
        public void DisplayDisjoint(bool isDisjoint)
        {
			disjoint = isDisjoint;
        }
        public void AddType(int idx, bool isProducer)
        {
            if (isProducer)
            {
                // this will have to be changed if we want to let species switch type
                producers.Add(idx);
                if (producers.Count >= leaf.ConstraintLimit && leaf.ConstraintLimit > 0) // TODO: better constraint class instead
                    OnProducersAvailable.Invoke(false);

                leaf.Display(producers.Count);
                // producerCount.text = (maxProducers-producers.Count).ToString();
            }
            else
            {
                consumers.Add(idx);
                if (consumers.Count >= paw.ConstraintLimit && paw.ConstraintLimit > 0)
                    OnConsumersAvailable.Invoke(false);

                paw.Display(consumers.Count);
                // consumerCount.text = (maxConsumers-producers.Count).ToString();
            }
        }
        public void RemoveIdx(int idx)
        {
            if (producers.Contains(idx))
            {
                if (producers.Count == leaf.ConstraintLimit)
                    OnProducersAvailable.Invoke(true);

                producers.Remove(idx);
                leaf.Display(producers.Count);
                // producerCount.text = (maxProducers-producers.Count).ToString();
            }
            else
            {
                if (consumers.Count == paw.ConstraintLimit)
                    OnConsumersAvailable.Invoke(true);

                consumers.Remove(idx);
                paw.Display(consumers.Count);
                // consumerCount.text = (maxConsumers-consumers.Count).ToString();
            }
        }
        public void DisplayNumEdges(int numEdges)
        {
            edge.Display(numEdges);
        }
        public void DisplayMaxChain(int lenChain)
        {
            chain.Display(lenChain);
        }
        public void DisplayMaxLoop(int lenLoop)
        {
            loop.Display(lenLoop);
        }



		bool feasible, stable;
        float targetAbundance, abundance;
        float targetScore, currentScore;

        public void DisplayFeastability(bool isFeasible, bool isStable)
        {
            feasible = isFeasible;
			stable = isStable;
            if (stable)
                heart.DisplayDirect("O");
            else
                heart.DisplayDirect("X");
        }
        public void DisplayScore(float score)
        {
            currentScore = score;
            scoreText.text = GameManager.Instance.NormaliseScore(score).ToString();//"000");
            // print(score);
        }


		//////////////////////
		// score calculation

        [SerializeField] Image scoreTargetImage;
        [SerializeField] Sprite targetSprite1, targetSprite2;
        [SerializeField] Text scoreText, scoreTargetText; //, abundanceText;

        public int NumStars { get; private set; }
		void Update()
		{
            if (!CanUpdate())
            {
                constrainedFrom.FinishButton.interactable = false;
            }
            else
            {
                constrainedFrom.FinishButton.interactable = true;
                int newNumStars = 0;
                star1.SetBool("Filled", false);
                star2.SetBool("Filled", false);
                star3.SetBool("Filled", false);

                int target1 = GameManager.Instance.NormaliseScore(constrainedFrom.Details.targetScore1);
                int target2 = GameManager.Instance.NormaliseScore(constrainedFrom.Details.targetScore2);

                if (!disjoint && feasible && //stable &&
                    leaf.IsSatisfied && paw.IsSatisfied &&
                    edge.IsSatisfied && chain.IsSatisfied && loop.IsSatisfied)
                {
                    newNumStars += 1;
                    star1.SetBool("Filled", true);

                    int score = GameManager.Instance.NormaliseScore(currentScore);

                    if (score >= target1 && stable)
                    {
                        newNumStars += 1;
                        star2.SetBool("Filled", true);

                        if (score >= target2)
                        {
                            newNumStars += 1;
                            star3.SetBool("Filled", true);
                        }
                    }
                }
                if (newNumStars < 2)
                {
                    scoreTargetText.text = target1.ToString();//"000");
                    scoreTargetImage.sprite = targetSprite1;
                }
                else
                {
                    scoreTargetText.text = target2.ToString();//"000");
                    scoreTargetImage.sprite = targetSprite2;
                }
                if (NumStars == 0 && newNumStars > 0)
                {
                    constrainedFrom.ShowFinishFlag();
                }
                else if (NumStars > 0 && newNumStars == 0)
                {
                    constrainedFrom.ShowThumbnail();
                }
                NumStars = newNumStars;
            }
		}
        public void Confetti()
        {
            help.SetText(constrainedFrom.Details.congratulation);
            help.DelayThenShow();

            GetComponent<Animator>().SetTrigger("Confetti");
            star1.SetTrigger("Confetti");
            star2.SetTrigger("Confetti");
            star3.SetTrigger("Confetti");
        }
    }
}