using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EcoBuilder.Model
{
    class Species
    {
        public int Idx { get; private set; }
        bool isProducer = true;
        public bool IsProducer {
            set {
                isProducer = value;
                // Growth = isProducer? growth_exp*b0 : -growth_exp*d0;
                // Efficiency = isProducer? e_p : e_c;
                Growth = isProducer? 1:-.1;
            }
        }
        // double growth_exp = 1/b0;
        // double search_exp = 1;
        public double BodySize {
            set {
                // growth_exp = Math.Pow(value, beta-1);
                // Growth = isProducer? growth_exp*b0 : -growth_exp*d0;

                // search_exp = Math.Pow(value, (p_v+2*p_r) - 1);
                // Search = search_exp * a0;
                Search = 1;
            }
        }
        public double Greediness {
            set {
                // this is the same as scaling on a log scale
                // SelfReg = -Math.Pow(a_ii_min, 1-value) * Math.Pow(a_ii_max, value);
                SelfReg = -10;
            }
        }
        public double Growth { get; private set; } = 1;
        public double Search { get; private set; } = 1;
        public double Find { get; private set; } = 1;
        public double SelfReg { get; private set; } = -1;
        public double Efficiency { get; private set; } = 1;

        public double Abundance { get; set; } = 1; // initialise as non-extinct

        public Species(int idx)
        {
            Idx = idx;
        }
    }
    public class Model : MonoBehaviour
    {   
        [Serializable] class IntEvent : UnityEvent<int> { }
        [Serializable] class IntFloatEvent : UnityEvent<int, float> { }
        [Serializable] class IntIntFloatEvent : UnityEvent<int, int, float> { }

        [SerializeField] double beta = .75,   // metabolic scaling
                                b0 = 1.71e-6, // birth
                                d0 = 4.15e-8, // death
                                p_v = .26,    // velocity exponent
                            //    v0 = .33,     // velocity constant
                                p_r = .21,    // reaction exponent
                            //    r0 = 1.62,    // reaction constant
                                // p_e = .85, // empirical exponent
                                a0 = 8.32e-4,
                                e_p = .2, // plant efficiency
                                e_c = .5, // animal efficiency
                                a_ii_min = 1e-3,
                                a_ii_max = 1e2;

        // public float GetKg(float normalizedBodySize)
        // {
        //     // float min = Mathf.Log10(minKg);
        //     // float max = Mathf.Log10(maxKg);
        //     // float mid = min + input*(max-min);
        //     // return Mathf.Pow(10, mid);

        //     // same as above commented
        //     return Mathf.Pow(minKg, 1-normalizedBodySize) * Mathf.Pow(maxKg, normalizedBodySize);
        // }


        LotkaVolterra<Species> simulation;
        void Awake()
        {
            simulation = new LotkaVolterra<Species>(
                s=> s.Growth,
                s=> s.SelfReg,
                (r,c)=> r.Search,
                (r,c)=> r.Efficiency
            );
        }
        Dictionary<int, Species> idxToSpecies = new Dictionary<int, Species>();

        public void AddSpecies(int idx)
        {
            var newSpecies = new Species(idx);
            simulation.AddSpecies(newSpecies);
            idxToSpecies.Add(idx, newSpecies);
            equilibriumSolved = false;
        }
        public void RemoveSpecies(int idx)
        {
            Species toRemove = idxToSpecies[idx];
            simulation.RemoveSpecies(toRemove);
            idxToSpecies.Remove(idx);
            equilibriumSolved = false;
        }
        public void SetSpeciesProduction(int idx, bool isProducer)
        {
            idxToSpecies[idx].IsProducer = isProducer;
            equilibriumSolved = false;
        }
        public void SetSpeciesBodySize(int idx, float kg)
        {
            idxToSpecies[idx].BodySize = kg;
            equilibriumSolved = false;
        }
        public void SetSpeciesGreediness(int idx, float greedNormalised)
        {
            idxToSpecies[idx].Greediness = greedNormalised;
            equilibriumSolved = false;
        }

        public void AddInteraction(int resource, int consumer)
        {
            if (resource == consumer)
                throw new Exception("can't eat itself");

            Species res = idxToSpecies[resource], con = idxToSpecies[consumer];
            simulation.AddInteraction(res, con);
            equilibriumSolved = false;
        }
        public void RemoveInteraction(int resource, int consumer)
        {
            if (resource == consumer)
                throw new Exception("can't eat itself");

            Species res = idxToSpecies[resource], con = idxToSpecies[consumer];
            simulation.RemoveInteraction(res, con);
            equilibriumSolved = false;
        }









        bool equilibriumSolved = true, calculating = false;
        void LateUpdate()
        {
            if (!equilibriumSolved && !calculating)
            {
                equilibriumSolved = true;
                Equilibrium();
            }
            else if (!equilibriumSolved && calculating)
            {
                // TODO: change this to a little icon instead of a print
                print("busy");
            }
        }

        public event Action OnCalculated;
        public event Action<int> OnEndangered;
        public event Action<int> OnRescued;

        public bool Feasible { get; private set; } = false;
        public bool Stable { get; private set; } = false;
        public bool Nonreactive { get; private set; } = false;
        public float Flux { get; private set; } = 0;
        async void Equilibrium()
        {
            calculating = true;
            Feasible = await Task.Run(() => simulation.SolveEquilibrium());

            Flux = (float)simulation.GetTotalFlux();

            if (Feasible)
            {
                Stable = await Task.Run(() => simulation.SolveStability());
                if (Stable)
                {
                    Nonreactive = await Task.Run(() => simulation.SolveReactivity());
                }
                else
                {
                    Nonreactive = false;
                }
            }
            else
            {
                Stable = Nonreactive = false;
            }

            // show abundance warnings
            foreach (int i in idxToSpecies.Keys)
            {
                Species s = idxToSpecies[i];
                double newAbundance = simulation.GetSolvedAbundance(s);
                if (s.Abundance > 0 && newAbundance <= 0)
                {
                    OnEndangered.Invoke(i);
                }
                if (s.Abundance <= 0 && newAbundance > 0)
                {
                    OnRescued.Invoke(i);
                }
                s.Abundance = newAbundance;
            }

            calculating = false;
            OnCalculated.Invoke();
        }

        public float GetAbundance(int idx)
        {
            return (float)idxToSpecies[idx].Abundance;
        }
    }
}