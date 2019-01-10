using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EcoBuilder.Model
{
    public class Model : MonoBehaviour
    {   
        [Serializable] class IntEvent : UnityEvent<int> { }
        [Serializable] class IntFloatEvent : UnityEvent<int, float> { }
        [Serializable] class IntIntFloatEvent : UnityEvent<int, int, float> { }

        [SerializeField] IntEvent WarningEvent;
        [SerializeField] IntEvent RescuedEvent;
        [SerializeField] IntEvent EndangeredEvent;
        [SerializeField] IntEvent ExtinctionEvent;
        [SerializeField] IntFloatEvent AbundanceSetEvent;
        [SerializeField] IntIntFloatEvent FluxSetEvent;

        [SerializeField] float heartRate=60, abundanceTweenSpeed=.1f, lagAbundanceTweenSpeed=.01f;
        [SerializeField] Monitor monitor;

        class Species
        {
            public int Idx { get; private set; }
            bool isProducer;
            public bool IsProducer {
                set { isProducer = value; }
            }
            double bodyMass;
            public double BodyMass {
                set { bodyMass = Math.Pow(10, value*9); }
            }
            double intraspecific;
            public double Intraspecific {
                set { intraspecific = value * .1; }
            }
            public Species(int idx)
            {
                Idx = idx;
            }

            public static double Birth(Species s)
            {
                return s.isProducer? 1:-1;
            }
            public static double Intra(Species s)
            {
                return s.intraspecific;
            }
            public static double Attack(Species res, Species con)
            {
                return 1;
            }
            public static double Efficiency(Species res, Species con)
            {
                return res.isProducer? 0.2:0.5;
            }

            static readonly double b0 = 1e0,
                                   d0 = 1e-2,
                                   a0 = 1e0,
                                   a_ii0 = 1e-1,
                                   beta = 0.75;
        }

        LotkaVolterraLAS<Species> simulation = new LotkaVolterraLAS<Species>(
            s => Species.Birth(s),
            s => Species.Intra(s),
            (r,c) => Species.Attack(r,c),
            (r,c) => Species.Efficiency(r,c)
        );

        Dictionary<int, Species> idx2Species = new Dictionary<int, Species>();
        Dictionary<Species, int> species2Idx = new Dictionary<Species, int>();
        Dictionary<int, float> gameAbundances = new Dictionary<int, float>();

        public void AddSpecies(int idx)
        {
            var newSpecies = new Species(idx);
            simulation.AddSpecies(newSpecies);
            idx2Species.Add(idx, newSpecies);
            species2Idx.Add(newSpecies, idx);
            gameAbundances.Add(idx, 0);
        }
        public void RemoveSpecies(int idx)
        {
            Species toRemove = idx2Species[idx];
            simulation.RemoveSpecies(toRemove);
            idx2Species.Remove(idx);
            species2Idx.Remove(toRemove);
            gameAbundances.Remove(idx);
        }
        public void SetSpeciesAsProducer(int idx)
        {
            idx2Species[idx].IsProducer = true;
        }
        public void SetSpeciesAsConsumer(int idx)
        {
            idx2Species[idx].IsProducer = false;
        }
        public void SetSpeciesBodyMass(int idx, float bodyMass)
        {
            idx2Species[idx].BodyMass = bodyMass;
        }
        public void SetSpeciesGreediness(int idx, float greediness)
        {
            idx2Species[idx].Intraspecific = greediness-1;
        }

        public void AddInteraction(int resource, int consumer)
        {
            if (resource == consumer)
                throw new Exception("can't eat itself");

            Species res = idx2Species[resource], con = idx2Species[consumer];
            simulation.AddInteraction(res, con);
        }
        public void RemoveInteraction(int resource, int consumer)
        {
            if (resource == consumer)
                throw new Exception("can't eat itself");

            Species res = idx2Species[resource], con = idx2Species[consumer];
            simulation.RemoveInteraction(res, con);
        }

        async void Equilibrium()
        {
            await Task.Run(() => simulation.SolveEquilibrium());
            bool feasible = ScaleAndSetAbundances();

            if (feasible)
                CalculateAndSetStability();
            else
                monitor.SetLAS("infeasible");
        }

        HashSet<int> warningSpecies = new HashSet<int>();
        private float lagMaxAbundance = 0, lagMinAbundance = float.MaxValue;
        static readonly float myEpsilon = 1e-10f;

        private bool ScaleAndSetAbundances() // returns if feasible
        {
            float maxModelAbundance=0, minGameAbundance=float.MaxValue;
            bool feasible = true;
            foreach (int idx in idx2Species.Keys)
            {
                Species s = idx2Species[idx];
                float modelAbundance = (float)simulation.GetAbundance(s);
                if (modelAbundance <= 0 && !warningSpecies.Contains(idx))
                {
                    warningSpecies.Add(idx);
                    WarningEvent.Invoke(idx);
                }
                else if (modelAbundance > 0 && warningSpecies.Contains(idx))
                {
                    warningSpecies.Remove(idx);
                    RescuedEvent.Invoke(idx);
                }
                maxModelAbundance = Mathf.Max(maxModelAbundance, modelAbundance);

                float oldAbundance = gameAbundances[idx];
                float newAbundance = oldAbundance + abundanceTweenSpeed*(modelAbundance-oldAbundance);
                if (newAbundance > 0)
                {
                    gameAbundances[idx] = newAbundance;
                    minGameAbundance = Mathf.Min(minGameAbundance, newAbundance);
                }
                else
                {
                    feasible = false;
                    if (oldAbundance > 0)
                    {
                        gameAbundances[idx] = 0;
                        EndangeredEvent.Invoke(idx);
                    }
                }
            }

            // guarantees a scale of 0 to 1, but with a time lag to guarantee that falls in abundance look like falls
            if (maxModelAbundance > lagMaxAbundance)
                lagMaxAbundance = maxModelAbundance;
            else
                lagMaxAbundance += lagAbundanceTweenSpeed * (maxModelAbundance - lagMaxAbundance);

            // if (minGameAbundance < lagMinAbundance)
            //     lagMinAbundance = minGameAbundance;
            // else
            //     lagMinAbundance += lagAbundanceTweenSpeed * (minGameAbundance - lagMinAbundance);

            // // monitor.SetHeight(lagMaxAbundance.ToString("0.000") + " " + minGameAbundance.ToString("0.000"));
            // monitor.SetHeight(lagMinAbundance.ToString("0.00") + " " + lagMaxAbundance.ToString("0.00"));

            // float maxLogAbundance = Mathf.Log(lagMaxAbundance);
            // float minLogAbundance = Mathf.Log(lagMinAbundance) - myEpsilon; //prevent divide by zero
            // float abundanceRange = maxLogAbundance - minLogAbundance;

            // foreach (int idx in gameAbundances.Keys)
            // {
            //     float abundance = gameAbundances[idx];
            //     if (abundance == 0)
            //     {
            //         AbundanceSetEvent.Invoke(idx, 0);
            //     }
            //     else
            //     {
            //         float scaledAbundance = (Mathf.Log(abundance) - minLogAbundance) / abundanceRange;
            //         AbundanceSetEvent.Invoke(idx, scaledAbundance);
            //     }
            // }

            foreach (int idx in gameAbundances.Keys)
            {
                float abundance = gameAbundances[idx];
                float scaledAbundance = abundance / lagMaxAbundance;
                AbundanceSetEvent.Invoke(idx, scaledAbundance);
            }

            return feasible;
        }
        private async void CalculateAndSetStability()
        {
            double stability = await Task.Run(() => simulation.LocalAsymptoticStability());
            monitor.SetLAS(stability.ToString("0.00"));
        }

        private void Start()
        {
            // StartCoroutine(Pulse(60f / heartRate));
        }

        IEnumerator Pulse(float delay)
        {
            while (true)
            {
                if (idx2Species.Count > 0)
                    Equilibrium();
                yield return new WaitForSeconds(delay);
            }
        }

    }
}