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
        }
        public void RemoveSpecies(int idx)
        {
            Species toRemove = idx2Species[idx];
            simulation.RemoveSpecies(toRemove);
            idx2Species.Remove(idx);
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
                s => s.Growth,
                s => s.SelfReg,
                (r,c) => r.Attack,
                (r,c) => r.Efficiency
            );
            simulation.OnAbundanceSet += SetAndScaleAbundance;
        }

        /////////////////////
        // mathy bits here

        async void Equilibrium()
        {
            bool feasible = await Task.Run(() => simulation.SolveEquilibrium());

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
        // float topTop, top, bot, botBot;

        HashSet<Species> endangered = new HashSet<Species>();
        Dictionary<int, float> scaledAbundances = new Dictionary<int, float>();
        private void SetAndScaleAbundance(Species s, double abundance)
        {
            print(s.Idx + " " + abundance);
            if (abundance <= 0)
            {
                if (!endangered.Contains(s))
                {
                    endangered.Add(s);
                    OnEndangered.Invoke(s.Idx);
                }
            }
            else
            {
                if (endangered.Contains(s))
                {
                    endangered.Remove(s);
                    OnRescued.Invoke(s.Idx);
                }
            }

            // TODO: do scaling and tweening instead of this
            scaledAbundances[s.Idx] = abundance<=0? 0 : .5f;
            OnAbundanceSet.Invoke(s.Idx, scaledAbundances[s.Idx]);

            // want to scale from min to max, also abs so 0-1
            // problems:
            //  - if the min/max in/decreases, the size should in/decrease as well
            //    but if we just scale from min to max they will stay the same size
            //  - despite how similar two nodes are in absolute size, the relative
            //    size is maxed out if there are only two of them
            //
            // solutions:
            //  - always keep a buffer either side in order to have room to move
            //    and
            //  - e.g. 0.25-0.75 is actual range, and if e.g. the min drops then
            //    0.0-0.25 is new min to old min
            //  - actual min then slowly move towards new in
            //  - keep track of min/max ever in order to make relative better
        }

        private void SetAndScaleFlux(Species res, Species con, double flux)
        {
            print(res.Idx + " " + con.Idx + " " + flux);
            // OnFluxSet.Invoke(res.Idx, con.Idx, 1);
            OnFluxSet.Invoke(res.Idx, con.Idx, UnityEngine.Random.Range(.2f, 1));
        }


        private async void CalculateAndSetStability()
        {
            double stability = await Task.Run(() => simulation.LocalAsymptoticStability());
            monitor.Debug(stability.ToString("E"));
        }

        private void Start()
        {
            StartCoroutine(Pulse(60f / heartRate));
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