using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;

namespace EcoBuilder.Model
{
    // a class to implement a simple lotka-volterra Type I functional response model
    // can find equilibrium abundances, flux at equilibrium, local asymptotic stability
    public class LotkaVolterraLAS<T>
    {
        private Func<T, double> r_i;
        private Func<T, double> a_ii;
        private Func<T,T, double> a_ij;
        private Func<T,T, double> e_ij;

        public LotkaVolterraLAS(
            Func<T, double> Growth, Func<T, double> Intra,
            Func<T,T, double> Attack, Func<T,T, double> Efficiency)
        {
            r_i = Growth;
            a_ii = Intra;
            a_ij = Attack;
            e_ij = Efficiency;
        }

        // external dictionary for lookup
        Dictionary<T, int> speciesDict = new Dictionary<T, int>();
        Dictionary<T, HashSet<T>> speciesAdjacency = new Dictionary<T, HashSet<T>>();
        Dictionary<T, double> equilibriumAbundances = new Dictionary<T, double>();
        Dictionary<T, Dictionary<T, double>> flux = new Dictionary<T, Dictionary<T, double>>();

        // internal 0-based indexing for matrix operations
        List<T> speciesList = new List<T>();
        List<HashSet<int>> adjacency = new List<HashSet<int>>();

        public void AddSpecies(T species)
        {
            if (speciesDict.ContainsKey(species))
                throw new Exception("Ecosystem has that species already");

            int n = speciesList.Count;

            speciesDict[species] = n;
            speciesAdjacency[species] = new HashSet<T>();
            equilibriumAbundances[species] = 0;
            flux[species] = new Dictionary<T, double>();

            speciesList.Add(species);
            adjacency.Add(new HashSet<int>());

            RebuildMatrices(n+1);
        }
        public void RemoveSpecies(T species)
        {
            if (!speciesDict.ContainsKey(species))
                throw new Exception("Ecosystem does not have that species");

            int n = speciesList.Count;

            speciesDict.Remove(species);
            speciesAdjacency.Remove(species);
            foreach (var hash in speciesAdjacency.Values)
                hash.Remove(species);

            equilibriumAbundances.Remove(species);
            flux.Remove(species);
            foreach (var dict in flux.Values)
                dict.Remove(species);

            // O(n) but that's okay, as it shifts indices as required
            speciesList.Remove(species);

            // iterate through all possible interactions to fix adjacency indices
            adjacency.RemoveAt(n-1);
            for (int i=0; i<n-1; i++)
            {
                T res = speciesList[i];
                adjacency[i].Clear();
                for (int j=0; j<n-1; j++)
                {
                    T con = speciesList[j];
                    if (speciesAdjacency[res].Contains(con))
                        adjacency[i].Add(j);
                }
            }

            RebuildMatrices(n-1);
        }
        public void AddInteraction(T res, T con)
        {
            if (speciesAdjacency[res].Contains(con))
                throw new Exception("ecosystem has interaction already");

            speciesAdjacency[res].Add(con);
            adjacency[speciesDict[res]].Add(speciesDict[con]);
        }
        public void RemoveInteraction(T res, T con)
        {
            if (!speciesAdjacency[res].Contains(con))
                throw new Exception("ecosystem does not have that interaction");

            speciesAdjacency[res].Remove(con);
            adjacency[speciesDict[res]].Remove(speciesDict[con]);
        }

        Matrix<double> A, community;
        Vector<double> x, b;
        public void RebuildMatrices(int n)
        {
            if (n > 0)
            {
                // A is interaction matrix, x is equilibrium abundance, b is -r
                A = Matrix<double>.Build.Dense(n, n);
                x = Vector<double>.Build.Dense(n);
                b = Vector<double>.Build.Dense(n);

                // community is Jacobian evaluated at x, flux is e*A
                community = Matrix<double>.Build.Dense(n, n);
            }
            else
            {
                A = community = null;
                x = b = null;
            }
        }

        ///////////////////////////////////////
        // should be run async because O(n^3)

        public void SolveEquilibrium()
        {
            // create Matrix and Vector that MathNet understands
            int n = speciesList.Count;

            A.Clear();
            b.Clear();
            for (int i=0; i<n; i++)
            {
                T res = speciesList[i];
                b[i] = -r_i(res); // TODO: can maybe remove this minus operator
                A[i,i] = a_ii(res);
                foreach (int j in adjacency[i])
                {
                    T con = speciesList[j];
                    double a = a_ij(res, con);
                    double e = e_ij(res, con);
                    A[i,j] -= a;
                    A[j,i] += flux[res][con] = e*a; // set flux values here
                    // TODO: this flux has leaks but will prob not cause issues
                }
            }
            // UnityEngine.Debug.Log(MathNetMatStr(A));
            // UnityEngine.Debug.Log(MathNetVecStr(b));

            // find stable equilibrium point of system
            x = A.Solve(b);
            for (int i=0; i<speciesList.Count; i++)
                equilibriumAbundances[speciesList[i]] = x[i];

            // UnityEngine.Debug.Log(MathNetVecStr(x));
        }

        // Depends on A and b being correct
        void BuildCommunityMatrix()
        {
            int n = speciesList.Count;

            community.Clear();
            for (int i=0; i<n; i++)
            {
                // calculate every element of the Jacobian, evaluated at equilibrium point
                // for (int j=0; j<n; j++)
                // {
                //     if (i==j)
                //     {
                //         community[i,i] += -b[i] + (2*A[i,i] * x[i]);
                //     }
                //     else
                //     {
                //         community[i,j] = A[i,j] * x[i];
                //         community[i,i] += A[i,j] * x[j];
                //     }
                // }
                
                // Cramer's rule to simplify the above commented
                community[i,i] = A[i,i] * x[i];
                for (int j=0; j<i; j++)
                    community[i,j] = A[i,j] * x[i];
                for (int j=i+1; j<n; j++)
                    community[i,j] = A[i,j] * x[i];
            }
        }

        ///////////////////////////////////////////
        // should be run async because O(n^3)

        public double LocalAsymptoticStability()
        {
            BuildCommunityMatrix();

            // calculate community matrix with jacobian
            var eigenValues = community.Evd().EigenValues;

            // get largest real part of any eigenvalue
            double Lambda = eigenValues.Real().Maximum();
            return Lambda;
        }
        public double GetAbundance(T species)
        {
            return equilibriumAbundances[species];
        }
        public double GetFlux(T res, T con)
        {
            return flux[res][con];
        }


        public static string MathNetMatStr(Matrix<double> mat)
        {
            var sb = new System.Text.StringBuilder();
            // int m = mat.GetLength(0), n = mat.GetLength(1);
            int n = mat.RowCount, m = mat.ColumnCount;
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
        
        public static string MathNetVecStr(Vector<double> vec)
        {
            var sb = new System.Text.StringBuilder();
            int n = vec.Count;
            for (int i=0; i<n; i++)
            {
                    sb.Append(vec[i].ToString() + " ");
            }
            sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }
    }
}