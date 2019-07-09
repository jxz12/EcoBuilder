using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;

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
            Func<T, double> r_i, Func<T, double> a_ii,
            Func<T,T, double> a_ij, Func<T,T, double> e_ij)
        {
            this.r_i = r_i;
            this.a_ii = a_ii;
            this.a_ij = a_ij;
            this.e_ij = e_ij;
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

            ResizeMatrices(n+1);
        }
        public void RemoveSpecies(T species)
        {
            if (!externToIntern.ContainsKey(species))
                throw new Exception("Ecosystem does not have that species");

            int n = internToExtern.Count;

            externInteractions.Remove(species);
            foreach (var hash in externInteractions.Values)
                hash.Remove(species);

            // O(n) but that's okay, as it shifts indices as required
            internToExtern.Remove(species);
            externToIntern.Clear();
            for (int i=0; i<n-1; i++)
                externToIntern[internToExtern[i]] = i;

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

            ResizeMatrices(n-1);
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

        // TODO: test if these faster when sparse
        Matrix<double> interaction, flux;
        Vector<double> negGrowth, abundance;
        Matrix<double> community, hermitian;
        public void ResizeMatrices(int n)
        {
            if (n > 0)
            {
                // A is interaction matrix, x is equilibrium abundance, b is -r
                interaction = Matrix<double>.Build.Dense(n, n);
                flux = Matrix<double>.Build.Sparse(n,n); // sparse as no operations required
                abundance = Vector<double>.Build.Dense(n);
                negGrowth = Vector<double>.Build.Dense(n);

                // community is Jacobian evaluated at x
                community = Matrix<double>.Build.Dense(n, n);
                hermitian = Matrix<double>.Build.Dense(n, n);
            }
            else
            {
                interaction = flux = community = hermitian = null;
                abundance = negGrowth = null;
            }
        }

        void BuildInteractionMatrix()
        {
            // create Matrix and Vector that MathNet understands
            int n = internToExtern.Count;

            interaction.Clear();
            negGrowth.Clear();

            // double max=0, min=double.MaxValue;
            for (int i=0; i<n; i++)
            {
                T res = internToExtern[i];
                negGrowth[i] = -r_i(res);
                interaction[i,i] = a_ii(res);
                foreach (int j in internInteractions[i])
                {
                    T con = internToExtern[j];
                    double a = a_ij(res, con);
                    double e = e_ij(res, con);

                    interaction[i,j] -= a;
                    interaction[j,i] += e * a;
                    // max = Math.Max(max, a);
                    // min = Math.Min(min, a);

                    flux[i,j] = e * a;
                }
            }
            // UnityEngine.Debug.Log(min + " " + max);
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
                community[i,i] = interaction[i,i] * abundance[i];
                for (int j=0; j<i; j++)
                    community[i,j] = interaction[i,j] * abundance[i];
                for (int j=i+1; j<n; j++)
                    community[i,j] = interaction[i,j] * abundance[i];
            }
        }

        // // depends on community being correct, and also destroys it
		// void BuildHermitianMatrix()
		// {
		// 	int n = internToExtern.Count;

        //     hermitian.Clear();
		// 	for (int i=0; i<n; i++)
		// 	{
		// 		for (int j=0; j<i; j++)
		// 		{
		// 			hermitian[i,j] = hermitian[j,i] = (community[i,j]+community[j,i]) / 2;
		// 		}
		// 	}
		// }




        ///////////
        // O(n^3)
        public bool SolveFeasibility()
        {
            int n = internToExtern.Count;
            if (n == 0)
            {
                TotalAbundance = TotalFlux = 0;
                return false;
            }

            BuildInteractionMatrix();
            // find fixed equilibrium point of system
            interaction.Solve(negGrowth, abundance);

            // UnityEngine.Debug.Log("A:\n" + MathNetMatStr(interaction));
            // UnityEngine.Debug.Log("b:\n" + MathNetVecStr(negGrowth));
            // UnityEngine.Debug.Log("x:\n" + MathNetVecStr(abundance));

            // solve flux values
            TotalFlux = 0;
            for (int i=0; i<internInteractions.Count; i++)
            {
                foreach (int j in internInteractions[i])
                {
                    flux[i,j] *= abundance[i];
                    TotalFlux += flux[i,j];
                }
            }

            TotalAbundance = 0;
            bool feasible = true;
            for (int i=0; i<n; i++)
            {
                TotalAbundance += abundance[i];
                if (abundance[i] <= 0)
                    feasible = false;
            }
            return feasible;
        }
        public double TotalAbundance { get; private set; }
        public double TotalFlux { get; private set; }

        public double GetSolvedAbundance(T species)
        {
            int idx = externToIntern[species];
            return abundance[idx];
        }
        public double GetSolvedFlux(T res, T con)
        {
            int i = externToIntern[res];
            int j = externToIntern[con];
            return flux[i,j];
        }


        ////////////////////
        // also O(n^3)
        public bool SolveStability()
        {
            int richness = internToExtern.Count;
            if (richness == 0)
            {
                MayComplexity = 0;
                return false;
            }

            BuildCommunityMatrix();
            // UnityEngine.Debug.Log("C:\n" + MathNetMatStr(community));

            var eigenValues = community.Evd().EigenValues;

            // calculate 'complexity'
            int connectance = 0;
            double meanDiag = 0;
            double meanOffDiag = 0;
            for (int i=0; i<richness; i++)
            {
                meanDiag += community[i,i];
                for (int j=0; j<richness; j++)
                {
                    if (i!=j && community[i,j]!=0)
                    {
                        meanOffDiag += community[i,j];
                        connectance += 1;
                    }
                }
            }

            if (richness > 0)
            {
                meanDiag /= richness;
            }

            double standardDev = 0;
            if (connectance > 0)
            {
                meanOffDiag /= connectance;

                for (int i=0; i<richness; i++)
                {
                    for (int j=0; j<richness; j++)
                    {
                        if (i!=j && community[i,j]!=0)
                        {
                            double deviation = community[i,j] - meanOffDiag;
                            standardDev += deviation * deviation;
                        }
                    }
                }
                standardDev = Math.Sqrt(standardDev / connectance);
            }

            MayComplexity = standardDev * Math.Sqrt(richness*connectance) - meanDiag;
            // UnityEngine.Debug.Log(MayComplexity);

            // get largest real part of any eigenvalue of this community matrix
            // to measure the local asymptotic stability
            double Lambda = eigenValues.Real().Maximum();
            return Lambda <= 0;
        }
        public double MayComplexity { get; private set; }
        // public double TangComplexity { get; private set; }


        // public bool SolveReactivity()
		// {
		// 	// calculate M + M^T
		// 	BuildHermitianMatrix();

		// 	// get largest real part of any eigenvalue
		// 	var eigenValues = hermitian.Evd().EigenValues;

		// 	double Lambda = eigenValues.Real().Maximum();
		// 	return Lambda <= 0;
		// }





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