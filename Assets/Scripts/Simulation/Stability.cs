using System;
using System.Collections.Generic;
using SparseMatrix;
using MathNet.Numerics.LinearAlgebra;
//using MathNet.Numerics.LinearAlgebra;

// simply a maths class to calculate things like trophic levels and eigenvalues
public class Stability
{
    public SparseVector<double> GrowthVector { get; private set; }
    public SparseMatrix<double> InteractionMatrix { get; private set; }

    public SparseVector<double> TrophicLevels { get; private set; }
    public SparseVector<double> EquilibriumAbundances { get; private set; }

    public Stability()
    {
        GrowthVector = new SparseVector<double>();
        InteractionMatrix = new SparseMatrix<double>();

        TrophicLevels = new SparseVector<double>();
        EquilibriumAbundances = new SparseVector<double>();
    }

    Queue<int> missingIndices = new Queue<int>();
    HashSet<int> speciesIndices = new HashSet<int>();
    public int AddSpecies()
    {
        int idx;
        if (missingIndices.Count == 0)
            idx = speciesIndices.Count;
        else
            idx = missingIndices.Dequeue();

        speciesIndices.Add(idx);
        return idx;
    }
    public void RemoveSpecies(int idx)
    {
        if (!speciesIndices.Contains(idx))
            throw new Exception("ecosystem does not have that idx");

        speciesIndices.Remove(idx);
        missingIndices.Enqueue(idx);
    }



    SparseMatrix<double> trophicA = new SparseMatrix<double>();
    
    public void TrophicGaussSeidel()
    {
        int n = speciesIndices.Count;
        if (n == 0)
            return;

        // update the system of linear equations
        foreach (int consumer in speciesIndices)
        {
            double aTotal = 0;
            foreach (int resource in speciesIndices)
            {
                if (consumer != resource) // no cannibalism
                {
                    double a = InteractionMatrix[consumer, resource];
                    if (a > 0) // if j gains biomass from i
                    {
                        trophicA[consumer, resource] = -a;
                        aTotal += a;
                    }
                    else
                    {
                        trophicA[consumer, resource] = 0;
                    }
                }
            }
            if (aTotal != 0) // in case divide by zero
            {
                foreach (int resource in speciesIndices)
                {
                    if (consumer != resource)
                        trophicA[consumer, resource] /= aTotal; // makes sure that the row sums to 1
                }
            }
        }

        // perform Gauss-Seidel iteration (positive definite)
        foreach (int i in speciesIndices)
        {
            double temp = 0;
            foreach (int j in speciesIndices)
            {
                if (i != j)
                    temp += trophicA[i, j] * TrophicLevels[j];
            }
            //TrophicLevels[i] = (trophicb[i] - temp) / trophicA[i, i];
            TrophicLevels[i] = (1 - temp); // trophicb and trophicA[i,i] not necessary as they equal 1
        }
    }

    // these two should be run async because they are O(n^3)
    public void Equilibrium()
    {
        // create Matrix and Vector that MathNet understands
        int n = speciesIndices.Count;
        var idxMap = new List<int>(n);
        foreach (int idx in speciesIndices)
            idxMap.Add(idx);

        Matrix<double> A = Matrix<double>.Build.Dense(n, n, (i, j) => InteractionMatrix[idxMap[i], idxMap[j]]);
        Vector<double> b = Vector<double>.Build.Dense(n, i => -GrowthVector[idxMap[i]]);

        //UnityEngine.Debug.Log(MatStr(A.ToArray()));
        //UnityEngine.Debug.Log(VecStr(b.ToArray()));


        // find stable equilibrium point of system
        var x = A.Solve(b);

        // place values back into SparseVector
        for (int i=0; i<n; i++)
            EquilibriumAbundances[idxMap[i]] = x[i];
    }

    public double LocalAsymptoticStability()
    {
        // calculate community matrix with jacobian

        Matrix<double> A = BuildCommunityMatrix();

        var eigenValues = A.Evd().EigenValues;

        // get largest real part of any eigenvalue
        double Lambda = double.MinValue;
        foreach (var e in eigenValues)
            Lambda = Math.Max(Lambda, e.Real);

        //System.Threading.Thread.Sleep(1000);
        return -Lambda;
    }



    Matrix<double> BuildCommunityMatrix()
    {
        //int n = growthVector.Length;
        int n = speciesIndices.Count;
        var idxMap = new List<int>(n);
        foreach (int idx in speciesIndices)
            idxMap.Add(idx);

        var mat = Matrix<double>.Build.Dense(n, n);
        for (int i=0; i<n; i++)
        {
            int res = idxMap[i];
            mat[i, i] = GrowthVector[res] + (2 * InteractionMatrix[res, res] * EquilibriumAbundances[res]);

            // calculate every element of the Jacobian, evaluated at equilibriumDensity
            for (int j = 0; j < n; j++)
            {
                if (i != j)
                {
                    int con = idxMap[j];
                    mat[i, j] = InteractionMatrix[res, con] * EquilibriumAbundances[res];
                    mat[i, i] += InteractionMatrix[res, con] * EquilibriumAbundances[con];
                }
            }
        }
        return mat;
    }
        

    public static string MatStr<T>(T[,] mat)
    {
        var sb = new System.Text.StringBuilder();
        int m = mat.GetLength(0), n = mat.GetLength(1);
        for (int i=0; i<m; i++)
        {
            for (int j=0; j<n; j++)
            {
                sb.Append(mat[i, j].ToString() + " ");
            }
            sb.Append("\n");
        }
        sb.Remove(sb.Length - 1, 1);
        return sb.ToString();
    }
    
    public static string VecStr<T>(T[] vec)
    {
        var sb = new System.Text.StringBuilder();
        int n = vec.Length;
        for (int i=0; i<n; i++)
        {
                sb.Append(vec[i].ToString() + " ");
        }
        sb.Remove(sb.Length - 1, 1);
        return sb.ToString();
    }
}
