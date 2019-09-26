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
        [SerializeField] Constraint shield, leaf, paw, edge, chain, loop;
        [SerializeField] Help help;
        [SerializeField] RectTransform levelParent;

        public Level Playing { get; private set; }
        void Awake()
        {
            var level = GameManager.Instance.PlayedLevel;
            if (level == null)
            {
                Playing = GameManager.Instance.GetDefaultLevel();
                leaf.Unconstrain();
                paw.Unconstrain();
                edge.Unconstrain();
                chain.Unconstrain();
                loop.Unconstrain();
                shield.Display(false);
            }
            else
            {
                Playing = level;
                leaf.Constrain(Playing.Details.numProducers);
                paw.Constrain(Playing.Details.numConsumers);

                edge.Constrain(Playing.Details.minEdges);
                chain.Constrain(Playing.Details.minChain);
                loop.Constrain(Playing.Details.minLoop);

            }
            help.SetText(Playing.Details.introduction);
            Playing.ShowThumbnailNewParent(levelParent, Vector2.zero);
            Playing.OnFinishClicked += CompleteLevel;

            target1 = Playing.Details.targetScore1;
            target2 = Playing.Details.targetScore2;
            defaultScoreCol = scoreText.color;
        }
        float target1, target2;
        Color defaultScoreCol;

        HashSet<int> producers = new HashSet<int>();
        HashSet<int> consumers = new HashSet<int>();

        void CompleteLevel()
        {
            help.SetDistFromTop(.15f);
            help.SetSide(false, false);

            if (NumStars < 1 || NumStars > 3)
                throw new Exception("cannot pass with less than 0 or more than 3 stars");

            if (NumStars > Playing.Details.numStars)
                Playing.Details.numStars = NumStars;

            if (currentScore > Playing.Details.highScore)
                Playing.Details.highScore = currentScore;

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
        [SerializeField] GameObject scoreParent, constraintsParent;
        public void HideScore(bool hidden=true)
        {
            scoreParent.gameObject.SetActive(!hidden);
        }
        public void HideConstraints(bool hidden=true)
        {
            constraintsParent.gameObject.SetActive(!hidden);
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
        bool starsNeedUpdate = false;
        // float targetAbundance, abundance;
        float currentScore, showingScore;

        public void DisplayFeastability(bool isFeasible, bool isStable)
        {
            feasible = isFeasible;
			stable = isStable;
            shield.Display(stable);
        }
        public void DisplayScore(float score)
        {
            scoreText.color = score >= currentScore? Color.green : Color.red;
            currentScore = score;
            starsNeedUpdate = true;
        }
        void FixedUpdate()
        {
            // showingScore = Mathf.Lerp(showingScore, currentScore, .2f);
            showingScore = Mathf.Lerp(showingScore, currentScore, 1);
            scoreText.text = ((int)Math.Truncate(showingScore * 100)).ToString();
            if (CanUpdate())
                scoreText.color = Color.Lerp(scoreText.color, defaultScoreCol, .05f);
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
                Playing.SetFinishable(false);
            }
            else if (starsNeedUpdate)
            {
                starsNeedUpdate = false;

                Playing.SetFinishable(true);
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

                    if (currentScore >= target1)
                    {
                        newNumStars += 1;
                        star2.SetBool("Filled", true);

                        if (currentScore >= target2)
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
                    Playing.ShowFinishFlag();
                }
                else if (NumStars > 0 && newNumStars == 0)
                {
                    Playing.ShowThumbnail();
                }
                NumStars = newNumStars;
            }
		}
        public void Confetti()
        {
            help.Show(false);
            help.SetText(Playing.Details.congratulation);
            help.DelayThenShow(2);

            GetComponent<Animator>().SetTrigger("Confetti");
            star1.SetTrigger("Confetti");
            star2.SetTrigger("Confetti");
            star3.SetTrigger("Confetti");
        }
    }
}