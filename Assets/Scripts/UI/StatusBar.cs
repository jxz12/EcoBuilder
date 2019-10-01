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
        [SerializeField] Animator score, constraints;
        [SerializeField] Constraint shield, leaf, paw, edge, chain, loop;
        [SerializeField] Help help;
        [SerializeField] RectTransform levelParent;

        [SerializeField] Tutorial[] tutorials;

        public Level Playing { get; private set; }
        void Awake()
        {
            var level = GameManager.Instance.PlayedLevel;
            if (level == null || level.Details.title == "Sandbox") // FIXME:
            {
                if (level == null)
                {
                    // note: only for testing
                    level = GameManager.Instance.GetDefaultLevel();
                }
                leaf.Unconstrain();
                paw.Unconstrain();
                edge.Unconstrain();
                chain.Unconstrain();
                loop.Unconstrain();
                shield.Display(false);
            }
            else
            {
                leaf.Constrain(level.Details.numProducers);
                paw.Constrain(level.Details.numConsumers);

                edge.Constrain(level.Details.minEdges);
                chain.Constrain(level.Details.minChain);
                loop.Constrain(level.Details.minLoop);
                if (level.Details.idx < tutorials.Length)
                {
                    Instantiate(tutorials[level.Details.idx], transform.parent, false);
                }
            }

            Playing = level;
            help.SetText(Playing.Details.introduction);
            Playing.ShowThumbnailNewParent(levelParent, Vector2.zero);
            Playing.OnFinishClicked += CompleteLevel;

            target1 = Playing.Details.targetScore1;
            target2 = Playing.Details.targetScore2;
            feasibleScoreCol = scoreText.color;
            infeasibleScoreCol = Color.grey;
        }
        int target1, target2;
        Color feasibleScoreCol, infeasibleScoreCol;

        HashSet<int> producers = new HashSet<int>();
        HashSet<int> consumers = new HashSet<int>();

        void CompleteLevel()
        {
            help.SetDistFromTop(.05f);
            help.SetSide(false, false);

            if (NumStars < 1 || NumStars > 3)
                throw new Exception("cannot pass with less than 0 or more than 3 stars");

            if (NumStars > Playing.Details.numStars)
                Playing.Details.numStars = NumStars;

            if (realisedScore > Playing.Details.highScore)
                Playing.Details.highScore = realisedScore;

            // unlock next level if not unlocked
            if (Playing.NextLevel.Details.numStars == -1)
            {
                print("TODO: animation here!");
                Playing.NextLevel.Details.numStars = 0;
                Playing.NextLevel.SaveToFile();
                Playing.NextLevel.Unlock();
            }
            Playing.SaveToFile();

            OnLevelCompleted.Invoke();
            Confetti();
        }
        void OnDestroy()
        {
            Playing.OnFinishClicked -= CompleteLevel; // not sure if necessary
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
        public void HideScore(bool hidden=true) // TODO: make these separate animators and smooth
        {
            // score.SetBool("Visible", !hidden);
            score.gameObject.SetActive(!hidden);
        }
        public void HideConstraints(bool hidden=true)
        {
            // constraints.SetBool("Visible", !hidden);
            constraints.gameObject.SetActive(!hidden);
        }
        bool scorePaused = false;
        public void PauseScoreCalculation(bool paused=true)
        {
            scorePaused = paused;
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
            shield.Display(stable);
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
            if (scorePaused)
                return;

            Playing.SetFinishable(CanUpdate());
            
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

            if (!disjoint && feasible && stable &&
                leaf.IsSatisfied && paw.IsSatisfied &&
                edge.IsSatisfied && chain.IsSatisfied && loop.IsSatisfied)
            {
                newNumStars += 1;
                star1.SetBool("Filled", true);
                scoreText.color = Color.Lerp(scoreText.color, feasibleScoreCol, .05f);

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
            if (NumStars == 0 && newNumStars > 0)
            {
                Playing.ShowFinishFlag();
            }
            else if (NumStars > 0 && newNumStars == 0)
            {
                Playing.ShowThumbnail();
            }
            NumStars = newNumStars;
		}

        public void Confetti()
        {
            help.Show(false);
            help.SetText(Playing.Details.congratulation);
            help.DelayThenShow(2);

            score.SetBool("Visible", false);
            constraints.SetBool("Visible", false);
            star1.SetTrigger("Confetti");
            star2.SetTrigger("Confetti");
            star3.SetTrigger("Confetti");
        }
    }
}