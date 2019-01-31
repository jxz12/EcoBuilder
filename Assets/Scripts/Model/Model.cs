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
        [SerializeField] IntEvent OnRescued;
        [SerializeField] IntEvent OnExtinction;
        [SerializeField] IntFloatEvent OnAbundanceSet;
        [SerializeField] IntIntFloatEvent OnFluxSet;

        [SerializeField] float heartRate=60; // frequency of LAS calculations
        // [SerializeField] int countdown=5; // how many heartbeats until death
        [SerializeField] Monitor monitor;

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
                    metabolism = (value+.1);
                    Growth = isProducer? (value+.1)*b0 : -(value+.1)*d0;
                    Attack = (value+.1) * a0;
                }
            }
            public double Greediness {
                set {
                    SelfReg = a_ii0 * (-1.1 + value);
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

            scaledAbundances[newSpecies] = 0;
        }
        public void RemoveSpecies(int idx)
        {
            Species toRemove = idx2Species[idx];
            simulation.RemoveSpecies(toRemove);
            idx2Species.Remove(idx);

            scaledAbundances.Remove(toRemove);
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
            simulation.OnAbundanceSet += SetAndScaleAbundance;
            simulation.OnFluxSet += SetAndScaleFlux;
        }

        /////////////////////
        // mathy bits here

        List<Action> UnityEventsTodo = new List<Action>();
        async void Equilibrium()
        {
            bool feasible = await Task.Run(() => simulation.SolveEquilibrium());

            foreach (Action a in UnityEventsTodo)
                a();
            UnityEventsTodo.Clear();

            print("Abund: " + minLogAbund + " " + maxLogAbund);

            // move mins and maxs slowly towards each other, so that any huge min/max isn't permanent
            if (minLogAbund != double.MaxValue && maxLogAbund != double.MinValue)
            {
                double minMaxGapAbund = maxLogAbund - minLogAbund;
                minLogAbund += minMaxGapAbund * minMaxTweenSpeed;
                maxLogAbund -= minMaxGapAbund * minMaxTweenSpeed;
            }

            if (minLogFlux != double.MaxValue && maxLogFlux != double.MinValue)
            {
                double minMaxGapFlux = maxLogFlux - minLogFlux;
                minLogFlux += minMaxGapFlux * minMaxTweenSpeed;
                maxLogFlux -= minMaxGapFlux * minMaxTweenSpeed;
            }


            if (feasible)
                CalculateAndSetStability();
            else
                monitor.Debug("infeasible");
        }

        // ------------------ topTop
        // ------------------ top
        //     UNBUFFERED
        // ------------------ bot
        // ------------------ botBot
        // always leave a buffer, if entered simply tween buffer STARTs slowly
        // if ends of buffer are reached, set buffer ENDs instantly

        [SerializeField] double minMaxTweenSpeed = .1f;
        double minLogAbund = double.MaxValue, maxLogAbund = double.MinValue;
        private static readonly double myEps = 1e-10f; // real double.Epsilon = 5e-324

        Dictionary<Species, float> scaledAbundances = new Dictionary<Species, float>();
        HashSet<Species> endangered = new HashSet<Species>();
        private void SetAndScaleAbundance(Species s, double abundance)
        {
            if (abundance <= 0)
            {
                if (!endangered.Contains(s))
                {
                    endangered.Add(s);
                    UnityEventsTodo.Add(()=> OnEndangered.Invoke(s.Idx));
                }
                // TODO: start countdown to zero if any species is in endangered
                //  Keep track of which species has the most negative abundance
                //  only kill that one species if countdown reaches zero
                //   then start countdown again
                //   if two species are tied, then kill the newer one?
                scaledAbundances[s] = 0;
            }
            else
            {
                if (endangered.Contains(s))
                {
                    endangered.Remove(s);
                    UnityEventsTodo.Add(()=>OnRescued.Invoke(s.Idx));
                }
                // TODO: this will have to be changed back once logs are put back in
                // double logAbundance = Math.Log10(abundance);
                double logAbundance = abundance;
                minLogAbund = Math.Min(logAbundance, minLogAbund) - myEps;
                maxLogAbund = Math.Max(logAbundance, maxLogAbund) + myEps;

                print(s.Idx + ": " + logAbundance);

                float scaled = (float)((logAbundance-minLogAbund) / (maxLogAbund-minLogAbund));
                scaledAbundances[s] = scaled;
            }
            UnityEventsTodo.Add(()=> OnAbundanceSet.Invoke(s.Idx, scaledAbundances[s]));
        }

        double minLogFlux = double.MaxValue, maxLogFlux = double.MinValue;
        private void SetAndScaleFlux(Species res, Species con, double flux)
        {
            // TODO: this will have to be changed back once logs are put back in
            // double logFlux = Math.Log10(flux);


            // TODO: This method is making lines way too thin almost all the time
            double logFlux = flux;
            // double logFlux = flux * scaledAbundances[res]*scaledAbundances[con]; // might be better otherwise
            minLogFlux = Math.Min(logFlux, minLogFlux) - myEps;
            maxLogFlux = Math.Max(logFlux, maxLogFlux) + myEps;

            print(res.Idx + " " + con.Idx + " " + logFlux);

            float scaled = (float)((logFlux-minLogFlux) / (maxLogFlux-minLogFlux));
            UnityEventsTodo.Add(()=> OnFluxSet.Invoke(res.Idx, con.Idx, scaled * scaledAbundances[res]*scaledAbundances[con]));
            // UnityActionsTodo.Add(()=> OnFluxSet.Invoke(res.Idx, con.Idx, scaled));
        }


        private async void CalculateAndSetStability()
        {
            double stability = await Task.Run(() => simulation.LocalAsymptoticStability());
            monitor.Debug(stability.ToString("E"));
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