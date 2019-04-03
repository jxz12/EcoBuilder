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
        [SerializeField] IntEvent OnCritical;
        [SerializeField] IntEvent OnRescued;
        [SerializeField] IntEvent OnExtinction;
        [SerializeField] IntFloatEvent OnAbundanceSet;
        [SerializeField] IntIntFloatEvent OnFluxSet;

        // [SerializeField] float heartRate=60; // frequency of LAS calculations
        // [SerializeField] int countdownMax=5; // how many heartbeats until death
        // [SerializeField] Monitor monitor;

        class Species
        {
            public int Idx { get; private set; }
            bool isProducer;
            public bool IsProducer {
                set {
                    isProducer = value;
                    Growth = value? metabolism*b0 : -metabolism*d0;
                    Efficiency = value? 0.2 : 0.5;
                }
            }
            double metabolism;
            public double Metabolism {
                set {
                    metabolism = value;
                    Growth = isProducer? value*b0 : -value*d0;
                    Attack = value * a0;
                }
            }
            public double Greediness {
                set {
                    SelfReg = -a_ii0 * value;
                }
            }
            public double Growth { get; private set; } = 0;
            public double SelfReg { get; private set; } = 0;
            public double Attack { get; private set; } = 0;
            public double Efficiency { get; private set; } = 0;

            public Species(int idx)
            {
                Idx = idx;
            }

            static readonly double b0 = 1,
                                   d0 = .1,
                                   a_ii0 = 1,
                                   a0 = 10;

            // static readonly double b0 = 1e0,
            //                        d0 = 1e-2,
            //                        a0 = 1e0,
            //                        a_ii0 = 1e-1,
            //                        beta = 0.75;
        }

        LotkaVolterra<Species> simulation;
        Dictionary<int, Species> idx2Species = new Dictionary<int, Species>();

        public void AddSpecies(int idx)
        {
            var newSpecies = new Species(idx);
            simulation.AddSpecies(newSpecies);
            idx2Species.Add(idx, newSpecies);

            scaledAbundances[newSpecies.Idx] = .5f;
        }
        public void RemoveSpecies(int idx)
        {
            Species toRemove = idx2Species[idx];
            simulation.RemoveSpecies(toRemove);
            idx2Species.Remove(idx);

            abundances.Remove(toRemove);
            scaledAbundances.Remove(idx);
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

        void Awake()
        {
            simulation = new LotkaVolterra<Species>(
                s=> s.Growth,
                s=> s.SelfReg,
                (r,c)=> r.Attack,
                (r,c)=> r.Efficiency
            );
            simulation.OnAbundanceSet += (s,x)=> abundances[s] = x;
            // simulation.OnFluxSet += SetAndScaleFlux;
        }

        private void Start()
        {
            // StartCoroutine(Pulse(60f / heartRate));
        }
        public UnityEvent OnPulse;
        IEnumerator Pulse(float delay)
        {
            while (true)
            {
                OnPulse.Invoke();
                if (idx2Species.Count > 0)
                    Equilibrium();
                yield return new WaitForSeconds(delay);
            }
        }

        /////////////////////
        // mathy bits here

        Dictionary<Species, double> abundances = new Dictionary<Species, double>();
        Dictionary<int, float> scaledAbundances = new Dictionary<int, float>();

        int countdown = -1;
        List<Action> UnityEventsTodo = new List<Action>();
        Species toExtinct = null;
        async void Equilibrium()
        {
            // bool feasible = await Task.Run(() => simulation.SolveEquilibrium());
            bool feasible = simulation.SolveEquilibrium();

            // print("Abund: " + minLogAbund + " " + maxLogAbund);

            // // move mins and maxs slowly towards each other, so that any huge min/max isn't permanent
            // if (minLogAbund != double.MaxValue && maxLogAbund != double.MinValue)
            // {
            //     double minMaxGapAbund = maxLogAbund - minLogAbund;
            //     minLogAbund += minMaxGapAbund * minMaxTweenSpeed;
            //     maxLogAbund -= minMaxGapAbund * minMaxTweenSpeed;
            // }

            // if (minLogFlux != double.MaxValue && maxLogFlux != double.MinValue)
            // {
            //     double minMaxGapFlux = maxLogFlux - minLogFlux;
            //     minLogFlux += minMaxGapFlux * minMaxTweenSpeed;
            //     maxLogFlux -= minMaxGapFlux * minMaxTweenSpeed;
            // }

            SetAndScaleAbundances();

            if (feasible)
            {
                CalculateAndSetStability();
                countdown = -1;
            }
            else
            {
                // monitor.Debug("infeasible");

                // if (countdown == -1) // if newly endangered
                //     countdown = countdownMax;
                // else
                //     countdown -= 1;

                if (countdown <= 2)
                {
                    if (toExtinct == null)
                    {
                        OnCritical.Invoke(critical.Idx);
                        toExtinct = critical;
                    }
                    else if (toExtinct != critical)
                    {
                        OnEndangered.Invoke(toExtinct.Idx);
                        OnCritical.Invoke(critical.Idx);
                        toExtinct = critical;
                    }
                }
                // print(countdown);
                if (countdown == 0)
                {
                    RemoveSpecies(critical.Idx);
                    OnExtinction.Invoke(critical.Idx);
                    // make extinct
                    // if two species are tied, then kill the newer one?
                    // countdown = countdownMax;
                    toExtinct = null;
                }
            }
        }
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

    }
}