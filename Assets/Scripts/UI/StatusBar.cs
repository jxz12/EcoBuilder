using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EcoBuilder.UI
{
    public class StatusBar : MonoBehaviour
    {
        [SerializeField] Animator star1, star2, star3;
        [SerializeField] Constraint leaf, paw, edge, chain, loop;
        [SerializeField] Text abundanceText, fluxText;
        [SerializeField] Transform levelParent;

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
            
            leaf.Constrain(level.Details.numProducers);
            paw.Constrain(level.Details.numConsumers);
            edge.Constrain(level.Details.minEdges);
            chain.Constrain(level.Details.minChain);
            loop.Constrain(level.Details.minLoop);

            constrainedFrom = level;
            constrainedFrom.ShowThumbnail(levelParent, Vector2.zero);
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

                leaf.Display(producers.Count);
                // producerCount.text = (maxProducers-producers.Count).ToString();
            }
            else
            {
                // producers.Remove(idx);
                consumers.Add(idx);
                if (consumers.Count >= maxConsumers)
                    OnConsumersAvailable.Invoke(false);

                paw.Display(consumers.Count);
                // consumerCount.text = (maxConsumers-producers.Count).ToString();
            }
        }
        public void RemoveIdx(int idx)
        {
            if (producers.Contains(idx))
            {
                if (producers.Count == maxProducers)
                    OnProducersAvailable.Invoke(true);

                producers.Remove(idx);
                leaf.Display(producers.Count);
                // producerCount.text = (maxProducers-producers.Count).ToString();
            }
            else
            {
                if (consumers.Count == maxConsumers)
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
        float targetFlux, flux;

        public void DisplayFeastability(bool isFeasible, bool isStable)
        {
            feasible = isFeasible;
			stable = isStable;
        }
		public void SetTargetAbundance(float target)
		{
			targetAbundance = target;
		}
        public void DisplayTotalAbundance(float totalAbundance)
        {
            abundance = totalAbundance;
            // abundanceText.text = abundance.ToString();
            abundanceText.text = Mathf.Log10(abundance*1e10f).ToString("0.0");
            print(abundance);
        }
		public void SetTargetFlux(float target)
		{
			targetFlux = target;
		}
        public void DisplayTotalFlux(float totalFlux)
        {
            flux = totalFlux;
            // fluxText.text = flux.ToString();
            fluxText.text = Mathf.Log10(flux*1e10f).ToString("0.0");
            print(flux);
        }


		//////////////////////
		// score calculation

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
                if (!disjoint && feasible &&
                    leaf.IsSatisfied && paw.IsSatisfied &&
                    edge.IsSatisfied && chain.IsSatisfied && loop.IsSatisfied)
                {
                    newNumStars += 1;
                    star1.SetBool("Filled", true);

                    if (stable)
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