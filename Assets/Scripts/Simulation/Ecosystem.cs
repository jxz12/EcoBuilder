using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

// Controls everything!
public class Ecosystem : MonoBehaviour
{   
    [SerializeField] Nichess nichess;
    [SerializeField] NodeLink nodeLink;
    [SerializeField] Monitor monitor;

    [SerializeField] float heartRate = 30;

    bool newIsProducer;
    float newBodyMass;
    public void SetNewIsProducer(bool isProducer) { newIsProducer = isProducer; }
    public void SetNewBodyMass(float bodyMass) { newBodyMass = bodyMass; }

    class Species
    {
        public int Idx { get; private set; }
        public bool IsProducer { get; private set; }
        public double BodyMass { get; private set; }
        public string Name { get; private set; }

        public Species(int idx, bool isProducer, float bodyMassScale, string name)
        {
            Idx = idx;
            IsProducer = isProducer;

            // this corresponds to a range from 1 mg all the way up to 1 tonne
            BodyMass = 1e-6*Mathf.Pow(10, bodyMassScale*9);;
            Name = name;
        }
    }

    static readonly double b0 = 1e0,
                           d0 = -1e-2,
                           a0 = 1e0,
                     aii_easy = -1e-2,
                   aii_medium = -1e-3,
                     aii_hard = -1e-4,
                         beta = 0.75;

    private static double r_i(Species i)
    {
        double mPow = Math.Pow(i.BodyMass, beta-1);
        return i.IsProducer? b0*mPow : d0*mPow;
    }
    private static double a_ii(Species i)
    {
        if (GameManager.Instance.difficulty == GameManager.Difficulty.Easy)
            return aii_easy;
        else if (GameManager.Instance.difficulty == GameManager.Difficulty.Medium)
            return aii_medium;
        else if (GameManager.Instance.difficulty == GameManager.Difficulty.Hard)
            return aii_hard;
        else
            throw new Exception("difficulty not supported");
    }
    private static double a_ij(Species i, Species j)
    {
        double mPow = Math.Pow(j.BodyMass, beta-1);
        return a0 * mPow;
    }
    private static double e_ij(Species i, Species j)
    {
        return i.IsProducer? .2 : .5;
    }


    Dictionary<int, Species> speciesDict = new Dictionary<int, Species>();
    LotkaVolterraLAS model = new LotkaVolterraLAS();

    public void AddSpecies(int idx, string name)
    {
        var newSpecies = new Species(idx, newIsProducer, newBodyMass, name);
        speciesDict.Add(idx, newSpecies);

        if (newIsProducer)
        {
            nichess.AddPiece(idx, Nichess.Shape.Square, 1-newBodyMass);
            nichess.FixPiecePos(idx);
            nodeLink.AddNode(idx, NodeLink.Shape.Cube);
        }
        else
        {
            nichess.AddPiece(idx, Nichess.Shape.Circle, 1-newBodyMass);
            nodeLink.AddNode(idx, NodeLink.Shape.Sphere);
        }

        model.AddSpecies(idx);
        model.GrowthVector[idx] = r_i(newSpecies);
        print(model.GrowthVector[idx]);
        model.InteractionMatrix[idx, idx] = a_ii(newSpecies);
    }
    public void RemoveSpecies(int idx)
    {
        nichess.RemovePiece(idx);
        nodeLink.RemoveNode(idx);

        speciesDict.Remove(idx);
        model.RemoveSpecies(idx);
    }

    public void AddInteraction(int resource, int consumer)
    {
        if (resource == consumer)
            throw new Exception("can't eat itself");

        Species res = speciesDict[resource], con = speciesDict[consumer];
        double interaction = a_ij(res, con);
        double efficiency = e_ij(res, con);
        model.InteractionMatrix[resource, consumer] += -interaction;
        model.InteractionMatrix[consumer, resource] += interaction * efficiency;
    }
    public void RemoveInteraction(int resource, int consumer)
    {
        if (resource == consumer)
            throw new Exception("can't eat itself");

        Species res = speciesDict[resource], con = speciesDict[consumer];
        double interaction = a_ij(res, con);
        double efficiency = e_ij(res, con);
        model.InteractionMatrix[resource, consumer] -= -interaction;
        model.InteractionMatrix[consumer, resource] -= interaction * efficiency;
    }

    public void ConfigFromString(string config)
    {
        throw new NotImplementedException();
    }
    public string GetConfigString()
    {
        throw new NotImplementedException();
    }


    /// <summary>
    /// Mathy stuff goes below
    /// </summary>

    static Func<double, string> Format =
        x => x>=0? Math.Log10(1+x).ToString("0.000") : (-Math.Log10(1-x)).ToString("0.000");

    async void Equilibrium()
    {
        await Task.Run(() => model.Equilibrium());
        monitor.SetFlux(model.InteractionMatrix.ToString(x => x.ToString()));
        monitor.SetHeight(model.EquilibriumAbundances.ToString(Format));

        // check whether feasible
        foreach (double abundance in model.EquilibriumAbundances)
        {
            if (abundance <= 0)
            {
                monitor.SetLAS("infeasible");
                return;
            }
        }
        LocalStability(); // calculate stability if feasible
    }
    async void LocalStability()
    {
        double stability = await Task.Run(() => model.LocalAsymptoticStability());
        monitor.SetLAS(Format(stability));
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


    ////////////////////////////////////////////////////////////////////////
    // REPLACE THE NICE 'PULSING' WITH EQUILIBRIUM ABUNDANCE CALCULATION! //
    ////////////////////////////////////////////////////////////////////////
}