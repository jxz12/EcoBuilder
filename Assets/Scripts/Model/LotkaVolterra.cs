using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;

namespace EcoBuilder.Model
{
	public class LotkaVolterra
	{
        // external dictionary for lookup
        Dictionary<int, int> externToIntern = new Dictionary<int, int>();
        Dictionary<int, HashSet<int>> externAdjacency = new Dictionary<int, HashSet<int>>();

        // internal 0-based indexing for matrix operations
        List<int> internToExtern = new List<int>();
        List<HashSet<int>> adjacency = new List<HashSet<int>>();

		public LotkaVolterra()
		{

		}

        public void AddSpecies(int idx)
        {
            if (externToIntern.ContainsKey(idx))
                throw new Exception("Ecosystem has that species already");

            int n = internToExtern.Count;

            externToIntern[idx] = n;
            externAdjacency[idx] = new HashSet<int>();

            internToExtern.Add(idx);
            adjacency.Add(new HashSet<int>());

            RebuildMatrices(n+1);
        }
        public void RemoveSpecies(int idx)
        {
            if (!externToIntern.ContainsKey(idx))
                throw new Exception("Ecosystem does not have that species");

            int n = internToExtern.Count;

            externToIntern.Remove(idx);
            externAdjacency.Remove(idx);
            foreach (var hash in externAdjacency.Values)
                hash.Remove(idx);

            // O(n) but that's okay, as it shifts indices as required
            internToExtern.Remove(idx);

            // iterate through all possible interactions
            // to fix adjacency indices according to the above shift
            adjacency.RemoveAt(n-1);
            for (int i=0; i<n-1; i++)
            {
                int res = internToExtern[i];
                adjacency[i].Clear();
                for (int j=0; j<n-1; j++)
                {
                    int con = internToExtern[j];
                    if (externAdjacency[res].Contains(con))
                        adjacency[i].Add(j);
                }
            }

            RebuildMatrices(n-1);
        }
        public void AddInteraction(int res, int con, double search, double efficiency)
        {
            if (externAdjacency[res].Contains(con))
                throw new Exception("ecosystem has interaction already");

            externAdjacency[res].Add(con);
            adjacency[externToIntern[res]].Add(externToIntern[con]);
        }
        public void RemoveInteraction(int res, int con, double search, double efficiency)
        {
            if (!externAdjacency[res].Contains(con))
                throw new Exception("ecosystem does not have that interaction");

            externAdjacency[res].Remove(con);
            adjacency[externToIntern[res]].Remove(externToIntern[con]);
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

        // public event Action<T, double> OnAbundanceSet;
        // public event Action<T, T, double> OnFluxSet;

        ///////////////////////////////////////
        // should be run async because O(n^3)

        public bool SolveEquilibrium()
        {
            // create Matrix and Vector that MathNet understands
            int n = internToExtern.Count;

            A.Clear();
            b.Clear();
            for (int i=0; i<n; i++)
            {
                // T res = internToExtern[i];
                // b[i] = -r_i(res);
                // A[i,i] = a_ii(res);
                // foreach (int j in adjacency[i])
                // {
                //     T con = internToExtern[j];
                //     double a = a_ij(res, con);
                //     double e = e_ij(res, con);

                //     A[i,j] -= a;
                //     double flux = e*a;
                //     A[j,i] += flux;
                //     // OnFluxSet(res, con, flux);
                // }
            }
            // UnityEngine.Debug.Log(MathNetMatStr(A));
            // UnityEngine.Debug.Log(MathNetVecStr(b));

            // find fixed equilibrium point of system
            x = A.Solve(b);

            // UnityEngine.Debug.Log(MathNetVecStr(x));

            bool feasible = true;
            for (int i=0; i<internToExtern.Count; i++)
            {
                // OnAbundanceSet(internToExtern[i], x[i]);
                if (x[i] <= 0)
                    feasible = false;
            }
            return feasible;
        }


        // Depends on A and b being correct
        void BuildCommunityMatrix()
        {
            int n = internToExtern.Count;

            // calculates every element of the Jacobian, evaluated at equilibrium point
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

        ///////////////////////////////////////////
        // should also be run async because O(n^3)

        public double LocalAsymptoticStability()
        {
            // calculate community matrix with jacobian
            BuildCommunityMatrix();

            // get largest real part of any eigenvalue of this community matrix
            var eigenValues = community.Evd().EigenValues;

            double Lambda = eigenValues.Real().Maximum();
            return Lambda;
        }

		public double LocalReactivity()
		{
			// calculate M + M^T
			BuildHermitianMatrix();

			// get largest real part of any eigenvalue
			var eigenValues = community.Evd().EigenValues;

			double Lambda = eigenValues.Real().Maximum();
			return Lambda;
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