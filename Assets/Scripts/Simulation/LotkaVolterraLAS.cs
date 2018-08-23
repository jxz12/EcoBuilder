using System;
using System.Collections.Generic;
using SparseMatrix;
using MathNet.Numerics.LinearAlgebra;

// simply a maths class to calculate things like trophic levels and eigenvalues
public class LotkaVolterraLAS
{
    public SparseVector<double> GrowthVector { get; private set; }
    public SparseMatrix<double> InteractionMatrix { get; private set; }

    public SparseVector<double> EquilibriumAbundances { get; private set; }

    public LotkaVolterraLAS()
    {
        GrowthVector = new SparseVector<double>();
        InteractionMatrix = new SparseMatrix<double>();
        EquilibriumAbundances = new SparseVector<double>();
    }

    HashSet<int> speciesIndices = new HashSet<int>();
    public void AddSpecies(int idx)
    {
        if (speciesIndices.Contains(idx))
            throw new Exception("ecosystem has idx already");

        speciesIndices.Add(idx);
    }
    public void RemoveSpecies(int idx)
    {
        if (!speciesIndices.Contains(idx))
            throw new Exception("ecosystem does not have that idx");

        speciesIndices.Remove(idx);

        // clean matrices and vectors
        GrowthVector.RemoveAt(idx);
        EquilibriumAbundances.RemoveAt(idx);
        foreach (int other in speciesIndices)
        {
            InteractionMatrix.RemoveAt(idx, other);
            InteractionMatrix.RemoveAt(other, idx);
        }

    }


    // these should be run async because O(n^3)
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

            // calculate every element of the Jacobian, evaluated at equilibrium point
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

    // these should be run async because O(n^3)
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
