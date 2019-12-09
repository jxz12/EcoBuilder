using UnityEngine;
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
        double a_min, a_max;  // calculated at runtime

        [ReadOnly] [SerializeField]
        float minRealAbund = 2e-10f,
              maxRealAbund = 1.95f,
              minRealFlux = 1e-16f,
              maxRealFlux = 1e-6f;

        private float minLogAbund, maxLogAbund,
                      minLogFlux, maxLogFlux;
        void InitScales()
        {
            minLogAbund = Mathf.Log10(minRealAbund);
            maxLogAbund = Mathf.Log10(maxRealAbund);
            minLogFlux = Mathf.Log10(minRealFlux);
            maxLogFlux = Mathf.Log10(maxRealFlux);
        }

        ///////////////////////////////////////////////////////////////////
        // search rate derivations
        class Species
        {
            public int Idx { get; private set; }
            public double BodySize { get; set; }
            public bool IsProducer { get; set; }

            public double Metabolism { get; set; }
            public double Interference { get; set; }
            public double Efficiency { get; set; }

            public float NormalisedAbundance { get; set; } = -1; // init positive

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
            // calculate bounds for a_ij and set a_ii in the same range
            a_max = ActiveCapture(kg_max, kg_min) / kg_min;
            a_min = Grazing(kg_min, kg_max) / kg_max;

            simulation = new LotkaVolterra<Species>(
                  (s)=> s.Metabolism,
                  (s)=> s.Interference,
                (r,c)=> CalculateForaging(r,c), // takes foraging strategy into account
                (r,c)=> r.Efficiency            // only depends on resource type
            );

            InitScales();
        }
        Dictionary<int, Species> idxToSpecies = new Dictionary<int, Species>();
        Dictionary<Species, int> speciesToIdx = new Dictionary<Species, int>();

        public void AddSpecies(int idx)
        {
            if (idxToSpecies.ContainsKey(idx))
                throw new Exception("already contains idx " + idx);

            var newSpecies = new Species(idx);
            simulation.AddSpecies(newSpecies);

            idxToSpecies.Add(idx, newSpecies);
            speciesToIdx.Add(newSpecies, idx);
        }
        public void RemoveSpecies(int idx)
        {
            if (!idxToSpecies.ContainsKey(idx))
                throw new Exception("does not contain idx " + idx);

            Species toRemove = idxToSpecies[idx];
            simulation.RemoveSpecies(toRemove);

            idxToSpecies.Remove(idx);
            speciesToIdx.Remove(toRemove);
            // AtEquilibrium = false;
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

        public float NormalisedScore { get; private set; }
        public bool IsCalculating { get; private set; } = false;

                                           // required because this class does not store adjacency
        public async void EquilibriumAsync(Func<int, IEnumerable<int>> Consumers)
        {
            IsCalculating = true;
            Func<Species, IEnumerable<Species>> map = s=>Consumers(speciesToIdx[s]).Select(i=>idxToSpecies[i]);

            Feasible = await Task.Run(() => simulation.SolveFeasibility(map));
            Stable = await Task.Run(()=> simulation.SolveStability());

            TriggerAbundanceEvents();
            CalculateScore();
            IsCalculating = false;
            OnEquilibrium.Invoke();
        }
        public void EquilibriumSync(Func<int, IEnumerable<int>> Consumers)
        {
            Func<Species, IEnumerable<Species>> map = s=>Consumers(speciesToIdx[s]).Select(i=>idxToSpecies[i]);

            Feasible = simulation.SolveFeasibility(map);
            Stable = simulation.SolveStability();

            TriggerAbundanceEvents();
            CalculateScore();
            OnEquilibrium.Invoke();
        }

        private void CalculateScore()
        {
            // print(simulation.TotalFlux + " " + simulation.TotalAbundance);
            NormalisedScore = (float)(simulation.Richness *
                                      simulation.Connectance *
                                      totalAbund_Norm) * 100;
        }
        public string ScoreExplanation()
        {
            // return "Number of Species " + simulation.Richness + " × Proportion of Links " + simulation.Connectance + " × Total Health " + totalAbund_Norm + " = " + (simulation.Richness*simulation.Connectance*totalAbund_Norm);
            return "Number of Species " + simulation.Richness + " × Proportion of Links " + simulation.Connectance + " × Total Health " + totalAbund_Norm + " = " + (NormalisedScore);
        }

        private float total_NormAbund;
        private float totalAbund_Norm;
        void TriggerAbundanceEvents()
        {
            total_NormAbund = totalAbund_Norm = 0;
            // show abundance warnings
            foreach (int i in idxToSpecies.Keys)
            {

                Species s = idxToSpecies[i];
                double realAbund = simulation.GetSolvedAbundance(s);

                float newAbund;
                if (realAbund <= 0)
                    newAbund = -1;
                else if (realAbund <= minRealAbund)
                    newAbund = 0;
                else if (realAbund >= maxRealAbund)
                    newAbund = 1;
                else
                    newAbund = (float)(Math.Log10(realAbund)-minLogAbund) / (maxLogAbund-minLogAbund);

                if (s.NormalisedAbundance >= 0 && newAbund < 0)
                    OnEndangered.Invoke(i);
                if (s.NormalisedAbundance < 0 && newAbund >= 0)
                    OnRescued.Invoke(i);

                s.NormalisedAbundance = newAbund;

                totalAbund_Norm += (float)realAbund;
                if (newAbund >= 0)
                    total_NormAbund += newAbund;
            }

            if (total_NormAbund == 0)
                total_NormAbund = 1; // to prevent NaN problems in GetNormalisedAbundance

            if (totalAbund_Norm <= minRealAbund)
                totalAbund_Norm = 0;
            else
                totalAbund_Norm = (Mathf.Log10(totalAbund_Norm)-minLogAbund) / (maxLogAbund-minLogAbund);
            // print(totalAbund_Norm+" "+total_NormAbund);
        }
        public float GetNormalisedAbundance(int idx)
        {
                   // calculate proportion first                            multiply by total
            return (idxToSpecies[idx].NormalisedAbundance / total_NormAbund) * totalAbund_Norm * idxToSpecies.Count;
            // this is so that total green in health bars adds up to the score
        }
        public float GetNormalisedFlux(int res, int con)
        {
            double flux = simulation.GetSolvedFlux(idxToSpecies[res], idxToSpecies[con]);
            if (flux <= minRealFlux)
                return 0;
            else if (flux >= maxRealFlux)
                return 1;
            else
                return (float)(Math.Log10(flux)-minLogFlux) / (maxLogFlux-minLogFlux);
        }

        // TODO: please work out a way of recording this
        // public void RecordInteractionMatrix(int )
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