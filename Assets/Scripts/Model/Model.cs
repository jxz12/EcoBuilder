using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EcoBuilder.Model
{
    public class Model : MonoBehaviour
    {   
        ///////////////////////////////////////////////////////////
        // MODEL DESCRIPION:
        //  foraging for plants follows a grazing strategy
        //  foraging for animals follows active foraging
        //  Pawar et al. (2012)
        ///////////////////////////////////////////////////////////

        [SerializeField]
        double r0 =   1.71e-6,     // production 
               z0 =   4.15e-8,     // loss constant
               a0 =   8.31e-4,     // search rate 
               beta = 0.75,        // metabolism expopnent
               p_v =  0.26,        // velocity exponent
               p_d =  0.21,        // reaction distance exponent

               e_p = 0.2,         // plant efficiency
               e_c = 0.5,         // animal efficiency

               kg_min =   1e-3,   // min body size
               kg_max =   1e3,    // max body size
               a_ii_min = 1e-5,   // min self-regulation
               a_ii_max = 1       // max self-regulation
               ;

        class Species
        {
            public int Idx { get; private set; }
            public double BodySize { get; set; }
            public bool IsProducer { get; set; }

            public double Metabolism { get; set; }
            public double SelfRegulation { get; set; }
            public double Efficiency { get; set; }

            public double Abundance { get; set; } = 1; // initialise as non-extinct

            public Species(int idx)
            {
                Idx = idx;
            }
        }

        ///////////////////////////////////////////////////////////////////
        // search rate derivations
        
        // NOTE: THESE ARE NOT MASS-SPECIFIC, THEY ARE INDIVIDUAL-SPECIFIC
        double ActiveCapture(double m_r, double m_c)
        {
            double k_rc = m_r/m_c;
            return a0 * Math.Pow(m_c, p_v+2*p_d) *
                        Math.Sqrt(1+Math.Pow(k_rc, 2*p_v)) *
                        Math.Pow(k_rc, p_d);
        }
        double Grazing(double m_r, double m_c)
        {
            double k_rc = m_r/m_c;
            return a0 * Math.Pow(m_c, p_v+2*p_d) *
                        Math.Pow(k_rc, p_d);
        }
        double SitAndWait(double m_r, double m_c)
        {
            double k_rc = m_r/m_c;
            return a0 * Math.Pow(m_c, p_v+2*p_d) *
                        Math.Pow(k_rc, p_v+p_d);
        }
        // MAKES THINGS MASS-SPECIFIC
        double CalculateForaging(Species resource, Species consumer)
        {
            if (!resource.IsProducer && !consumer.IsProducer)
            {
                return ActiveCapture(resource.BodySize, consumer.BodySize) / consumer.BodySize;
            }
            else if (resource.IsProducer && !consumer.IsProducer)
            {
                return Grazing(resource.BodySize, consumer.BodySize) / consumer.BodySize;
            }
            else
            {
                throw new Exception("impossible foraging");
            }
        }
        
                                
        LotkaVolterra<Species> simulation;
        void Awake()
        {
            simulation = new LotkaVolterra<Species>(
                  (s)=> s.Metabolism,
                  (s)=> s.SelfRegulation,
                (r,c)=> CalculateForaging(r,c), // takes foraging strategy into account
                (r,c)=> r.Efficiency            // only depends on resource type
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

            if (idxToSpecies.Count == 0)
            {
                Feasible = Stable = Nonreactive = false;
                // TODO: fix this because OnCalculated() makes nodelink try to get abundances
                // OnCalculated.Invoke();
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

            double sizeScaling = Math.Pow(s.BodySize, beta-1);
            s.Metabolism = s.IsProducer? sizeScaling * r0 : -sizeScaling * z0;
            s.Efficiency = s.IsProducer? e_p : e_c;

            equilibriumSolved = false;
        }
        public void SetSpeciesBodySize(int idx, float sizeNormalised)
        {
            Species s = idxToSpecies[idx];
            s.BodySize = GetOnLogScale(sizeNormalised, kg_min, kg_max);

            double sizeScaling = Math.Pow(s.BodySize, beta-1);
            s.Metabolism = s.IsProducer? sizeScaling * r0 : -sizeScaling * z0;

            equilibriumSolved = false;
        }
        public void SetSpeciesGreediness(int idx, float greedNormalised)
        {
            idxToSpecies[idx].SelfRegulation = -GetOnLogScale(greedNormalised, a_ii_max, a_ii_min);

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









        [SerializeField] GameObject busyIcon;
        bool equilibriumSolved = true, calculating = false;
        void LateUpdate()
        {
            if (!equilibriumSolved && !calculating && idxToSpecies.Count>0)
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
        // public float Complexity { get; private set; } = 0;
        // public float Flux { get; private set; } = 0;

        async void Equilibrium()
        {
            calculating = true;
            Feasible = await Task.Run(() => simulation.SolveFeasibility());
            Stable = await Task.Run(() => simulation.SolveStability());
            Nonreactive = await Task.Run(() => simulation.SolveReactivity());

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
            return (float)simulation.GetSolvedAbundance(idxToSpecies[idx]);
        }
        public float GetFlux(int res, int con)
        {
            return (float)simulation.GetSolvedFlux(idxToSpecies[res], idxToSpecies[con]);
        }
    }
}