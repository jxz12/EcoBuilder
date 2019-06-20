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
        public event Action<bool> OnConstraintsMet;

        public event Action OnLevelReplay;
        public event Action OnLevelNext;
        public event Action OnBackToMenu;

        public void SlotInLevel(Level level)
        {
            level.transform.SetParent(levelParent, false);
            level.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }




		/////////////////
		// first star

        bool disjoint;

        public void DisplayDisjoint(bool isDisjoint)
        {
			disjoint = isDisjoint;
        }

        int maxProducers=int.MaxValue, maxConsumers=int.MaxValue;
        HashSet<int> producers = new HashSet<int>();
        HashSet<int> consumers = new HashSet<int>();
        public void ConstrainTypes(int numProducers, int numConsumers)
        {
            if (numProducers < 0 || numConsumers < 0)
                throw new Exception("Cannot have negative numbers of species");
            
            maxProducers = numProducers;
            maxConsumers = numConsumers;
            producerCount.text = (maxProducers-producers.Count).ToString();
            consumerCount.text = (maxConsumers-consumers.Count).ToString();
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

		// structural constraints
        public void ConstrainNumEdges(int numEdges)
        {
            edge.Constrain(numEdges);
        }
        public void ConstrainMaxChain(int lenChain)
        {
            chain.Constrain(lenChain);
        }
        public void ConstrainMaxLoop(int lenLoop)
        {
            loop.Constrain(lenLoop);
        }
        public void DisplayNumEdges(int numEdges)
        {
            edge.Display(numEdges);
            // CheckIfSatisfied();
        }
        public void DisplayMaxChain(int lenChain)
        {
            chain.Display(lenChain);
            // CheckIfSatisfied();
        }
        public void DisplayMaxLoop(int lenLoop)
        {
            loop.Display(lenLoop);
            // CheckIfSatisfied();
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
				OnConstraintsMet.Invoke(true);
			}
			else if (NumStars > 0 && newNumStars == 0)
			{
				OnConstraintsMet.Invoke(false);
			}
			NumStars = newNumStars;
		}
        public void Confetti()
        {
            print("TODO: confetti");
            // GetComponent<Animator>().SetTrigger("Finish");
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