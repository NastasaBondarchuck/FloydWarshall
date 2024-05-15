using System.Diagnostics;

class FloydWarshall
{
    static void Main()
    {
        Console.Write("Enter the size of the matrix: ");
        int size = int.Parse(Console.ReadLine()!);

        double[,] graph = GenerateRandomGraph(size);            
        PrintMatrix(graph);

        Console.WriteLine($"\nMaximum processors count: {Environment.ProcessorCount}");
        
        try
        {
            Console.WriteLine("\nCalculating...");
            
            int[,] originalPaths = CreatePathMatrix(graph);
            double[,] dist = new double[size, size];
            Array.Copy(graph, dist, graph.Length);
            Stopwatch originalWatch = Stopwatch.StartNew();
            double[,] originalResult = FloydWarshallOriginal(dist, originalPaths);
            originalWatch.Stop();
            Console.WriteLine("\nOriginal algorithm of Floyd-Warshall: " +
                              "Success");
            PrintMatrix(originalResult);
            Console.WriteLine("\nPaths:");
            PrintAllPaths(originalPaths);
            
            int[,] parallelPaths = CreatePathMatrix(graph);
            dist = new double[size, size];
            Array.Copy(graph, dist, graph.Length);
            Stopwatch parallelWatch = Stopwatch.StartNew();
            double[,] parallelResult = FloydWarshallParallel(dist, parallelPaths);

            parallelWatch.Stop();
            Console.WriteLine("\nParallel algorithm of Floyd-Warshall (by splitting the matrix into rows): " +
                              "Success");
            PrintMatrix(parallelResult);
            Console.WriteLine("\nPaths:");
            PrintAllPaths(parallelPaths);
            
            Console.WriteLine("\nComparing results between lengths:");
            CompareLengthResults(originalResult, parallelResult);
            Console.WriteLine("\nComparing results between paths:");
            ComparePathResults(originalPaths, parallelPaths);

            Console.WriteLine("\nExecution time:");
            Console.WriteLine("Original algorithm: " + originalWatch.Elapsed + ".");
            Console.WriteLine("Parallel algorithm: " + parallelWatch.Elapsed + ".");
            
        }
        catch (Exception e)
        {
            Console.WriteLine("\nGraph has a negative cycle.\n");
            throw;
        }
        Console.Read();
    }

    static double[,] GenerateRandomGraph(int size)
    {
        Random rand = new Random();
        double[,] graph = new double[size, size];
        int infinityCounter = 0;
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                int currentRand = rand.Next(-10, 10);
                if (i == j)
                    graph[i, j] = 0;
                else if (currentRand <= -5)
                {
                    graph[i, j] = double.PositiveInfinity;
                    infinityCounter++;
                }
                else
                {
                    graph[i, j] = rand.Next(10);
                }
            }
        }
        Console.WriteLine($"\nCount of infinities: {infinityCounter}\n");
        return graph;
    }

    static int[,] CreatePathMatrix(double[,] graph)
    {
        int size = graph.GetLength(0);
        int[,] pathMatrix = new int[size, size];
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                pathMatrix[i, j] = (double.IsPositiveInfinity(graph[i, j])) ? -1 : j;
            }
        }

        return pathMatrix;
    }

    static double[,] FloydWarshallOriginal(double[,] dist, int[,] pathMatrix)
    {
        int size = dist.GetLength(0);

        for (int k = 0; k < size; k++)
        {
            for (int i = 0; i < size; i++)
            {
                if (double.IsPositiveInfinity(dist[i, k])) continue;
                for (int j = 0; j < size; j++)
                {
                    if (i == j && dist[i, k] + dist[k, j] < 0)
                    {
                        throw new Exception("Negative Cycle Exception");
                    }

                    if (!double.IsPositiveInfinity(dist[k, j]) &&
                        dist[i, k] + dist[k, j] < dist[i, j])
                    {
                        dist[i, j] = dist[i, k] + dist[k, j];
                        pathMatrix[i, j] = pathMatrix[i, k];
                    }
                }
            }
        }

        return dist;
    }

    static double[,] FloydWarshallParallel(double[,] dist, int[,] pathMatrix)
    {
        int size = dist.GetLength(0);

        ParallelOptions options = new ParallelOptions();
        options.MaxDegreeOfParallelism = 8;
        
        for (int k = 0; k < size; k++)
        {
            Parallel.For(0, size, options, i =>
            {
                if (double.IsPositiveInfinity(dist[i, k])) return;
                for (int j = 0; j < size; j++)
                {
                    if (i == j && dist[i, k] + dist[k, j] < 0)
                    {
                        throw new Exception("Negative Cycle Exception");
                    }

                    if (!double.IsPositiveInfinity(dist[i, k]) && !double.IsPositiveInfinity(dist[k, j]) &&
                        dist[i, k] + dist[k, j] < dist[i, j])
                    {
                        dist[i, j] = dist[i, k] + dist[k, j];
                        pathMatrix[i, j] = pathMatrix[i, k];
                    }
                }

            });
        }
        return dist;
    }

    static void PrintMatrix(double[,] dist)
    {
        int size = dist.GetLength(0);
        Console.Write("\t");
        for (int i = 0; i < size; i++)
        {
            Console.Write($"[{i}]\t");
        }

        Console.WriteLine();
        for (int i = 0; i < size; ++i)
        {
            Console.Write($"[{i}]\t");
            for (int j = 0; j < size; ++j)
            {
                if (double.IsPositiveInfinity(dist[i, j]))
                    Console.Write("INF\t");
                else
                    Console.Write(dist[i, j] + "\t");
            }

            Console.WriteLine();
        }
    }

    static void CompareLengthResults(double[,] originalResult, double[,] parallelResult)
    {
        int size = originalResult.GetLength(0);
        int compareCounter = 0;
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (Math.Abs(originalResult[i, j] - parallelResult[i, j]) > 0)
                {
                    compareCounter++;
                }
            }
        }

        Console.WriteLine($"There are {compareCounter} difference between original and parallel algorithms' lengths.");
    }

    static void ComparePathResults(int[,] originalPath, int[,] parallelPath)
    {
        int size = originalPath.GetLength(0);
        int compareCounter = 0;
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (Math.Abs(originalPath[i, j] - parallelPath[i, j]) > 0)
                {
                    compareCounter++;
                }
            }
        }

        Console.WriteLine($"There are {compareCounter} difference between original and parallel algorithms' paths.");
    }

    static void PrintAllPaths(int[,] pathMatrix)
    {
        int size = pathMatrix.GetLength(0);
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (i != j)
                {
                    List<int> path = new List<int> { i };
                    Console.Write($"Path from [{i}] to [{j}]: ");
                    PrintPath(i, j, pathMatrix, path);
                }
            }
        }
    }

    static void PrintPath(int from, int to, int[,] pathMatrix, List<int> path)
    {
        if (from == to)
        {
            Console.WriteLine(string.Join(" -> ", path));
        }
        else if (pathMatrix[from, to] == -1)
        {
            Console.WriteLine("No path.");
        }
        else
        {
            path.Add(pathMatrix[from, to]);
            PrintPath(pathMatrix[from, to], to, pathMatrix, path);
        }
    }
}