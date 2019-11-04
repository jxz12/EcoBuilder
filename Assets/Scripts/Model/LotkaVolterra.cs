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
        // internal 0-based indexing for matrix operations
        List<T> internToExtern = new List<T>();

        public void AddSpecies(T species)
        {
            if (externToIntern.ContainsKey(species))
                throw new Exception("Ecosystem has that species already");

            int n = internToExtern.Count;

            externToIntern[species] = n;
            // externInteractions[species] = new HashSet<T>();

            internToExtern.Add(species);
            // internInteractions.Add(new HashSet<int>());

            ResizeMatrices(n+1);
        }
        public void RemoveSpecies(T species)
        {
            if (!externToIntern.ContainsKey(species))
                throw new Exception("Ecosystem does not have that species");

            int n = internToExtern.Count;

            // O(n) but that's okay, as it shifts indices as required
            internToExtern.Remove(species);
            externToIntern.Clear();
            for (int i=0; i<n-1; i++)
                externToIntern[internToExtern[i]] = i;

            ResizeMatrices(n-1);
        }

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

        int numSpecies { get { return internToExtern.Count; } }
        int numInteractions;
        public void BuildInteractionMatrix(Func<T, IEnumerable<T>> Consumers)
        {
            if (numSpecies == 0)
                return;

            interaction.Clear();
            negGrowth.Clear();

            numInteractions = 0;
            int n = numSpecies;
            for (int i=0; i<n; i++)
            {
                T res = internToExtern[i];
                negGrowth[i] = -r_i(res);
                interaction[i,i] = a_ii(res);
                foreach (T con in Consumers(res))
                {
                    int j = externToIntern[con];
                    double a = a_ij(res, con);
                    double e = e_ij(res, con);

                    interaction[i,j] -= a;
                    interaction[j,i] += e * a;
                    flux[i,j] = e * a; // init flux here, multiply by abundances later
                    numInteractions += 1;
                }
            }
        }

        // Depends on A and b being correct
        void BuildCommunityMatrix()
        {
            if (numSpecies == 0)
                return;

            // calculates every element of the Jacobian, evaluated at equilibrium point
            community.Clear();
            int n = numSpecies;
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
        //     int n = internToExtern.Count;

        //     hermitian.Clear();
        //     for (int i=0; i<n; i++)
        //     {
        //         for (int j=0; j<i; j++)
        //         {
        //             hermitian[i,j] = hermitian[j,i] = (community[i,j]+community[j,i]) / 2;
        //         }
        //     }
        // }

        ///////////
        // O(n^3)
        public bool SolveFeasibility(Func<T, IEnumerable<T>> Consumers)
        {
            if (numSpecies == 0)
            {
                TotalAbundance = TotalFlux = 0;
                return true;
            }

            // BuildInteractionMatrix(Consumers);
            // find fixed equilibrium point of system
            interaction.Solve(negGrowth, abundance);

            // UnityEngine.Debug.Log("A:\n" + MathNetMatStr(interaction));
            // UnityEngine.Debug.Log("b:\n" + MathNetVecStr(negGrowth));
            // UnityEngine.Debug.Log("x:\n" + MathNetVecStr(abundance));

            // solve flux values
            TotalFlux = 0;
            int n = numSpecies;
            for (int i=0; i<n; i++)
            {
                T res = internToExtern[i];
                foreach (T con in Consumers(res))
                {
                    int j = externToIntern[con];
                    flux[i,j] *= abundance[i] * abundance[j]; // complete values
                    TotalFlux += flux[i,j];
                }
            }

            TotalAbundance = 0;
            bool feasible = true;
            for (int i=0; i<n; i++)
            {
                TotalAbundance += abundance[i];
                if (double.IsNaN(abundance[i]) || double.IsInfinity(abundance[i]))
                    abundance[i] = 0;

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
            if (numSpecies == 0)
                return false;

            BuildCommunityMatrix();
            // UnityEngine.Debug.Log("C:\n" + MathNetMatStr(community));

            // get largest real part of any eigenvalue of this community matrix
            // to measure the local asymptotic stability
            var eigenValues = community.Evd().EigenValues;
            double Lambda = eigenValues.Real().Maximum();
            return Lambda <= 0;
        }
        public double CalculateMayComplexity()
        {
            // TODO: score function... AGAIN :(

            // TODO: grass shrub tree
            // TODO: make size not equal population (health bar instead? outline?)
            // TODO: report card
            // TODO: add super animations for stars 
            // TODO: separate the competitive exclusion level into two levels - one where you edit the traits, one were you add another species
            // TODO: last example is a straight line: over-exploitation (nah)
            // TODO: print all parameters to file
            if (numSpecies == 0 || numInteractions == 0)
                return 0;
            
            // 'complexity' = rho * sqrt(R*C)
            // double mean = 0;
            // double meanDiag = 0;
            double meanOffDiag = 0;
            int numOffDiagEntries = 0;
            // double meanOffDiagPairs = 0;
            for (int i=0; i<numSpecies; i++)
            {
                // meanDiag += community[i,i];
                for (int j=0; j<numSpecies; j++)
                {
                    // mean += community[i,j];
                    if (i!=j && community[i,j]!=0)
                    {
                        meanOffDiag += community[i,j];
                        numOffDiagEntries += 1;
                    }
                    // if (i<j)
                    //     meanOffDiagPairs += community[i,j] * community[j,i];
                }
            }

            // only account for non zeros
            double variance = 0;
            double standardDev = 0;
            meanOffDiag /= numOffDiagEntries;

            for (int i=0; i<numSpecies; i++)
            {
                for (int j=0; j<numSpecies; j++)
                {
                    if (i!=j && community[i,j]!=0)
                    {
                        double deviation = community[i,j] - meanOffDiag;
                        variance += deviation * deviation;
                    }
                }
            }
            variance /= numOffDiagEntries;
            standardDev = Math.Sqrt(variance);

            // double mayComplexity = standardDev * Math.Sqrt(richness*connectance) - meanDiag;
            double richness = numSpecies;
            double connectance = (double)numInteractions / ((numSpecies*(numSpecies-1))/2);
            double mayComplexity = standardDev * Math.Sqrt(richness*connectance);
            return mayComplexity;

            // if (variance > 0)
            // {
            //     double correlation = (meanOffDiagPairs - meanOffDiag*meanOffDiag) / variance;
            //     TangComplexity = MayComplexity * (1+correlation) - mean - meanDiag;
            // }
            // else
            // {
            //     TangComplexity = 0;
            // }
        }

        // public bool SolveReactivity()
        // {
        //     // calculate M + M^T
        //     BuildHermitianMatrix();

        //     // get largest real part of any eigenvalue
        //     var eigenValues = hermitian.Evd().EigenValues;

        //     double Lambda = eigenValues.Real().Maximum();
        //     return Lambda <= 0;
        // }





        /////////////////////
        // helper functions

        public static string MathNetMatStr(Matrix<double> mat, string formatter="e1")
        {
            var sb = new System.Text.StringBuilder();
            // int m = mat.GetLength(0), n = mat.GetLength(1);
            int n = mat.RowCount, m = mat.ColumnCount;
            for (int i=0; i<m; i++)
            {
                for (int j=0; j<n; j++)
                {
                    sb.Append(mat[i, j].ToString(formatter) + " ");
                }
                sb.Append("\n");
            }
            sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }
        
        public static string MathNetVecStr(Vector<double> vec, string formatter="e2")
        {
            var sb = new System.Text.StringBuilder();
            int n = vec.Count;
            for (int i=0; i<n; i++)
            {
                    sb.Append(vec[i].ToString(formatter) + " ");
            }
            sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }
    }
}