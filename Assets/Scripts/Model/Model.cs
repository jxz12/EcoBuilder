using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EcoBuilder.Model
{
    class Species
    {
        public int Idx { get; private set; }
        public bool IsProducer { get; set; }
        public double BodySize { get; set; }
        public double SelfReg { get; set; }

        public double Growth { get; set; }
        public double Search { get; set; }

        public double Abundance { get; set; } = 1; // initialise as non-extinct

        public Species(int idx)
        {
            Idx = idx;
        }
    }
    public class Model : MonoBehaviour
    {   
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
                                kg_min = 1e-3,
                                kg_max = 1e3,
                                a_ii_min = 1e-4,
                                a_ii_max = 1e2;
        
        [SerializeField] GameObject busyIcon;
                                
        LotkaVolterra<Species> simulation;
        void Awake()
        {
            simulation = new LotkaVolterra<Species>(
                s=> s.Growth,
                s=> s.SelfReg,
                (r,c)=> c.Search,
                (r,c)=> r.IsProducer? e_p : e_c
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
            if (idxToSpecies.Count > 0)
            {
                equilibriumSolved = false;
            }
            else
            {
                Feasible = Stable = Nonreactive = false;
                OnCalculated.Invoke();
            }
        }
        static double GetOnLogScale(float normalised, double minVal, double maxVal)
        {
            // float min = Mathf.Log10(minVal);
            // float max = Mathf.Log10(maxVal);
            // float mid = min + normalised*(max-min);
            // return Mathf.Pow(10, mid);

            // same as above commented
            return Math.Pow(minVal, 1-normalised) * Math.Pow(maxVal, normalised);
        }

        public void SetSpeciesIsProducer(int idx, bool isProducer)
        {
            Species s = idxToSpecies[idx];
            s.IsProducer = isProducer;

            double metabolism = Math.Pow(s.BodySize, beta-1);
            s.Growth = s.IsProducer? metabolism * b0 : -metabolism * d0;

            equilibriumSolved = false;
        }
        public void SetSpeciesBodySize(int idx, float sizeNormalised)
        {
            Species s = idxToSpecies[idx];
            s.BodySize = GetOnLogScale(sizeNormalised, kg_min, kg_max);

            double metabolism = Math.Pow(s.BodySize, beta-1);
            s.Growth = s.IsProducer? metabolism * b0 : -metabolism * d0;

                                                // velocity    mass-specific
            double velocity = Math.Pow(s.BodySize, (p_v+2*p_r) - 1);
            s.Search = velocity * a0;

            equilibriumSolved = false;
        }
        public void SetSpeciesGreediness(int idx, float greedNormalised)
        {
            idxToSpecies[idx].SelfReg = -GetOnLogScale(greedNormalised, a_ii_max, a_ii_min);
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
            busyIcon.SetActive(calculating);
        }

        public event Action OnCalculated;
        public event Action<int> OnEndangered;
        public event Action<int> OnRescued;

        public bool Feasible { get; private set; } = false;
        public bool Stable { get; private set; } = false;
        public bool Nonreactive { get; private set; } = false;

        // TODO: both of these
        public float Complexity { get; private set; } = 0;
        public float Flux { get; private set; } = 0;

        async void Equilibrium()
        {
            calculating = true;
            Feasible = await Task.Run(() => simulation.SolveFeasibility());

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