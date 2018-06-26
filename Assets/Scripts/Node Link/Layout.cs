using System;
using System.Collections.Generic;
using UnityEngine;

public static class Layout
{ 
    public static void Bacon(Func<int, int, bool> Adj, int[,] d, int sourceIdx)
    {
        int n = d.GetLength(1);

        // keep a dictionary of previously visited nodes to backtrack the shortest paths to get there
        //var prevVisits = new Dictionary<int, int>();
        var prevVisits = new int[n];
        for (int i = 0; i < n; i++)
            prevVisits[i] = -1;

        // create a queue for the BFS
        var currentSearchNodes = new Queue<int>();
        currentSearchNodes.Enqueue(sourceIdx);

        // then take breadth steps from the node in order to fill up previousVisitedFlags
        while (currentSearchNodes.Count > 0)
        {
            int currentNode = currentSearchNodes.Dequeue();
            // try hopping to each connected node
            for (int nextNode = 0; nextNode < n; nextNode++)
            {
                if (currentNode != nextNode && Adj(currentNode, nextNode))
                {
                    // if we havent seen it before
                    //if (prevVisits.ContainsKey(nextNode) == false)
                    if (prevVisits[nextNode] == -1)
                    {
                        // add it to the queue to explore from there
                        currentSearchNodes.Enqueue(nextNode);
                        // and set its flag to the node we hopped from
                        prevVisits[nextNode] = currentNode;
                    }
                }
            }
        }

        // backtrack for each node to get the distance
        //foreach (var kvp in prevVisits)
        for (int i = 0; i < n; i++)
        {
            //int destinationIdx = kvp.Key;
            //int current = kvp.Value;
            int destinationIdx = i;
            int current = prevVisits[i];
            if (current == -1)
                //continue;
                d[sourceIdx, destinationIdx] = 0;
            else
            {
                int distance = 1;
                while (current != sourceIdx)
                {
                    current = prevVisits[current];
                    distance++;
                }
                //d[sourceIdx, destinationIdx] = d[destinationIdx, sourceIdx] = distance;
                d[sourceIdx, destinationIdx] = distance;
            }
        }
    }
    public static void Bacon2(Func<int, int, bool> Adj, int[,] d, int sourceIdx, int maxDepth)
    {
        int n = d.GetLength(1);

        for (int i = 0; i < n; i++)
            d[sourceIdx, i] = d[i, sourceIdx] = 0;

        int depth = 0;
        var visited = new HashSet<int>();
        var toVisit = new Queue<int>();
        toVisit.Enqueue(sourceIdx);
        toVisit.Enqueue(-1);

        while (true)
        {
            int next = toVisit.Dequeue();

            if (next == -1)
            {
                //if (toVisit.Count == 0)
                if (toVisit.Count == 0 || depth >= maxDepth)
                    break;

                toVisit.Enqueue(-1);
                depth++;
            }
            else
            {
                d[sourceIdx, next] = d[next, sourceIdx] = depth;
                visited.Add(next);

                for (int nextnext=0; nextnext<n; nextnext++)
                {
                    if (Adj(next,nextnext) && !visited.Contains(nextnext))
                    {
                        toVisit.Enqueue(nextnext);
                    }
                }
            }
        }
    }

    public static void Majorization(Vector3[] X, int[,] d)
    {
        int n = X.Length;
        for (int i=0; i<n; i++) {

            float topSumX = 0, topSumY = 0, topSumZ = 0, botSum=0;
            for (int j=0; j<n; j++) {
                if (i!=j) {
                    int d_ij = d[i,j];
                    float w_ij = 1f / (d_ij * d_ij);
                    float magnitude = (X[i] - X[j]).magnitude;

                    topSumX += w_ij * (X[j].x + (d_ij*(X[i].x - X[j].x))/magnitude);
                    topSumY += w_ij * (X[j].y + (d_ij*(X[i].y - X[j].y))/magnitude);
                    topSumZ += w_ij * (X[j].z + (d_ij*(X[i].z - X[j].z))/magnitude);
                    botSum += w_ij;
                }
            }

            float newX = topSumX / botSum;
            float newY = topSumY / botSum;
            float newZ = topSumZ / botSum;

            X[i] = new Vector3(newX, newY, newZ);
        }

    }

    const float sep = 2f;
    // localized majorization, converges slowly but that's what we want!
    public static void MajorizationFixedY(Vector3[] X, int[,] d, int i, float y, float w_0)
    {
        int n = X.Length;
        //if (n < 2)
        //    return;

        float topSumX = 0, topSumZ = 0, botSum = 0;
        for (int j=0; j<n; j++) {
            if (i == j)
                continue;

            float d_ij = d[i, j];
            float magnitude = (X[i] - X[j]).magnitude;

            if (d_ij == 0)
            {
                if (magnitude > sep)
                    continue;
                else
                    d_ij = sep;
            }

            float w_ij = 1f / (d_ij * d_ij);
            //float w_ij = 1f;

            topSumX += w_ij * (X[j].x + (d_ij*(X[i].x - X[j].x))/magnitude);
            topSumZ += w_ij * (X[j].z + (d_ij*(X[i].z - X[j].z))/magnitude);
            botSum += w_ij;
        }
        //if (botSum == 0)
        //    return;
        //add a small attractive force to the centre line
        float mag = Mathf.Sqrt(X[i].x * X[i].x + X[i].z * X[i].z);
        topSumX += w_0 * ((.001f * X[i].x) / mag);
        topSumZ += w_0 * ((.001f * X[i].z) / mag);
        botSum += w_0;

        //float bounds = 100;
        //var boundaries = new Vector3[] { bounds * Vector3.right, bounds * Vector3.left, bounds * Vector3.forward, bounds * Vector3.back };
        //foreach (var boundary in boundaries)
        //{
        //    float magnitude = new Vector3(boundary.x - X[i].x, 0, boundary.z - X[i].z).magnitude;

        //    topSumX += w_0 * (boundary.x + (bounds * (X[i].x - boundary.x)) / magnitude);
        //    topSumZ += w_0 * (boundary.z + (bounds * (X[i].z - boundary.z)) / magnitude);
        //    botSum += w_0;
        //}



        float newY = y;

        float newX = topSumX / botSum;
        float newZ = topSumZ / botSum;

        X[i] = new Vector3(newX, newY, newZ);
    }

    //public static void CenteringForce(Vector3[] X, float om = 0.01f)
    //{

    //    if (om > 1)
    //        om = 1;

    //    for (int i=0; i<X.Length; i++)
    //    {
    //        Vector3 toCentre = new Vector3(X[i].x, 0, X[i].z);
    //        // mag is |p-q|
    //        float mag = toCentre.magnitude;
    //        // r is minimum distance each vertex has to move to satisfy the constraint
    //        Vector3 r = ((mag - 0)/2f) * (toCentre/mag);

    //        X[i] -= om * r;
    //    }
    //}

    //public static void WCRSingle(Vector3[] X, int[,] d, float c, int numSatisfactions)
    //{
    //    int n = X.Length;
    //    int nn = (n * (n - 1)) / 2;
    //    for (int it=0; it<numSatisfactions; it++)
    //    {
    //        int ij = UnityEngine.Random.Range(0, nn);
    //        int i = (int)( (1+Math.Sqrt(8*ij+1))/2 );
    //        int j = ij - (i*(i-1))/2;

    //        Satisfy(ref X[i], ref X[j], d[i, j], 1);
    //    }
    //}

    public static void WCR(Vector3[] X, int[,] d, float c)
    {
        int n = X.Length;
        int nn = (n * (n - 1)) / 2;

        var indices = new int[nn];
        for (int i = 0; i < nn; i++)
            indices[i] = i;

        foreach (int ij in ShuffleBag(indices))
        {
            int i = (int)( (1+Math.Sqrt(8*ij+1))/2 );
            int j = ij - (i*(i-1))/2;

            Satisfy(ref X[i], ref X[j], d[i, j], 1);
        }

    }
    static void Satisfy(ref Vector3 x_i, ref Vector3 x_j, float d_ij, float c)
    {
        Vector2 pq = x_i - x_j;
        // mag is |p-q|
        float mag = pq.magnitude;
        // r is minimum distance each vertex has to move to satisfy the constraint
        float r = (d_ij - mag) / 2f;

        float w_ij = 1f / (d_ij * d_ij);
        // weight by a maximum of 2
        float wc = w_ij * c;
        if (wc > 1)
            wc = 1;
        r = wc * r;

        Vector3 m = pq * r / mag;
        x_i += m;
        x_j -= m;
    }
    static IEnumerable<int> ShuffleBag(int[] bag)
    {
        int n = bag.Length;
        for (int i=0; i<n; i++)
        {
            int j = UnityEngine.Random.Range(i, n);
            int toSwap = bag[j];
            bag[j] = bag[i];
            //bag[i] = toSwap;
            yield return toSwap;
        }
    }


    public static void TrophicEquations(Func<int, int, bool> Adj, Func<int, int, double> a_ij, double[,] A, int consumer)
    {
        int n = A.GetLength(0);
        // place the correct values inside the system of linear equations

        int j = consumer;
        double aTotal = 0;

        for (int i=0; i<j; i++)
        	aTotal -= A[j,i] = Adj(i,j)? -a_ij(i,j) : 0;
        for (int i=j+1; i<n; i++)
        	aTotal -= A[j,i] = Adj(i,j)? -a_ij(i,j) : 0;

        if (aTotal == 0)
            return;

        for (int i = 0; i < j; i++)
            A[j, i] /= aTotal;
        for (int i = j + 1; i < n; i++)
            A[j, i] /= aTotal;

        /*for (int i=0; i<n; i++)
        {
            if (i != j) // no cannibalism
            {
                if (Adj(i, j))
                {
                    var a = a_ij(i, j);
                    A[j, i] = -a;
                    aTotal += a;
                }
                else
                    A[j, i] = 0;
            }
        }

        if (aTotal == 0)
            return;

        for (int i=0; i<n; i++)
            if (i != j)
                A[j, i] /= aTotal; */
    }
    public static void GaussSeidel(double[,] A, double[] x, double[] b)
    {
        int n = x.Length;
        for (int i = 0; i < n; i++)
        {
            double temp = 0;
            for (int j = 0; j < i; j++)
                temp += A[i, j] * x[j];

            for (int j = i + 1; j < n; j++)
                temp += A[i, j] * x[j];

            x[i] = (b[i] - temp) / A[i, i];
        }
    }
    public static void GaussSeidel(double[,] A, double[] x, double[] b, int i)
    {
        int n = x.Length;
        double temp = 0;
        for (int j = 0; j < i; j++)
            temp += A[i, j] * x[j];

        for (int j = i + 1; j < n; j++)
            temp += A[i, j] * x[j];

        x[i] = (b[i] - temp) / A[i, i];
    }
}
