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

        Dictionary<T, int> speciesDict = new Dictionary<T, int>();
        Dictionary<T, HashSet<T>> speciesAdjacency = new Dictionary<T, HashSet<T>>();
        Dictionary<T, double> equilibriumAbundances = new Dictionary<T, double>();

        List<T> speciesList = new List<T>();
        List<HashSet<int>> adjacency = new List<HashSet<int>>();

        Matrix<double> A, community, flux;
        Vector<double> x, b;
        public void RebuildMatrices(int n)
        {
            // A is interaction matrix, x and b are equilibrium abundance
            A = Matrix<double>.Build.Dense(n, n);
            x = Vector<double>.Build.Dense(n);
            b = Vector<double>.Build.Dense(n);
            community = Matrix<double>.Build.Dense(n, n);
            flux = Matrix<double>.Build.Dense(n, n);
        }

        public void AddSpecies(T species)
        {
            if (speciesDict.ContainsKey(species))
                throw new Exception("Ecosystem has that species already");

            speciesList.Add(species);
            adjacency.Add(new HashSet<int>());

            int n = speciesList.Count;
            speciesDict[species] = n-1;
            speciesAdjacency[species] = new HashSet<T>();
            equilibriumAbundances[species] = 0;

            RebuildMatrices(n);
        }
        public void RemoveSpecies(T species)
        {
            if (!speciesDict.ContainsKey(species))
                throw new Exception("Ecosystem does not have that species");

            speciesDict.Remove(species);
            foreach (T other in speciesList)
                speciesAdjacency[other].Remove(species);
            speciesAdjacency.Remove(species);
            equilibriumAbundances.Remove(species);

            speciesList.Remove(species);
            // iterate through all possible interactions to fix adjacency indices
            int n = speciesList.Count;
            for (int i=0; i<n; i++)
            {
                adjacency[i].Clear();
                for (int j=0; j<speciesList.Count; j++)
                {
                    if (speciesAdjacency[speciesList[i]].Contains(speciesList[j]))
                        adjacency[i].Add(j);
                }
            }
            adjacency.RemoveAt(n);

            if (n > 0)
                RebuildMatrices(n);
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

        // should be run async because O(n^3)
        public void SolveEquilibrium()
        {
            // create Matrix and Vector that MathNet understands
            int n = speciesList.Count;

            A.Clear();
            b.Clear();
            flux.Clear();
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
                    A[j,i] += flux[i,j] = e*a;
                }
            }

            UnityEngine.Debug.Log(MatStr(A));
            // UnityEngine.Debug.Log(VecStr(b));

            // find stable equilibrium point of system
            x = A.Solve(b);
            UnityEngine.Debug.Log(VecStr(x));

            ScaleAbundance();
        }

        // Depends on A and b being correct
        void BuildCommunityMatrix()
        {
            int n = speciesList.Count;

            community.Clear();
            for (int i=0; i<n; i++)
            {
                // calculate every element of the Jacobian, evaluated at equilibrium point
                for (int j=0; j<n; j++)
                {
                    if (i==j)
                    {
                        community[i,i] += -b[i] + (2*A[i,i] * x[i]);
                    }
                    else
                    {
                        community[i,j] = A[i,j] * x[i];
                        community[i,i] += A[i,j] * x[j];
                    }
                }
            }
        }

        // should be run async because O(n^3)
        public double LocalAsymptoticStability()
        {
            BuildCommunityMatrix();
            // UnityEngine.Debug.Log(MatStr(A));
            // UnityEngine.Debug.Log(MatStr(community));

            // calculate community matrix with jacobian
            var eigenValues = community.Evd().EigenValues;

            // get largest real part of any eigenvalue
            double Lambda = -double.MaxValue;
            foreach (var e in eigenValues)
                Lambda = Math.Max(Lambda, e.Real);

            return -Lambda;
        }

        private void ScaleAbundance()
        {
            int n = x.Count;
            double minPosAbundance, maxNegAbundance;

            MaxAbundance = maxNegAbundance = -double.MaxValue;
            MinAbundance = minPosAbundance = double.MaxValue;
            for (int i=0; i<n; i++)
            {
                double abundance = x[i];
                MaxAbundance = Math.Max(abundance, MaxAbundance);
                MinAbundance = Math.Min(abundance, MinAbundance);
                if (abundance > 0)
                    minPosAbundance = Math.Min(abundance, minPosAbundance);
                else if (abundance < 0)
                    maxNegAbundance = Math.Max(abundance, maxNegAbundance);
                // else
                    // throw new Exception("equilibrium population of zero");
            }

            double posLogMaxNorm = 1, negLogMinNorm = 1;
            if (MaxAbundance > 0)
            {
                if (minPosAbundance == MaxAbundance)
                    minPosAbundance = MaxAbundance / Math.Exp(0.5); // result=1.5
                else 
                    posLogMaxNorm = Math.Log(MaxAbundance / minPosAbundance);
            }
            if (MinAbundance < 0)
            {
                if (maxNegAbundance == MinAbundance)
                    maxNegAbundance = MinAbundance / Math.Exp(0.5);
                else
                    negLogMinNorm = Math.Log(MinAbundance / maxNegAbundance);
            }

            for (int i=0; i<n; i++)
            {
                T species = speciesList[i];
                double abundance = x[i];
                if (abundance > 0)
                {
                    equilibriumAbundances[species] = 1 + Math.Log(abundance/minPosAbundance)/posLogMaxNorm;
                }
                else if (abundance < 0)
                    equilibriumAbundances[species] = -1 - Math.Log(abundance/maxNegAbundance)/negLogMinNorm;
                else
                    equilibriumAbundances[species] = 0;
            }
        }
        public double GetAbundance(T species)
        {
            return equilibriumAbundances[species];
        }
        public double GetFlux(T res, T con)
        {
            int i = speciesDict[res], j = speciesDict[con];
            return flux[i,j] * x[i] * x[j];
        }
        public double MaxAbundance { get; private set; }
        public double MinAbundance { get; private set; }


        public static string MatStr(Matrix<double> mat)
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
        
        public static string VecStr(Vector<double> vec)
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