using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CellularAutomata
{
    public class PatternRecognizer
    {
        public List<HashSet<(int x, int y)>> DetectClusters(GameOfLife simulation)
        {
            var clusters = new List<HashSet<(int x, int y)>>();
            var visitedCells = new bool[simulation.Width, simulation.Height];

            for (int y = 0; y < simulation.Height; y++)
            {
                for (int x = 0; x < simulation.Width; x++)
                {
                    if (simulation.GetCellState(x, y) && !visitedCells[x, y])
                    {
                        var cluster = new HashSet<(int x, int y)>();
                        FindConnectedCells(simulation, x, y, visitedCells, cluster);
                        clusters.Add(cluster);
                    }
                }
            }

            return clusters;
        }

        private void FindConnectedCells(GameOfLife simulation, int startX, int startY,
            bool[,] visited, HashSet<(int x, int y)> cluster)
        {
            var cellsToExplore = new Queue<(int x, int y)>();
            cellsToExplore.Enqueue((startX, startY));
            visited[startX, startY] = true;

            while (cellsToExplore.Count > 0)
            {
                var (currentX, currentY) = cellsToExplore.Dequeue();
                cluster.Add((currentX, currentY));

                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        int neighborX = (currentX + dx + simulation.Width) % simulation.Width;
                        int neighborY = (currentY + dy + simulation.Height) % simulation.Height;

                        if (simulation.GetCellState(neighborX, neighborY) && !visited[neighborX, neighborY])
                        {
                            visited[neighborX, neighborY] = true;
                            cellsToExplore.Enqueue((neighborX, neighborY));
                        }
                    }
                }
            }
        }

        public string IdentifyPattern(HashSet<(int x, int y)> cluster, string patternsDirectory = null)
        {
            var normalized = NormalizeClusterCoordinates(cluster);

            if (patternsDirectory != null)
            {
                var knownPatterns = LoadPatternTemplates(patternsDirectory);
                foreach (var (name, pattern) in knownPatterns)
                {
                    if (CompareClusters(normalized, pattern))
                    {
                        return name;
                    }
                }
            }

            return $"Неизвестный паттерн ({cluster.Count} клеток)";
        }

        private HashSet<(int x, int y)> NormalizeClusterCoordinates(HashSet<(int x, int y)> cluster)
        {
            int minX = cluster.Min(p => p.x);
            int minY = cluster.Min(p => p.y);

            return new HashSet<(int x, int y)>(cluster.Select(p => (p.x - minX, p.y - minY)));
        }

        private Dictionary<string, HashSet<(int x, int y)>> LoadPatternTemplates(string directory)
        {
            var templates = new Dictionary<string, HashSet<(int x, int y)>>();

            try
            {
                foreach (var file in Directory.GetFiles(directory, "*.txt"))
                {
                    var pattern = new HashSet<(int x, int y)>();
                    string[] lines = File.ReadAllLines(file);

                    for (int y = 0; y < lines.Length; y++)
                    {
                        for (int x = 0; x < lines[y].Length; x++)
                        {
                            if (lines[y][x] == '1')
                            {
                                pattern.Add((x, y));
                            }
                        }
                    }

                    templates.Add(Path.GetFileNameWithoutExtension(file), pattern);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки шаблонов: {ex.Message}");
            }

            return templates;
        }

        private bool CompareClusters(HashSet<(int x, int y)> cluster1, HashSet<(int x, int y)> cluster2)
        {
            if (cluster1.Count != cluster2.Count)
                return false;

            for (int rotation = 0; rotation < 4; rotation++)
            {
                var rotated = RotateCluster(cluster1, rotation);
                if (rotated.SetEquals(cluster2))
                {
                    return true;
                }
            }

            return false;
        }

        private HashSet<(int x, int y)> RotateCluster(HashSet<(int x, int y)> cluster, int rotations)
        {
            var result = new HashSet<(int x, int y)>();
            int size = cluster.Max(p => Math.Max(p.x, p.y)) + 1;

            foreach (var (x, y) in cluster)
            {
                var (rx, ry) = (x, y);
                for (int i = 0; i < rotations; i++)
                {
                    (rx, ry) = (ry, size - 1 - rx);
                }
                result.Add((rx, ry));
            }

            return result;
        }
    }
}