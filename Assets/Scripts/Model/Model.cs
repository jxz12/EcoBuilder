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

        [SerializeField] IntEvent OnEndangered;
        [SerializeField] IntEvent OnExtinction;
        [SerializeField] IntEvent OnRescued;
        [SerializeField] IntFloatEvent OnAbundanceSet;
        [SerializeField] IntIntFloatEvent OnFluxSet;

        [SerializeField] float heartRate=60; // frequency of LAS calculations
        [SerializeField] int countdown=5; // how many heartbeats until death
        [SerializeField] Monitor monitor;

        class Species
        {
            public int Idx { get; private set; }
            bool isProducer;
            public bool IsProducer {
                set { isProducer = value; }
            }
            // double bodyMass;
            // public double BodyMass {
            //     set { bodyMass = Math.Pow(10, value*9); }
            // }
            double metabolism;
            public double Metabolism {
                set { metabolism = value; }
            }
            double greediness;
            public double Greediness {
                set { Greediness = value; }
            }
            public Species(int idx)
            {
                Idx = idx;
            }

            public static double Growth(Species s)
            {
                if (s.isProducer)
                    return -d0 * s.metabolism;
                else
                    return b0 * s.metabolism;
            }
            public static double Intra(Species s)
            {
                return a_ii0 * (-1 + s.greediness);
            }
            public static double Attack(Species res, Species con)
            {
                return a0 * con.metabolism;
            }
            public static double Efficiency(Species res, Species con)
            {
                return res.isProducer? 0.2:0.5;
            }
            static readonly double b0 = 1,
                                   d0 = 1,
                                   a_ii0 = 1,
                                   a0 = 1;

            // static readonly double b0 = 1e0,
            //                        d0 = 1e-2,
            //                        a0 = 1e0,
            //                        a_ii0 = 1e-1,
            //                        beta = 0.75;
        }

        LotkaVolterraLAS<Species> simulation = new LotkaVolterraLAS<Species>
        (
            s => Species.Growth(s),
            s => Species.Intra(s),
            (r,c) => Species.Attack(r,c),
            (r,c) => Species.Efficiency(r,c)
        );

        Dictionary<int, Species> idx2Species = new Dictionary<int, Species>();
        // Dictionary<Species, int> species2Idx = new Dictionary<Species, int>();
        Dictionary<int, float> gameAbundances = new Dictionary<int, float>();

        public void AddSpecies(int idx)
        {
            var newSpecies = new Species(idx);
            simulation.AddSpecies(newSpecies);
            idx2Species.Add(idx, newSpecies);
            // species2Idx.Add(newSpecies, idx);
            gameAbundances.Add(idx, 0);
        }
        public void RemoveSpecies(int idx)
        {
            Species toRemove = idx2Species[idx];
            simulation.RemoveSpecies(toRemove);
            idx2Species.Remove(idx);
            // species2Idx.Remove(toRemove);
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
        public void SetSpeciesMetabolism(int idx, float metabolism)
        {
            idx2Species[idx].Metabolism = metabolism;
        }
        public void SetSpeciesGreediness(int idx, float greediness)
        {
            idx2Species[idx].Greediness = greediness;
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

        HashSet<Species> endangeredSpecies = new HashSet<Species>();
        private bool ScaleAndSetAbundances()
        {
            bool feasible = true;
            foreach (Species s in idx2Species.Values)
            {
                // Species s = idx2Species[idx];
                float modelAbundance = (float)simulation.GetAbundance(s);
                if (modelAbundance <= 0)
                {
                    feasible = false;
                    if (!endangeredSpecies.Contains(s))
                    {
                        endangeredSpecies.Add(s);
                        OnEndangered.Invoke(s.Idx);
                    }
                }
                else
                {
                    if (endangeredSpecies.Contains(s))
                    {
                        endangeredSpecies.Remove(s);
                        OnRescued.Invoke(s.Idx);
                    }
                }
            }
            return feasible;
        }
        private async void CalculateAndSetStability()
        {
            double stability = await Task.Run(() => simulation.LocalAsymptoticStability());
            monitor.SetLAS(stability.ToString("E"));
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