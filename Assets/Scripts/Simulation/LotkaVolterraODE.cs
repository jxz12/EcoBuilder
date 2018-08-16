using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.OdeSolvers;

public class LotkaVolterraODE
{
    protected Func<int, double> Growth;
    protected Func<int, double> Intraspecific;
    protected Func<int, int, double> AttackRate;
    protected Func<int, int, double> Efficiency;
    protected Func<int, int, bool> Adjacency;

    public LotkaVolterraODE(
        Func<int, double> Growth,
        Func<int, double> Intraspecific,
        Func<int, int, double> AttackRate,
        Func<int, int, double> ConversionEfficiency,
        Func<int, int, bool> Adjacency)
    {
        this.Growth = Growth;
        this.Intraspecific = Intraspecific;
        this.AttackRate = AttackRate;
        this.Efficiency = ConversionEfficiency;
        this.Adjacency = Adjacency;
    }

    List<double> abundances = new List<double>();
    //protected override void OnSpawn(int species)
    //{
    //    abundances[species] = 0;
    //}
    //protected override void OnExtinction(int species)
    //{
    //    abundances.Remove(species);
    //}

    //public void SetAbundance(int toSet, double amount)
    //{
    //    if (!abundances.ContainsKey(toSet))
    //        throw new Exception("Species not added");
    //    abundances[toSet] = amount;
    //}
    //public double GetAbundance(int toGet)
    //{
    //    if (!abundances.ContainsKey(toGet))
    //        throw new Exception("Species not added");
    //    return abundances[toGet];
    //}


    public void Integrate(double timeStep, int steps = 2)
    {
        int n = abundances.Count;
        if (n == 0)
            return;

        var x = CreateVector.Dense(n, i => abundances[i]);

        var result = RungeKutta.FourthOrder(x, 0, timeStep, steps, Dxdt);
        x = result[steps - 1];
    }
    private Vector<double> Dxdt(double t, Vector<double> x) // t is not used because LV is time-invariant
    {
        int n = abundances.Count;
        var dxdt = CreateVector.Dense<double>(n, 0);
        for (int i = 0; i < n; i++)
        {
            dxdt[i] += Growth(i);
            dxdt[i] += Intraspecific(i) * x[i];
            for (int j = 0; j < n; j++)
            {
                if (Adjacency(i, j) == true)
                {
                    double a_ij = AttackRate(i, j);
                    double e = Efficiency(i, j);

                    dxdt[i] -= a_ij * x[j];
                    dxdt[j] += a_ij * x[i] * e;
                }
            }
        }
        for (int i = 0; i < n; i++)
            dxdt[i] *= x[i];
        return dxdt;
    }

}