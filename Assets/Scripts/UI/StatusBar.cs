using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.UI
{
    public class StatusBar : MonoBehaviour
    {
        [SerializeField] Animator star1, star2, star3;
        [SerializeField] Constraint edge, chain, loop;
        [SerializeField] Transform levelParent;
        [SerializeField] Text producerCount;
        [SerializeField] Text consumerCount;

        public event Action<bool> OnProducersAvailable;
        public event Action<bool> OnConsumersAvailable;

        public event Action OnLevelCompleted;

        int maxProducers=int.MaxValue, maxConsumers=int.MaxValue;
        HashSet<int> producers = new HashSet<int>();
        HashSet<int> consumers = new HashSet<int>();

        Level constrainedFrom;
        public void ConstrainFromLevel(Level level)
        {
            // take the played level and place it in the scene
            if (level.Details.numProducers < 0 || level.Details.numConsumers < 0)
                throw new Exception("Cannot have negative numbers of species");
            
            maxProducers = level.Details.numProducers;
            maxConsumers = level.Details.numConsumers;
            producerCount.text = (maxProducers-producers.Count).ToString();
            consumerCount.text = (maxConsumers-consumers.Count).ToString();

            edge.Constrain(level.Details.minEdges);
            chain.Constrain(level.Details.minChain);
            loop.Constrain(level.Details.minChain);

            level.transform.SetParent(levelParent, false);
            level.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            level.OnFinishFlagPressed += ()=> CompleteLevel();
            constrainedFrom = level;
        }

        public void CompleteLevel()
        {
            GameManager.Instance.SavePlayedLevel(NumStars);
            OnLevelCompleted.Invoke();
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
                // TODO: this will have to be changed if we want to let species switch type
                // consumers.Remove(idx);
                producers.Add(idx);
                if (producers.Count >= maxProducers)
                    OnProducersAvailable.Invoke(false);

                producerCount.text = (maxProducers-producers.Count).ToString();
            }
            else
            {
                // producers.Remove(idx);
                consumers.Add(idx);
                consumerCount.text = (maxConsumers-consumers.Count).ToString();
                if (consumers.Count >= maxConsumers)
                    OnConsumersAvailable.Invoke(false);

                consumerCount.text = (maxConsumers-producers.Count).ToString();
            }
        }
        public void RemoveIdx(int idx)
        {
            if (producers.Contains(idx))
            {
                if (producers.Count == maxProducers)
                    OnProducersAvailable.Invoke(true);

                producers.Remove(idx);
                producerCount.text = (maxProducers-producers.Count).ToString();
            }
            else
            {
                if (consumers.Count == maxConsumers)
                    OnConsumersAvailable.Invoke(true);

                consumers.Remove(idx);
                consumerCount.text = (maxConsumers-consumers.Count).ToString();
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



        /////////////////////////
        // additional stars

		bool feasible, stable;
        float targetFlux, flux;

        public void DisplayFeastability(bool isFeasible, bool isStable)
        {
            feasible = isFeasible;
			stable = isStable;
        }
		public void SetTargetFlux(float target)
		{
			targetFlux = target;
		}
        public void DisplayTotalFlux(float totalFlux)
        {
            flux = totalFlux;
        }


		//////////////////////
		// score calculation

        public int NumStars { get; private set; }
		void Update()
		{
            int newNumStars = 0;
			star1.SetBool("Filled", false);
			star2.SetBool("Filled", false);
			star3.SetBool("Filled", false);
            if (!disjoint &&
                producers.Count == maxProducers && consumers.Count == maxConsumers &&
                edge.IsSatisfied && chain.IsSatisfied && loop.IsSatisfied)
            {
                newNumStars += 1;
				star1.SetBool("Filled", true);

                if (feasible && stable)
                {
                    newNumStars += 1;
					star2.SetBool("Filled", true);

                    if (flux > targetFlux)
					{
						newNumStars += 1;
						star3.SetBool("Filled", true);
					}
                }
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
        public void Confetti()
        {
            GetComponent<Animator>().SetTrigger("Confetti");
            star1.SetTrigger("Confetti");
            star2.SetTrigger("Confetti");
            star3.SetTrigger("Confetti");
        }

        // // TODO: make this into prefab instead?
        // [SerializeField] Text errorText;
        // public void ShowErrorMessage(string message)
        // {
        //     errorText.text = message;
        //     GetComponent<Animator>().SetTrigger("Show Error");
        // }
    }
}