using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace EcoBuilder.UI
{
    public class StatusBar : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler
    {
        public event Action<bool> OnProducersAvailable;
        public event Action<bool> OnConsumersAvailable;
        public event Action OnErrorShown;
        public event Action OnLevelCompleted;
        public event Action OnLevelCompletabled, OnLevelIncompletabled;

        [SerializeField] Animator star1, star2, star3;
        [SerializeField] Animator score, constraints;
        [SerializeField] Constraint leaf, paw, edge, chain, loop;
        [SerializeField] Help help;

        void Awake()
        {
            var level = GameManager.Instance.PlayedLevel;
            if (level == null || level.Details.title == "Sandbox") // FIXME:
            {
                leaf.Unconstrain();
                paw.Unconstrain();
                edge.Unconstrain();
                chain.Unconstrain();
                loop.Unconstrain();
            }
            else
            {
                leaf.Constrain(level.Details.numProducers);
                paw.Constrain(level.Details.numConsumers);

                edge.Constrain(level.Details.minEdges);
                chain.Constrain(level.Details.minChain);
                loop.Constrain(level.Details.minLoop);
            }
            // shield.Constrain(-1);
            feasibleScoreCol = scoreText.color;
            infeasibleScoreCol = new Color(.7f,.7f,.7f,.8f); // TODO: magic number
            scoreText.color = infeasibleScoreCol;

            if (level == null) // only for test
            {
                DisableFinish();
                return;
            }
            help.SetText(level.Details.introduction);
            target1 = level.Details.targetScore1;
            target2 = level.Details.targetScore2;

            GameManager.Instance.PlayedLevel.OnFinished += CompleteLevel;
        }
        void OnDestroy()
        {
            if (GameManager.Instance != null && GameManager.Instance.PlayedLevel != null)
                GameManager.Instance.PlayedLevel.OnFinished -= CompleteLevel; // not sure if necessary?
        }
        int target1, target2;
        Color feasibleScoreCol, infeasibleScoreCol;

        HashSet<int> producers = new HashSet<int>();
        HashSet<int> consumers = new HashSet<int>();

        Func<bool> CanUpdate;
        public void AllowUpdateWhen(Func<bool> Allowed)
        {
            CanUpdate = Allowed;
        }
        public void ShowHelp(bool showing)
        {
            help.Show(showing);
        }
        public void HideScore(bool hidden=true)
        {
            // score.SetBool("Visible", !hidden);
            score.gameObject.SetActive(!hidden);
        }
        public void HideConstraints(bool hidden=true)
        {
            // constraints.SetBool("Visible", !hidden);
            constraints.gameObject.SetActive(!hidden);
        }
        bool finishDisabled = false;
        public void DisableFinish(bool disabled=true)
        {
            finishDisabled = disabled;
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
        // float targetAbundance, abundance;
        int modelScore, realisedScore;

        public void DisplayFeastability(bool isFeasible, bool isStable)
        {
            feasible = isFeasible;
			stable = isStable;
            // shield.Display(stable);
        }
        public void DisplayScore(float score)
        {
            modelScore = ((int)Math.Truncate(score * 1000));
        }

		//////////////////////
		// score calculation

        [SerializeField] Image scoreTargetImage;
        [SerializeField] Sprite targetSprite1, targetSprite2;
        [SerializeField] Text scoreText, scoreTargetText; //, abundanceText;

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
            int newNumStars = 0;

            star1.SetBool("Filled", false);
            star2.SetBool("Filled", false);
            star3.SetBool("Filled", false);

            if (!disjoint && feasible &&
                // stable &&
                leaf.IsSatisfied && paw.IsSatisfied &&
                edge.IsSatisfied && chain.IsSatisfied && loop.IsSatisfied)
            {
                newNumStars += 1;
                star1.SetBool("Filled", true);
                scoreText.color = Color.Lerp(scoreText.color, feasibleScoreCol, .01f);

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
            }
            else
            {
                // scoreText.color = infeasibleScoreCol;
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
        void CompleteLevel()
        {
            if (NumStars < 1 || NumStars > 3)
                throw new Exception("cannot pass with less than 0 or more than 3 stars");

            GameManager.Instance.SavePlayedLevel(NumStars, realisedScore);

            help.Show(false);
            help.SetText(GameManager.Instance.PlayedLevel.Details.congratulation);
            help.DelayThenShow(2);

            score.SetBool("Visible", false);
            constraints.SetBool("Visible", false);
            star1.SetTrigger("Confetti");
            star2.SetTrigger("Confetti");
            star3.SetTrigger("Confetti");

            OnLevelCompleted.Invoke();
        }

        [SerializeField] Tooltip tooltip;
        public void OnPointerEnter(PointerEventData ped)
        {
            tooltip.Enable();
            tooltip.ShowText(Error());
            StartCoroutine(FollowCursor());
            OnErrorShown?.Invoke();
        }
        IEnumerator FollowCursor()
        {
            while (true)
            {
                tooltip.SetPos(Input.mousePosition);
                yield return null;
            }
        }
        public void OnPointerExit(PointerEventData ped)
        {
            tooltip.Disable();
            StopCoroutine(FollowCursor());
        }
        string Error()
        {
            if (consumers.Count==0 && producers.Count==0)
                return "Your ecosystem is empty.";
            if (!leaf.IsSatisfied)
                return "You have not added enough plants.";
            if (!paw.IsSatisfied)
                return "You have not added enough animals.";
            if (!feasible)
                return "At least one species is going extinct.";
            if (disjoint)
                return "Your network is not connected.";
            // if (!stable)
            //     return "Your ecosystem is not stable.";
            if (!edge.IsSatisfied)
                return "You have not added enough links.";
            if (!chain.IsSatisfied)
                return "Your web is not tall enough.";
            if (!loop.IsSatisfied)
                return "You do not have a long enough loop.";
            else
                return "Your ecosystem has no errors!";
        }
    }
}