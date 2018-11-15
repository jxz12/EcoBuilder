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

    public bool nextIsProducer { get; set; }
    public float nextBodyMass { get; set; }
    public float nextGreediness { get; set; } = 1f;

    class Species
    {
        public int Idx { get; private set; }
        public bool IsProducer { get; private set; }
        public double BodyMass { get; private set; }
        public double Intra { get; private set; }
        public string Name { get; private set; }

        public Species(int idx, bool isProducer, float bodyMassScale, float greedyScale, string name)
        {
            Idx = idx;
            IsProducer = isProducer;

            // this corresponds to a range from 1 mg all the way up to 1 tonne
            // TODO: do the POW thing here so that model does not do endless exponents
            BodyMass = 1e-6*Mathf.Pow(10, bodyMassScale*9);;
            // intraspecific interaction scales from 0 to -1
            Intra = greedyScale - 1;
            Name = name;
        }

        static readonly double b0 = 1e0,
                            d0 = 1e-2,
                            a0 = 1e0,
                            a_ii0 = 1e-1,
                            beta = 0.75;
    }

    Dictionary<int, Species> speciesDict = new Dictionary<int, Species>();
    LotkaVolterraLAS<Species> model = new LotkaVolterraLAS<Species>(
        i => i.IsProducer? 1:-1,
        i => i.Intra,
        (i,j) => 1,
        (i,j) => i.IsProducer? 0.2:0.5
    );

    public void AddSpecies(int idx, string name)
    {
        var newSpecies = new Species(idx, nextIsProducer, nextBodyMass, nextGreediness, name);
        model.AddSpecies(newSpecies);
        speciesDict.Add(idx, newSpecies);

        if (nextIsProducer)
        {
            nichess.AddPiece(idx, Nichess.Shape.Square, 1-nextBodyMass);
            nichess.FixPiecePos(idx);
            nodeLink.AddNode(idx, NodeLink.Shape.Cube);
        }
        else
        {
            nichess.AddPiece(idx, Nichess.Shape.Circle, 1-nextBodyMass);
            nodeLink.AddNode(idx, NodeLink.Shape.Sphere);
        }

    }
    public void RemoveSpecies(int idx)
    {
        nichess.RemovePiece(idx);
        nodeLink.RemoveNode(idx);
        model.RemoveSpecies(speciesDict[idx]);

        speciesDict.Remove(idx);
    }

    public void AddInteraction(int resource, int consumer)
    {
        if (resource == consumer)
            throw new Exception("can't eat itself");

        Species res = speciesDict[resource], con = speciesDict[consumer];
        model.AddInteraction(res, con);
    }
    public void RemoveInteraction(int resource, int consumer)
    {
        if (resource == consumer)
            throw new Exception("can't eat itself");

        Species res = speciesDict[resource], con = speciesDict[consumer];
        model.RemoveInteraction(res, con);
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
        await Task.Run(() => model.SolveEquilibrium());
        monitor.SetHeight(model.MaxAbundance + " " + model.MinAbundance);
        // calculate stability if feasible
        if (model.MinAbundance > 0)
        {
            double stability = await Task.Run(() => model.LocalAsymptoticStability());
            monitor.SetLAS(stability.ToString());
        }
        else
        {
            monitor.SetLAS("infeasible");
        }
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