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

        // [SerializeField] IntFloatEvent OnAbundanceSet;
        // [SerializeField] IntIntFloatEvent OnFluxSet;

        class Species
        {
            public int Idx { get; private set; }
            bool isProducer = true;
            public bool IsProducer {
                set {
                    isProducer = value;
                    Growth = isProducer? metabolism*b0 : -metabolism*d0;
                    Efficiency = isProducer? 0.2 : 0.5;
                }
            }
            double metabolism = 1; // metabolism is mass^-.25
            public double BodySize {
                set {
                    // metabolism = Math.Pow(value, -.25);
                    metabolism = 1.1 - value;
                    Growth = isProducer? metabolism*b0 : -metabolism*d0;
                    Attack = metabolism * a0;
                }
            }
            public double Greediness {
                set {
                    SelfReg = -a_ii0 * (1.1 - value);
                }
            }
            public double Growth { get; private set; } = 0;
            public double SelfReg { get; private set; } = -1;
            public double Attack { get; private set; } = 0;
            public double Efficiency { get; private set; } = 0;

            public double Abundance { get; set; } = 1; // initialise as non-extinct

            public Species(int idx)
            {
                Idx = idx;
            }

            static readonly double b0 = 1,
                                   d0 = .1,
                                   a_ii0 = 10,
                                   a0 = 10;

            // static readonly double b0 = 1e0,
            //                        d0 = 1e-2,
            //                        a0 = 1e0,
            //                        a_ii0 = 1e-1,
            //                        beta = 0.75;
        }

        LotkaVolterra<Species> simulation;
        void Awake()
        {
            simulation = new LotkaVolterra<Species>(
                s=> s.Growth,
                s=> s.SelfReg,
                (r,c)=> r.Attack,
                (r,c)=> r.Efficiency
            );

            // simulation.OnAbundanceSet += (s,x)=> abundances[s] = x;
            // simulation.OnFluxSet += SetAndScaleFlux;
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
        public void SetSpeciesAsProducer(int idx)
        {
            idxToSpecies[idx].IsProducer = true;
            equilibriumSolved = false;
        }
        public void SetSpeciesAsConsumer(int idx)
        {
            idxToSpecies[idx].IsProducer = false;
            equilibriumSolved = false;
        }
        public void SetSpeciesBodySize(int idx, float size)
        {
            idxToSpecies[idx].BodySize = size;
            equilibriumSolved = false;
        }
        public void SetSpeciesGreediness(int idx, float greed)
        {
            idxToSpecies[idx].Greediness = greed;
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



        /////////////////////
        // mathy bits here

        /*
        private static readonly double myEps = 1e-10f; // real double.Epsilon = 5e-324
        HashSet<Species> endangered = new HashSet<Species>();
        Species critical = null;

        private void SetAndScaleAbundances() // return most endangered
        {
            // double minPosAbund = double.MaxValue, maxAbund = 0;
            // double minNegAbund = 0;

            // foreach (Species s in abundances.Keys)
            // {
            //     double abund = abundances[s];
            //     if (abund <= 0)
            //     {
            //         if (!endangered.Contains(s))
            //         {
            //             endangered.Add(s);
            //             OnEndangered.Invoke(s.Idx);
            //         }
            //         if (abund < minNegAbund)
            //         {
            //             minNegAbund = abund;
            //             critical = s;
            //         }
            //     }
            //     else
            //     {
            //         if (endangered.Contains(s))
            //         {
            //             endangered.Remove(s);
            //             OnRescued.Invoke(s.Idx);
            //         }
            //         minPosAbund = Math.Min(minPosAbund, abund-myEps);
            //         maxAbund = Math.Max(maxAbund, abund+myEps);
            //     }
            // }
            // Func<double, float> Scale = x=> (float)((x-minPosAbund)/(maxAbund-minPosAbund));
            // foreach (Species s in abundances.Keys)
            // {
            //     double abund = abundances[s];
            //     if (abund <= 0)
            //     {
            //         float newAbund = Mathf.Lerp(scaledAbundances[s.Idx], 0, .5f);
            //         scaledAbundances[s.Idx] = newAbund;
            //     }
            //     else
            //     {
            //         float scaledAbund = Scale(abund);
            //         float newAbund = Mathf.Lerp(scaledAbundances[s.Idx], scaledAbund, .5f);
            //         scaledAbundances[s.Idx] = newAbund;
            //     }
            // }
            // foreach (int idx in scaledAbundances.Keys)
            // {
            //     OnAbundanceSet.Invoke(idx, scaledAbundances[idx]);
            // }

            Func<double, float> Scale = x=>(float)x;

            double minNegAbund = 0;
            Species argMin;
            foreach (Species s in abundances.Keys)
            {
                double abund = abundances[s];
                // print(s.Idx + " " + abund);
                if (abund <= 0)
                {
                    if (!endangered.Contains(s))
                    {
                        endangered.Add(s);
                        OnEndangered.Invoke(s.Idx);
                    }
                    if (abund < minNegAbund)
                    {
                        minNegAbund = abund;
                        critical = s;
                    }
                    float lerpedAbund = Mathf.Lerp(scaledAbundances[s.Idx], 0, .25f);
                    scaledAbundances[s.Idx] = lerpedAbund;
                }
                else
                {
                    if (endangered.Contains(s))
                    {
                        endangered.Remove(s);
                        OnRescued.Invoke(s.Idx);
                    }
                    float lerpedAbund = Mathf.Lerp(scaledAbundances[s.Idx], Scale(abund), .25f);
                    scaledAbundances[s.Idx] = lerpedAbund;
                }
            }
            foreach (int idx in scaledAbundances.Keys)
            {
                OnAbundanceSet.Invoke(idx, scaledAbundances[idx]);
            }
        }

        // ------------------ topTop
        // ------------------ top
        //     UNBUFFERED
        // ------------------ bot
        // ------------------ botBot
        // always leave a buffer, if entered simply tween buffer STARTs slowly
        // if ends of buffer are reached, set buffer ENDs instantly

        // [SerializeField] double minMaxTweenSpeed = .1f;
        // double minLogAbund = double.MaxValue, maxLogAbund = double.MinValue;

        // Dictionary<Species, float> scaledAbundances = new Dictionary<Species, float>();
        // private void SetAndScaleAbundance(Species s, double abundance)
        // {
        //     if (abundance <= 0)
        //     {
        //         if (!endangered.Contains(s))
        //         {
        //             endangered.Add(s);
        //             UnityEventsTodo.Add(()=> OnEndangered.Invoke(s.Idx));
        //         }
        //         scaledAbundances[s] = Mathf.Lerp(scaledAbundances[s], 0, .5f);
        //     }
        //     else
        //     {
        //         if (endangered.Contains(s))
        //         {
        //             endangered.Remove(s);
        //             UnityEventsTodo.Add(()=>OnRescued.Invoke(s.Idx));
        //         }
        //         // TODO: this will have to be changed back once logs are put back in
        //         // double logAbundance = Math.Log10(abundance);
        //         double logAbundance = abundance;
        //         minLogAbund = Math.Min(logAbundance, minLogAbund) - myEps;
        //         maxLogAbund = Math.Max(logAbundance, maxLogAbund) + myEps;

        //         // print(s.Idx + ": " + logAbundance);

        //         float scaled = (float)((logAbundance-minLogAbund) / (maxLogAbund-minLogAbund));
        //         scaledAbundances[s] = Mathf.Lerp(scaledAbundances[s], scaled, .5f);
        //     }

        //     UnityEventsTodo.Add(()=> OnAbundanceSet.Invoke(s.Idx, scaledAbundances[s]));
        // }

        // double minLogFlux = double.MaxValue, maxLogFlux = double.MinValue;
        // private void SetAndScaleFlux(Species res, Species con, double flux)
        // {
        //     // TODO: this will have to be changed back once logs are put back in
        //     // double logFlux = Math.Log10(flux);


        //     // TODO: This method is making lines way too thin almost all the time
        //     double logFlux = flux;
        //     // double logFlux = flux * scaledAbundances[res]*scaledAbundances[con]; // might be better otherwise
        //     minLogFlux = Math.Min(logFlux, minLogFlux) - myEps;
        //     maxLogFlux = Math.Max(logFlux, maxLogFlux) + myEps;

        //     // print(res.Idx + " " + con.Idx + " " + logFlux);

        //     float scaled = (float)((logFlux-minLogFlux) / (maxLogFlux-minLogFlux));
        //     // UnityEventsTodo.Add(()=> OnFluxSet.Invoke(res.Idx, con.Idx, scaled * scaledAbundances[res]*scaledAbundances[con]));
        //     // UnityActionsTodo.Add(()=> OnFluxSet.Invoke(res.Idx, con.Idx, scaled));
        // }


        private async void CalculateAndSetStability()
        {
            // double stability = await Task.Run(() => simulation.LocalAsymptoticStability());
            double stability = simulation.LocalAsymptoticStability();
            // monitor.Debug(stability.ToString("E3"));
        }
        */
    }
}