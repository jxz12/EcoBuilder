using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

// for heavy calculations
using System.Threading.Tasks;

namespace EcoBuilder.Model
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

        [ReadOnly] [SerializeField]
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

        [ReadOnly] [SerializeField]
        double a_min = 1e-5,
               a_max = 1;

        [ReadOnly] [SerializeField]
        float minRealAbund = 2e-10f,
              maxRealAbund = 1.95f,
              minRealFlux = 1e-16f,
              maxRealFlux = 1e-6f,
              minRealPlex = 2.1e-5f, // TODO: calculate actual values
              maxRealPlex = 6.3e-3f;

        private float minLogAbund, maxLogAbund,
                      minLogFlux, maxLogFlux,
                      minLogPlex, maxLogPlex;

        ///////////////////////////////////////////////////////////////////
        // search rate derivations
        class Species
        {
            public int Idx { get; private set; } = -1;
            public bool IsProducer { get; set; } = false;
            public double BodySize { get; set; } = double.NaN;
            public double Interference { get; set; } = double.NaN;

            public double Metabolism { get; set; } = double.NaN;
            public double Efficiency { get; set; } = double.NaN;

            public bool Endangered { get; set; } = true; // initialise extinct so heart appears
            public float NormalisedAbundance { get; set; } = 0;

            public Species(int idx)
            {
                Idx = idx;
            }
        }
        
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
            minLogAbund = Mathf.Log10(minRealAbund);
            maxLogAbund = Mathf.Log10(maxRealAbund);
            minLogFlux = Mathf.Log10(minRealFlux);
            maxLogFlux = Mathf.Log10(maxRealFlux);
            minLogPlex = Mathf.Log10(minRealPlex);
            maxLogPlex = Mathf.Log10(maxRealPlex);
        }
        Dictionary<int, Species> idxToSpecies = new Dictionary<int, Species>();
        Dictionary<Species, int> speciesToIdx = new Dictionary<Species, int>();

        public void AddSpecies(int idx)
        {
            Assert.IsFalse(idxToSpecies.ContainsKey(idx), "already contains idx " + idx);

            var newSpecies = new Species(idx);
            simulation.AddSpecies(newSpecies);

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

        public void SetSpeciesIsProducer(int idx, bool isProducer)
        {
            Species s = idxToSpecies[idx];
            s.IsProducer = isProducer;

            double sizeScaling = Math.Pow(s.BodySize, beta-1);
            s.Metabolism = s.IsProducer? sizeScaling * r0 : -sizeScaling * z0;
            s.Efficiency = s.IsProducer? e_p : e_c;
        }
        public void SetSpeciesBodySize(int idx, float sizeNormalised)
        {
            Species s = idxToSpecies[idx];
            s.BodySize = UnNormaliseOnLogScale(sizeNormalised, kg_min, kg_max);

            double sizeScaling = Math.Pow(s.BodySize, beta-1);
            s.Metabolism = s.IsProducer? sizeScaling * r0 : -sizeScaling * z0;
        }
        public void SetSpeciesInterference(int idx, float greedNormalised)
        {
            idxToSpecies[idx].Interference = -UnNormaliseOnLogScale(greedNormalised, a_min, a_max);
        }
        static double UnNormaliseOnLogScale(double normalised, double minVal, double maxVal)
        {
            // float min = Mathf.Log10(minVal);
            // float max = Mathf.Log10(maxVal);
            // float mid = min + normalised*(max-min);
            // return Mathf.Pow(10, mid);

            // same as above commented
            return Math.Pow(minVal, 1-normalised) * Math.Pow(maxVal, normalised);
        }




        /////////////////////////////////////
        // actual calculations here

        public bool Feasible { get; private set; } = false;
        public bool Stable { get; private set; } = false;
        public bool IsCalculatingAsync { get; private set; } = false;

                                           // required because this class does not store adjacency
        public async void EquilibriumAsync(Func<int, IEnumerable<int>> Consumers)
        {
            IsCalculatingAsync = true;
            Func<Species, IEnumerable<Species>> map = s=>Consumers(speciesToIdx[s]).Select(i=>idxToSpecies[i]);

            Feasible = await Task.Run(()=> simulation.SolveFeasibility(map));
            Stable = await Task.Run(()=> simulation.SolveStability());

            IsCalculatingAsync = false;
            OnEquilibrium.Invoke();
        }
        public void EquilibriumSync(Func<int, IEnumerable<int>> Consumers)
        {
            Func<Species, IEnumerable<Species>> map = s=>Consumers(speciesToIdx[s]).Select(i=>idxToSpecies[i]);

            Feasible = simulation.SolveFeasibility(map);
            Stable = simulation.SolveStability();

            OnEquilibrium.Invoke();
        }
        public float GetNormalisedAbundance(int idx)
        {
            Species s = idxToSpecies[idx];
            double realAbund = simulation.GetSolvedAbundance(s);

            if (!s.Endangered && realAbund <= 0) {
                OnEndangered.Invoke(idx);
            }
            if (s.Endangered && realAbund > 0) {
                OnRescued.Invoke(idx);
            }
            s.Endangered = realAbund <= 0;

            if (realAbund <= minRealAbund) {
                return 0;
            } else if (realAbund >= maxRealAbund) {
                return 1;
            } else {
                return (float)(Math.Log10(realAbund)-minLogAbund) / (maxLogAbund-minLogAbund);
            }
        }
        public float GetNormalisedFlux(int res, int con)
        {
            double realFlux = simulation.GetSolvedFlux(idxToSpecies[res], idxToSpecies[con]);
            if (realFlux <= minRealFlux) {
                return 0;
            } else if (realFlux >= maxRealFlux) {
                return 1;
            } else {
                return (float)(Math.Log10(realFlux)-minLogFlux) / (maxLogFlux-minLogFlux);
            }
        }
        public float GetNormalisedComplexity()
        {
            double realPlex = simulation.HoComplexity;
            // print("ho "+realPlex.ToString("e2"));
            if (realPlex < minRealPlex) {
                return 0;
            } else if (realPlex <= maxRealPlex) {
                return (float)(Math.Log10(realPlex)-minLogPlex) / (maxLogPlex-minLogPlex);
            } else {
                return (float)(realPlex / maxRealPlex);
            }
        }
        public string GetComplexityExplanation()
        {
            // return "Number of Species " + simulation.Richness + " × Proportion of Links " + simulation.Connectance + " × Total Health " + totalAbund_Norm + " = " + (simulation.Richness*simulation.Connectance*totalAbund_Norm);
            // return "Number of Species " + simulation.Richness + " × Proportion of Links " + simulation.Connectance + " × Total Health " + totalAbund_Norm + " = " + (NormalisedScore);
            return "TODO: explain score better";
        }

        public string GetMatrix()
        {
            return simulation.GetState();
        }
    }

    #if UNITY_EDITOR
    public class ReadOnlyAttribute : PropertyAttribute {}
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ShowOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
        {
            string valueStr;
    
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    valueStr = prop.intValue.ToString();
                    break;
                case SerializedPropertyType.Boolean:
                    valueStr = prop.boolValue.ToString();
                    break;
                case SerializedPropertyType.Float:
                    valueStr = prop.floatValue.ToString("e2");
                    break;
                case SerializedPropertyType.String:
                    valueStr = prop.stringValue;
                    break;
                default:
                    valueStr = "(not supported)";
                    break;
            }
    
            EditorGUI.LabelField(position,label.text, valueStr);
        }
    }
    #else
    // empty attribute
    public class ReadOnlyAttribute : PropertyAttribute {}
    #endif
}