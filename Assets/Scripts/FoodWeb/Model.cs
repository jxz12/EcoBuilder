using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using System;
using System.Collections.Generic;

#if !UNITY_WEBGL
using System.Threading.Tasks;
#endif

namespace EcoBuilder.FoodWeb
{
    public class Model : MonoBehaviour
    {   
        public event Action OnEquilibrium;
        public event Action<int> OnEndangered;
        public event Action<int> OnRescued;

        ///////////////////////////////////////////////////////////
        // MODEL DESCRIPION:
        //  foraging for plants follows a grazing strategy
        //  foraging for animals follows active foraging
        //  Pawar et al. (2012)
        ///////////////////////////////////////////////////////////

        [SerializeField] /*[ReadOnly]*/
        double r0 =   1.71e-6,     // production 
               z0 =   4.15e-8,     // loss constant
               a0 =   8.31e-4,     // search rate 
               beta = 0.75,        // metabolism expopnent
               p_v =  0.26,        // velocity exponent
               p_d =  0.21,        // reaction distance exponent

               e_p = 0.2,          // plant efficiency
               e_c = 0.5,          // animal efficiency

               kg_min = 1e-3,     // min body size
               kg_max = 1e3       // max body size
               ;

        [SerializeField] /*[ReadOnly]*/
        double a_min = 1e-5,
               a_max = 1;

        [SerializeField] /*[ReadOnly]*/
        double minRealAbund = 2e-10f,
               maxRealAbund = 1.95f,
               minRealFlux = 1e-16f,
               maxRealFlux = 1e-6f,
               minRealPlex = 2.1e-5f,
               maxRealPlex = 6.3e-3f;

        private double minLogAbund, maxLogAbund,
                       minLogFlux, maxLogFlux,
                       minLogPlex, maxLogPlex;

        private class Species
        {
            // you may be asking: why have another class for this?
            // so that some values can be cached instead of calculated every time.
            public int Idx;
            public bool IsProducer = false;
            public double BodySize = double.NaN;
            public double Interference = double.NaN;

            public double Metabolism = double.NaN;
            public double Efficiency = double.NaN;

            public bool? Endangered = null;

            public Species(int idx)
            {
                Idx = idx;
            }
        }
        
        //////////////////////////////
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
            Assert.IsFalse(consumer.IsProducer, "producers cannot have prey");

            if (!resource.IsProducer) {
                return ActiveCapture(resource.BodySize, consumer.BodySize) / consumer.BodySize;
            } else {
                return Grazing(resource.BodySize, consumer.BodySize) / consumer.BodySize;
            }
        }
        
                                
        LotkaVolterra<Species> simulation;
        void Awake()
        {
            // calculate bounds for a_ij and set a_ii in the same range
            // a_max = ActiveCapture(kg_max, kg_min) / kg_min;
            // a_min = Grazing(kg_min, kg_max) / kg_max;
            // print(a_min+" "+a_max);

            simulation = new LotkaVolterra<Species>(
                  (s)=> s.Metabolism,
                  (s)=> s.Interference,
                (r,c)=> CalculateForaging(r,c), // takes foraging strategy into account
                (r,c)=> r.Efficiency            // only depends on resource type
            );
            minLogAbund = Math.Log10(minRealAbund);
            maxLogAbund = Math.Log10(maxRealAbund);
            minLogFlux = Math.Log10(minRealFlux);
            maxLogFlux = Math.Log10(maxRealFlux);
            minLogPlex = Math.Log10(minRealPlex);
            maxLogPlex = Math.Log10(maxRealPlex);
        }
        Dictionary<int, Species> idxToSpecies = new Dictionary<int, Species>();
        Dictionary<Species, int> speciesToIdx = new Dictionary<Species, int>();

        public void AddSpecies(int idx, bool isProducer)
        {
            Assert.IsFalse(idxToSpecies.ContainsKey(idx), "already contains idx " + idx);

            var newSpecies = new Species(idx);
            simulation.AddSpecies(newSpecies);

            newSpecies.IsProducer = isProducer;
            newSpecies.Efficiency = isProducer? e_p : e_c;

            idxToSpecies.Add(idx, newSpecies);
            speciesToIdx.Add(newSpecies, idx);
        }
        public void RemoveSpecies(int idx)
        {
            Assert.IsTrue(idxToSpecies.ContainsKey(idx), "does not contain idx " + idx);

            Species toRemove = idxToSpecies[idx];
            simulation.RemoveSpecies(toRemove);

            idxToSpecies.Remove(idx);
            speciesToIdx.Remove(toRemove);
        }

        // we need this because the graveyard in nodelink makes it nontrivial
        // to store a copy of the whole graph by only relying on OnLinked<int,int>
        Func<int, IEnumerable<int>> AttachedAdjacency;
        public void AttachAdjacency(Func<int, IEnumerable<int>> Adjacency)
        {
            AttachedAdjacency = Adjacency;
        }
        private IEnumerable<Species> GetConsumers(Species resource)
        {
            foreach (int con in AttachedAdjacency(speciesToIdx[resource]))
            {
                yield return idxToSpecies[con];
            }
        }
        // trigger is necessary because we do not know when the structure will change
        public void TriggerSolve()
        {
            solveTriggered = true;
        }

        public void SetSpeciesBodySize(int idx, double sizeNormalised)
        {
            Species s = idxToSpecies[idx];
            s.BodySize = UnNormaliseOnLogScale(sizeNormalised, kg_min, kg_max);

            double sizeScaling = Math.Pow(s.BodySize, beta-1);
            s.Metabolism = s.IsProducer? sizeScaling * r0 : -sizeScaling * z0;
        }
        public void SetSpeciesInterference(int idx, double greedNormalised)
        {
            idxToSpecies[idx].Interference = -UnNormaliseOnLogScale(greedNormalised, a_min, a_max);
        }
        static double UnNormaliseOnLogScale(double normalised, double minVal, double maxVal)
        {
            // double min = Math.Log10(minVal);
            // double max = Math.Log10(maxVal);
            // double mid = min + normalised*(max-min);
            // return Math.Pow(10, mid);

            // same as above commented
            return Math.Pow(minVal, 1-normalised) * Math.Pow(maxVal, normalised);
        }




        /////////////////////////////////////
        // actual calculations here

        public bool Feasible { get; private set; } = false;
        public bool Stable { get; private set; } = false;

        void Update()
        {
            if (solveTriggered && !isCalculatingAsync)
            {
                solveTriggered = false;
                Equilibrate();
            }
        }


        private bool solveTriggered = false;
        private bool isCalculatingAsync = false;
        public bool EquilibriumSolved { get { return !solveTriggered && !isCalculatingAsync; } }

// because webgl does not support threads
#if !UNITY_WEBGL
        async void Equilibrate() {
#else
        void Equilibrate() {
#endif
            isCalculatingAsync = true;

#if !UNITY_WEBGL
            Feasible = await Task.Run(()=> simulation.SolveFeasibility(GetConsumers));
            Stable = await Task.Run(()=> simulation.SolveStability());
#else
            Feasible = simulation.SolveFeasibility(GetConsumers);
            Stable = simulation.SolveStability();
#endif

            isCalculatingAsync = false;
            OnEquilibrium.Invoke();
        }

        public double GetNormalisedAbundance(int idx)
        {
            Species s = idxToSpecies[idx];
            double realAbund = simulation.GetSolvedAbundance(s);

            // null used to always throw event on first time
            if (realAbund <= 0 && (!s.Endangered ?? true)) {
                OnEndangered.Invoke(idx);
            }
            if (realAbund > 0 && (s.Endangered ?? true)) {
                OnRescued.Invoke(idx);
            }
            s.Endangered = realAbund <= 0;


            if (realAbund > 0)
            {
                if (realAbund < minRealAbund) {
                    return 0;
                } else if (realAbund <= maxRealAbund) {
                    return (Math.Log10(realAbund)-minLogAbund) / (maxLogAbund-minLogAbund);
                } else {
                    return 1;
                }
            }
            else // return a negative abundance but scaled as if it were positive
            {
                if (-realAbund < minRealAbund) {
                    return 0;
                } else if (-realAbund <= maxRealAbund) {
                    return -(Math.Log10(-realAbund)-minLogAbund) / (maxLogAbund-minLogAbund);
                } else {
                    return -1;
                }
            }
        }
        public double GetNormalisedFlux(int res, int con)
        {
            double realFlux = simulation.GetSolvedFlux(idxToSpecies[res], idxToSpecies[con]);
            if (realFlux <= minRealFlux) {
                return 0;
            } else if (realFlux >= maxRealFlux) {
                return 1;
            } else {
                return (Math.Log10(realFlux)-minLogFlux) / (maxLogFlux-minLogFlux);
            }
        }
        public double GetNormalisedComplexity()
        {
            double realPlex = simulation.HoComplexity;
            if (realPlex < minRealPlex) {
                return 0;
            } else if (realPlex <= maxRealPlex) {
                return (Math.Log10(realPlex)-minLogPlex) / (maxLogPlex-minLogPlex);
            } else {
                return (realPlex / maxRealPlex);
            }
        }
        public string GetComplexityDescription()
        {
            // TODO: scaling
            return $"#species={simulation.Richness}\n#interactions={simulation.Connectance}\nhealth={simulation.TotalAbundance}";
        }

        public string GetMatrix()
        {
            return simulation.GetState();
        }
    }

    // #if UNITY_EDITOR
    // public class ReadOnlyAttribute : PropertyAttribute {}
    // [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    // public class ShowOnlyDrawer : PropertyDrawer
    // {
    //     public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
    //     {
    //         string valueStr;
    
    //         switch (prop.propertyType)
    //         {
    //             case SerializedPropertyType.Integer:
    //                 valueStr = prop.intValue.ToString();
    //                 break;
    //             case SerializedPropertyType.Boolean:
    //                 valueStr = prop.boolValue.ToString();
    //                 break;
    //             case SerializedPropertyType.Float:
    //                 valueStr = prop.floatValue.ToString("e2");
    //                 break;
    //             case SerializedPropertyType.String:
    //                 valueStr = prop.stringValue;
    //                 break;
    //             default:
    //                 valueStr = "(not supported)";
    //                 break;
    //         }
    
    //         EditorGUI.LabelField(position,label.text, valueStr);
    //     }
    // }
    // #else
    // // empty attribute
    // public class ReadOnlyAttribute : PropertyAttribute {}
    // #endif
}