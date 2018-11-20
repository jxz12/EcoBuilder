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
        [Serializable] class IntFloatEvent : UnityEvent<int, float> { }
        [SerializeField] IntFloatEvent AbundanceSetEvent;
        [SerializeField] float heartRate = 30;

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

        Dictionary<int, Species> speciesDict = new Dictionary<int, Species>();

        public void AddSpecies(int idx)
        {
            var newSpecies = new Species(idx);
            simulation.AddSpecies(newSpecies);
            speciesDict.Add(idx, newSpecies);
        }
        public void RemoveSpecies(int idx)
        {
            // nichess.RemovePiece(idx);
            // nodeLink.RemoveNode(idx);
            simulation.RemoveSpecies(speciesDict[idx]);

            speciesDict.Remove(idx);
            print(speciesDict.Count);
        }
        public void SetSpeciesAsProducer(int idx)
        {
            speciesDict[idx].IsProducer = true;
        }
        public void SetSpeciesAsConsumer(int idx)
        {
            speciesDict[idx].IsProducer = false;
        }
        public void SetSpeciesBodyMass(int idx, float bodyMass)
        {
            speciesDict[idx].BodyMass = bodyMass;
        }
        public void SetSpeciesGreediness(int idx, float greediness)
        {
            speciesDict[idx].Intraspecific = greediness-1;
        }

        public void AddInteraction(int resource, int consumer)
        {
            if (resource == consumer)
                throw new Exception("can't eat itself");

            Species res = speciesDict[resource], con = speciesDict[consumer];
            simulation.AddInteraction(res, con);
        }
        public void RemoveInteraction(int resource, int consumer)
        {
            if (resource == consumer)
                throw new Exception("can't eat itself");

            Species res = speciesDict[resource], con = speciesDict[consumer];
            simulation.RemoveInteraction(res, con);
        }

        public void ConfigFromString(string config)
        {
            throw new NotImplementedException();
        }
        public string GetConfigString()
        {
            throw new NotImplementedException();
        }



        async void Equilibrium()
        {
            await Task.Run(() => simulation.SolveEquilibrium());
            // monitor.SetHeight(model.MaxAbundance + " " + model.MinAbundance);
            foreach (int i in speciesDict.Keys)
            {
                float size = (float)simulation.GetAbundance(speciesDict[i]);
                if (size > 0)
                    AbundanceSetEvent.Invoke(i, size);
                else
                    AbundanceSetEvent.Invoke(i, .5f);
            }

            // // calculate stability if feasible
            // if (model.MinAbundance > 0)
            // {
            //     double stability = await Task.Run(() => model.LocalAsymptoticStability());
            //     monitor.SetLAS(stability.ToString());
            // }
            // else
            // {
            //     monitor.SetLAS("infeasible");
            // }
        }

        private void Start()
        {
            StartCoroutine(Pulse(60f / heartRate));
        }

        IEnumerator Pulse(float delay)
        {
            while (true)
            {
                if (speciesDict.Count > 0)
                    Equilibrium();
                yield return new WaitForSeconds(delay);
            }
        }

    }
}