using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CellularAutomata
{
    public class StabilityAnalyzer
    {
        private const int StabilityCheckCount = 5;
        private readonly Queue<int> populationHistory = new Queue<int>();

        public bool CheckForStableState(GameOfLife simulation)
        {
            int currentPopulation = simulation.CountLiveCells();
            populationHistory.Enqueue(currentPopulation);

            if (populationHistory.Count > StabilityCheckCount)
            {
                populationHistory.Dequeue();
            }

            return populationHistory.Distinct().Count() == 1 &&
                   populationHistory.Count == StabilityCheckCount;
        }

        public void SaveStabilityData(int generation, string filePath)
        {
            var records = File.Exists(filePath) ?
                File.ReadAllLines(filePath).ToList() :
                new List<string>();

            string newRecord = $"{DateTime.Now}: {generation}";

            if (records.Count > 0)
            {
                records[^1] = newRecord;
            }
            else
            {
                records.Add(newRecord);
            }

            int average = (int)records.Take(records.Count - 1)
                .Select(line => int.Parse(line.Split(": ")[1]))
                .DefaultIfEmpty(0)
                .Average();

            records.Add($"Среднее поколений до стабильности: {average}");
            File.WriteAllLines(filePath, records);
        }
    }
}