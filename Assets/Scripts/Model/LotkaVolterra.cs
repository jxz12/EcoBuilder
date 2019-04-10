using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Solvers;
using MathNet.Numerics.LinearAlgebra.Double.Solvers;

namespace EcoBuilder.Model
{
    // a class to implement a simple lotka-volterra Type I functional response model
    // can find equilibrium abundances, flux at equilibrium, local asymptotic stability
    public class LotkaVolterra<T>
    {
        private Func<T, double> r_i;
        private Func<T, double> a_ii;
        private Func<T,T, double> a_ij;
        private Func<T,T, double> e_ij;

        public LotkaVolterra(
            Func<T, double> Growth, Func<T, double> Intra,
            Func<T,T, double> Attack, Func<T,T, double> Efficiency)
        {
            r_i = Growth;
            a_ii = Intra;
            a_ij = Attack;
            e_ij = Efficiency;
        }

        // external dictionary for lookup
        Dictionary<T, int> externToIntern = new Dictionary<T, int>();
        Dictionary<T, HashSet<T>> externInteractions = new Dictionary<T, HashSet<T>>();

        // internal 0-based indexing for matrix operations
        List<T> internToExtern = new List<T>();
        List<HashSet<int>> internInteractions = new List<HashSet<int>>();

        public void AddSpecies(T species)
        {
            if (externToIntern.ContainsKey(species))
                throw new Exception("Ecosystem has that species already");

            int n = internToExtern.Count;

            externToIntern[species] = n;
            externInteractions[species] = new HashSet<T>();

            internToExtern.Add(species);
            internInteractions.Add(new HashSet<int>());

            InitMatrices(n+1);
        }
        public void RemoveSpecies(T species)
        {
            if (!externToIntern.ContainsKey(species))
                throw new Exception("Ecosystem does not have that species");

            int n = internToExtern.Count;

            externToIntern.Remove(species);
            externInteractions.Remove(species);
            foreach (var hash in externInteractions.Values)
                hash.Remove(species);

            // O(n) but that's okay, as it shifts indices as required
            internToExtern.Remove(species);

            // iterate through all possible interactions
            // to fix adjacency indices according to the above shift
            internInteractions.RemoveAt(n-1);
            for (int i=0; i<n-1; i++)
            {
                T res = internToExtern[i];
                internInteractions[i].Clear();
                for (int j=0; j<n-1; j++)
                {
                    T con = internToExtern[j];
                    if (externInteractions[res].Contains(con))
                        internInteractions[i].Add(j);
                }
            }

            InitMatrices(n-1);
        }
        public void AddInteraction(T res, T con)
        {
            if (externInteractions[res].Contains(con))
                throw new Exception("ecosystem has interaction already");

            externInteractions[res].Add(con);
            internInteractions[externToIntern[res]].Add(externToIntern[con]);
        }
        public void RemoveInteraction(T res, T con)
        {
            if (!externInteractions[res].Contains(con))
                throw new Exception("ecosystem does not have that interaction");

            externInteractions[res].Remove(con);
            internInteractions[externToIntern[res]].Remove(externToIntern[con]);
        }

        Matrix<double> A, community;
        Vector<double> x, b;
        public void InitMatrices(int n)
        {
            if (n > 0)
            {
                // A is interaction matrix, x is equilibrium abundance, b is -r
                A = Matrix<double>.Build.Dense(n, n);
                x = Vector<double>.Build.Dense(n);
                b = Vector<double>.Build.Dense(n);

                // community is Jacobian evaluated at x
                community = Matrix<double>.Build.Dense(n, n);
            }
            else
            {
                A = community = null;
                x = b = null;
            }
        }

        // public event Action<T, double> OnAbundanceSet;
        // public event Action<T, T, double> OnFluxSet;

        void BuildEquilibriumMatrix()
        {
            // create Matrix and Vector that MathNet understands
            int n = internToExtern.Count;

            A.Clear();
            b.Clear();
            for (int i=0; i<n; i++)
            {
                T res = internToExtern[i];
                b[i] = -r_i(res);
                A[i,i] = a_ii(res);
                foreach (int j in internInteractions[i])
                {
                    T con = internToExtern[j];
                    double a = a_ij(res, con);
                    double e = e_ij(res, con);

                    A[i,j] -= a;
                    A[j,i] += e * a;
                }
            }
            // precond.Initialize(A);
            // UnityEngine.Debug.Log(MathNetMatStr(A));
            // UnityEngine.Debug.Log(MathNetVecStr(b));
        }

        /*
        ILU0Preconditioner precond = new ILU0Preconditioner();
        BiCgStab solver = new BiCgStab();
        //////////////////////////////////////
        // O(n^2), so can do it every frame
        public void IterateEquilibrium()
        {
            var iterator = new Iterator<double>(
                new IterationCountStopCriterion<double>(5),
                // new ResidualStopCriterion<double>(tolerance),
                new DivergenceStopCriterion<double>()
            );

            // solver.Solve(A, b, x, iterator, precond);

            // MILU0Preconditioner precond = new MILU0Preconditioner();
            // var precond = new DiagonalPreconditioner();
            // BiCgStab solver = new BiCgStab();

            precond.Initialize(A);
            solver.Solve(A, b, x, iterator, precond);
            UnityEngine.Debug.Log(A.Determinant() + ": " +  MathNetVecStr(x));
        }
        */

        public void Test(int n = 1000)
        {
            var asd = Matrix<double>.Build.Dense(n, n);
            var f = Vector<double>.Build.Dense(n);
            Random rand = new Random();
            for (int i=0; i<n; i++)
            {
                f[i] = rand.NextDouble();
                for (int j=0; j<n; j++)
                {
                    asd[i,j] = rand.NextDouble();
                }
            }
            var fgh = asd.Solve(f);
            // var fgh = asd.Multiply(asd);
        }

        ///////////
        // O(n^3)
        public bool SolveEquilibrium()
        {
            BuildEquilibriumMatrix();
            // UnityEngine.Debug.Log(MathNetMatStr(A));
            // UnityEngine.Debug.Log(MathNetVecStr(b));

            // find fixed equilibrium point of system
            A.Solve(b, x);
            // UnityEngine.Debug.Log(MathNetVecStr(x));

            return Feasible();
        }
        public double GetSolvedAbundance(T species)
        {
            int idx = externToIntern[species];
            return x[idx];
        }
        bool Feasible()
        {
            for (int i=0; i<x.Count; i++)
            {
                if (x[i] <= 0)
                    return false;
            }
            return true;
        }

        public double GetTotalFlux()
        {
            double flux = 0;
            foreach (T res in externInteractions.Keys)
            {
                int i = externToIntern[res];
                double abundance = x[i];
                foreach (T con in externInteractions[res])
                {
                    double a = a_ij(res, con);
                    double e = e_ij(res, con);
                    flux += a * e * abundance;
                }
            }
            return flux;
        }



        // Depends on A and b being correct
        void BuildCommunityMatrix()
        {
            // calculates every element of the Jacobian, evaluated at equilibrium point
            int n = internToExtern.Count;

            community.Clear();
            for (int i=0; i<n; i++)
            {
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

        // depends on community being correct, and also destroys it
		void BuildHermitianMatrix()
		{
			int n = internToExtern.Count;

			for (int i=0; i<n; i++)
			{
				for (int j=0; j<i; j++)
				{
					community[i,j] = community[j,i] = (community[i,j]+community[j,i]) / 2;
				}
			}
		}

        ////////////////////
        // Both also O(n^3)
        public bool SolveStability()
        {
            // calculate community matrix with Jacobian
            BuildCommunityMatrix();

            // get largest real part of any eigenvalue of this community matrix
            // implies the local asymptotic stability
            var eigenValues = community.Evd().EigenValues;

            double Lambda = eigenValues.Real().Maximum();
            return Lambda <= 0;
        }

        public bool SolveReactivity()
		{
			// calculate M + M^T
			BuildHermitianMatrix();

			// get largest real part of any eigenvalue
			var eigenValues = community.Evd().EigenValues;

			double Lambda = eigenValues.Real().Maximum();
			return Lambda <= 0;
		}





        /////////////////////
        // helper functions

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