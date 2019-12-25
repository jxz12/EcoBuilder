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

            ResizeMatrices(1); // this is so that things are cached so the first species addition isn't so heavy
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
            internToExtern.Add(species);
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
            if (n > 0 && (interaction==null || n!=interaction.RowCount))
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
            // else
            // {
            //     interaction = flux = community = hermitian = null;
            //     abundance = negGrowth = null;
            // }
        }

        public int Richness { get { return internToExtern.Count; } }

        ///////////
        // O(n^3)
        public bool SolveFeasibility(Func<T, IEnumerable<T>> Consumers)
        {
            if (Richness == 0)
            {
                TotalAbundance = TotalFlux = 0;
                return false;
            }
            interaction.Clear();
            negGrowth.Clear();

            int n = Richness;
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
                }
            }

            // find fixed equilibrium point of system
            interaction.Solve(negGrowth, abundance);

            // UnityEngine.Debug.Log("A:\n" + MathNetMatStr(interaction));
            // UnityEngine.Debug.Log("b:\n" + MathNetVecStr(negGrowth));
            // UnityEngine.Debug.Log("x:\n" + MathNetVecStr(abundance));

            // complete flux values
            TotalFlux = 0;
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
                if (double.IsNaN(abundance[i]) || double.IsInfinity(abundance[i]))
                    abundance[i] = 0;

                TotalAbundance += abundance[i];

                if (abundance[i] <= 0)
                    feasible = false;
            }
            return feasible;
        }
        public double TotalAbundance { get; private set; } = 0;
        public double TotalFlux { get; private set; } = 0;

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

        // Depends on A and b being correct
        void BuildCommunityMatrix()
        {
            if (Richness == 0)
                return;

            // calculates every element of the Jacobian, evaluated at equilibrium point
            community.Clear();
            int n = Richness;
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


        ////////////////////
        // also O(n^3)
        public bool SolveStability()
        {
            if (Richness == 0)
                return false;

            BuildCommunityMatrix();
            // UnityEngine.Debug.Log("C:\n" + MathNetMatStr(community));

            // get largest real part of any eigenvalue of this community matrix
            // to measure the local asymptotic stability
            var eigenValues = community.Evd().EigenValues;
            double Lambda = eigenValues.Real().Maximum();
            return Lambda <= 0;
        }
        public double MayComplexity { get { return CalculateMayComplexity(community); } }
        public double TangComplexity { get { return CalculateTangComplexity(community); } }

        static double CalculateMayComplexity(Matrix<double> community)
        {
            if (community == null)
                return 0;
            if (community.RowCount != community.ColumnCount)
                throw new Exception("community matrix malformed");

            int n = community.RowCount;
            double meanDiag = 0;
            double meanOffDiag = 0;
            int numOffDiag = 0;
            for (int i=0; i<n; i++)
            {
                meanDiag += community[i,i];
                for (int j=0; j<n; j++)
                {
                    if (i!=j && community[i,j]!=0)
                    {
                        meanOffDiag += community[i,j];
                        numOffDiag += 1;
                    }
                }
            }
            if (numOffDiag == 0)
                return 0;

            meanDiag /= n;
            meanOffDiag /= numOffDiag; // only account for non zeros

            double variance = 0;
            for (int i=0; i<n; i++)
            {
                for (int j=0; j<n; j++)
                {
                    if (i!=j && community[i,j]!=0)
                    {
                        double deviation = community[i,j] - meanOffDiag;
                        variance += deviation * deviation;
                    }
                }
            }
            variance /= numOffDiag;

            // 'complexity' = rho * sqrt(R*C)
            double standardDev = Math.Sqrt(variance);
            double richness = n;
            double connectance = (double)numOffDiag / (richness*(richness-1));
            if (connectance > 1)
                throw new Exception("connectance cannot be above 1");

            double complexity = standardDev * Math.Sqrt(richness*connectance);
            return complexity / -meanDiag;
        }
        static double CalculateTangComplexity(Matrix<double> community)
        {
            if (community == null)
                return 0;
            if (community.RowCount != community.ColumnCount)
                throw new Exception("community matrix malformed");

            int n = community.RowCount;
            double meanDiag = 0;
            double meanOffDiag = 0;
            double meanOffDiagPairs = 0;
            for (int i=0; i<n; i++)
            {
                meanDiag += community[i,i];
                for (int j=0; j<i; j++)
                {
                    meanOffDiag += community[i,j];
                    meanOffDiagPairs += community[i,j] * community[j,i]; // pairs
                }
                for (int j=i+1; j<n; j++)
                {
                    meanOffDiag += community[i,j];
                }
            }
            meanDiag /= community.RowCount;
            meanOffDiag /= n*(n-1);
            meanOffDiagPairs /= (n*(n-1)) / 2;

            double variance = 0;
            for (int i=0; i<n; i++)
            {
                for (int j=0; j<n; j++)
                {
                    if (i != j)
                    {
                        double deviation = community[i,j] - meanOffDiag;
                        variance += deviation * deviation;
                    }
                }
            }
            if (variance == 0)
                return 0;

            variance /= n*(n-1);
            // UnityEngine.Debug.Log("S:"+n+" V:"+variance+" E:"+meanOffDiag+" E2:"+meanOffDiagPairs+" d:"+meanDiag);

            double richness = n;
            double correlation = (meanOffDiagPairs - meanOffDiag*meanOffDiag) / variance;
            double complexity = Math.Sqrt(richness*variance) * (1+correlation) - meanOffDiag;
            return complexity / -meanDiag;
        }

        public double Connectance {
            get {
                int numOffDiag = 0;
                for (int i=0; i<Richness; i++)
                {
                    for (int j=0; j<Richness; j++)
                    {
                        if (i!=j && community[i,j]!=0)
                        {
                            numOffDiag += 1;
                        }
                    }
                }
                if (numOffDiag == 0)
                    return 0;
                else
                    return (double)numOffDiag / (Richness*(Richness-1));
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

        // public bool SolveReactivity()
        // {
        //     // calculate M + M^T
        //     BuildHermitianMatrix();

        //     // get largest real part of any eigenvalue
        //     var eigenValues = hermitian.Evd().EigenValues;

        //     double Lambda = eigenValues.Real().Maximum();
        //     return Lambda <= 0;
        // }

        public double[,] GetState()
        {
            // columns:
            // A*x=b
            // n 1 1

            var state = new double[Richness, Richness+2];
            for (int row=0; row<Richness; row++)
            {
                for (int col=0; col<Richness; col++)
                {
                    state[row,col] = interaction[row,col];
                }
                state[row, Richness] = abundance[row];
                state[row, Richness+1] = negGrowth[row];
            }
            return state;
        }




        /////////////////////
        // helper functions

        static string MathNetMatStr(Matrix<double> mat, string formatter="e1")
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
        
        static string MathNetVecStr(Vector<double> vec, string formatter="e2")
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