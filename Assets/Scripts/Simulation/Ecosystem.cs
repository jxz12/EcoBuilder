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
    [SerializeField] Thermometer therm;

    [SerializeField] float heartRate = 30;

    bool newIsProducer;
    float newBodyMass;
    public void SetNewIsProducer(bool isProducer) { newIsProducer = isProducer; }
    public void SetNewBodyMass(float bodyMass) { newBodyMass = bodyMass; }

    class Species
    {
        public Species(int idx, bool isProducer, float bodyMass, string name)
        {
            Idx = idx;
            IsProducer = isProducer;
            BodyMass = bodyMass;
            Name = name;
        }

        public int Idx { get; private set; }
        public bool IsProducer { get; private set; }
        public float BodyMass { get; private set; }
        public string Name { get; private set; }
    }

    private readonly Func<float, double> m0 = m => Mathf.Pow(10, m);

    private readonly Func<Species, double> r_i = i => i.IsProducer? .1 : -.4;
    private readonly Func<Species, double> a_ii = i => .01;
    private readonly Func<Species, Species, double> a_ij = (i, j) => .02;
    private readonly Func<Species, Species, double> e_ij = (i, j) => .5;

    Dictionary<int, Species> speciesDict = new Dictionary<int, Species>();
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
        model.InteractionMatrix[idx, idx] = a_ii(newSpecies);
    }
    public void RemoveSpecies(int idx)
    {
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

    private LotkaVolterraLAS model = new LotkaVolterraLAS();

    async void Equilibrium()
    {
        await Task.Run(() => model.Equilibrium());
        print("equilibrium:\n" + model.EquilibriumAbundances.ToString(x => x.ToString()));
    }
    async void LocalStability()
    {
        double stability = await Task.Run(() => model.LocalAsymptoticStability());
        print("stability: " + stability);
    }

    private void Start()
    {
        StartCoroutine(Pulse(60f / heartRate));
    }

    IEnumerator Pulse(float delay)
    {
        while (true)
        {
            // Equilibrium();
            print(model.InteractionMatrix.ToString(x=>x.ToString()));
            yield return new WaitForSeconds(delay);
        }
    }


    ////////////////////////////////////////////////////////////////////////
    // REPLACE THE NICE 'PULSING' WITH EQUILIBRIUM ABUNDANCE CALCULATION! //
    ////////////////////////////////////////////////////////////////////////
}