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
        //  we use a single invisible node as nutrients
        //  'foraging' for nutrients follows a grazing strategy (is this okay?)
        //  foraging for plants also follows a grazing strategy
        //  foraging for animals follows active foraging
        ///////////////////////////////////////////////////////////

        [SerializeField] double p_v = 0.26,        // velocity exponent
                                v0 =  0.33,        // velocity constant
                                p_d = 0.21,        // reaction distance exponent
                                d0 =  1.62,        // reaction distance constant

                                e_n = 1.0,         // nutrient efficiency
                                e_p = 0.2,         // plant efficiency
                                e_c = 0.5,         // animal efficiency

                                kg_min =   1e-3,   // min body size
                                kg_max =   1e3,    // max body size
                                a_ii_min = 1e-8,   // min self-regulation
                                a_ii_max = 1e-2,   // max self-regulation
                                
                                z0 =   4.15e-8,    // death constant
                                r0  =  8.31e-4,    // nutrient absorption constant
                                // beta = 0.75,       // metabolic scaling exponent
                                // r_n =  10,         // nutrient growth rate
                                // K_n =   1          // nutrient carrying capacity
                                ;

        class Species
        {
            public int Idx { get; private set; }
            public double BodySize { get; set; }
            public bool Active { get; set; }

            public double Metabolism { get; set; }
            public double SelfRegulation { get; set; }
            public double Efficiency { get; set; }

            public double Abundance { get; set; } = 1; // initialise as non-extinct

            public Species(int idx)
            {
                Idx = idx;
            }
        }
        double ActiveCapture(double m_r, double m_c)
        {
            double k_rc = m_r/m_c;
            return 2*v0*d0 * Math.Pow(m_c, p_v+2*p_d) *
                             Math.Sqrt(1+Math.Pow(k_rc, 2*p_v)) *
                             Math.Pow(k_rc, p_d);
        }
        double Grazing(double m_r, double m_c)
        {
            double k_rc = m_r/m_c;
            return 2*v0*d0 * Math.Pow(m_c, p_v+2*p_d) *
                             Math.Pow(k_rc, p_d);
        }
        double SitAndWait(double m_r, double m_c)
        {
            double k_rc = m_r/m_c;
            return 2*v0*d0 * Math.Pow(m_c, p_v+2*p_d) *
                             Math.Pow(k_rc, p_v+p_d);
        }
        double NutrientAbsorption(double m_c)
        {
            // TODO: just make the nutrient the mean of body size and selfreg
        }
        double SpeciesInteraction(Species resource, Species consumer)
        {
            if (resource.Active && consumer.Active)
                return ActiveCapture(resource.BodySize, consumer.BodySize);
            else if (!resource.Active && consumer.Active)
                return Grazing(resource.BodySize, consumer.BodySize);
            else if  (resource.Active && !consumer.Active)
                return SitAndWait(resource.BodySize, consumer.BodySize);
            else
                return 0;
        }
        
                                
        LotkaVolterra<Species> simulation;
        Species nutrient;
        void Awake()
        {
            simulation = new LotkaVolterra<Species>(
                  (s)=> s.Metabolism,
                  (s)=> s.SelfRegulation,
                (r,c)=> r==nutrient? Grazing(),
                (r,c)=> r.Efficiency
            );

            nutrient = new Species(int.MinValue);
            nutrient.Metabolism = r_n;
            nutrient.SelfRegulation = r_n/K_n;
            nutrient.Efficiency = e_n;
            // bodysize and production does not matter for nutrient

            simulation.AddSpecies(nutrient);
        }
        Dictionary<int, Species> idxToSpecies = new Dictionary<int, Species>();

        public void AddSpecies(int idx)
        {
            if (idx == int.MinValue)
                throw new Exception("idx reserved for nutrient");

            var newSpecies = new Species(idx);
            simulation.AddSpecies(newSpecies);
            idxToSpecies.Add(idx, newSpecies);
            equilibriumSolved = false;
        }
        public void RemoveSpecies(int idx)
        {
            if (idx == int.MinValue)
                throw new Exception("idx reserved for nutrient");

            Species toRemove = idxToSpecies[idx];
            simulation.RemoveSpecies(toRemove);
            idxToSpecies.Remove(idx);
            equilibriumSolved = false;
            // else
            // {
            //     // TODO: fix this because OnCalculated() makes nodelink try to get abundances
            //     Feasible = Stable = Nonreactive = false;
            //     OnCalculated.Invoke();
            // }
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
            s.Search = velocity * 1;//a0;

            equilibriumSolved = false;
        }
        public void SetSpeciesGreediness(int idx, float greedNormalised)
        {
            idxToSpecies[idx].SelfReg = -GetOnLogScale(greedNormalised, a_ii_max, a_ii_min);
            // if (idxToSpecies[idx].IsProducer)
            //     idxToSpecies[idx].SelfReg /= 100;
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