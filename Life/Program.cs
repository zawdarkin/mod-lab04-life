using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using ScottPlot;
using ScottPlot.Colormaps;

namespace CellularAutomata
{
    public class Program
    {
        private static GameOfLife currentSimulation;
        private static SimulationConfig config;
        private static StabilityAnalyzer stabilityAnalyzer = new StabilityAnalyzer();
        private static PatternRecognizer patternRecognizer = new PatternRecognizer();
        private static int currentGeneration;
        private static int generationsToStableState;

        public static void Main(string[] args)
        {
            InitializeApplication();

            Console.WriteLine("Выберите режим моделирования:");
            Console.WriteLine("1 - Загрузка начального состояния из файла");
            Console.WriteLine("2 - Загрузка схемы 'Пушка Госпера'");
            Console.WriteLine("3 - Анализ разных плотностей заполнения");
            Console.Write("Ваш выбор: ");

            var choice = Console.ReadLine();
            ProcessUserChoice(choice);
        }

        private static void InitializeApplication()
        {
            string appDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
            string configPath = Path.Combine(appDirectory, "config.json");
            LoadConfiguration(configPath);
        }

        private static void ProcessUserChoice(string choice)
        {
            switch (choice)
            {
                case "1":
                    StartSimulationFromFile();
                    break;
                case "2":
                    StartSimulationWithGosperGun();
                    break;
                case "3":
                    AnalyzeMultipleDensities();
                    break;
                default:
                    Console.WriteLine("Неверный выбор, запуск с файла по умолчанию");
                    StartSimulationFromFile();
                    break;
            }
        }

        private static void StartSimulationFromFile()
        {
            string boardFile = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, "board.txt");
            string stabilityFile = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, "stab/stable0_1.txt");

            InitializeSimulation(boardFile);
            RunMainSimulationLoop(stabilityFile);
        }

        private static void StartSimulationWithGosperGun()
        {
            string patternsDir = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, "patterns/");
            string dataFile = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, "populationData.txt");
            string plotFile = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, "populationPlot.png");

            InitializeSimulation();
            currentSimulation.LoadPattern(Path.Combine(patternsDir, "gosperGun.txt"));

            RunMainSimulationLoop();
            AnalyzePopulationDynamics(dataFile, plotFile);
        }

        private static void AnalyzeMultipleDensities()
        {
            string dataFile = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, "multiDensityData.txt");
            string plotFile = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, "multiDensityPlot.png");

            double[] densities = { 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9 };
            int maxGenerations = 600;

            File.WriteAllText(dataFile, "Поколение Плотность Количество_клеток\n");

            foreach (var density in densities)
            {
                var simulation = new GameOfLife(50, 20, 1, density);
                for (int gen = 0; gen < maxGenerations; gen++)
                {
                    File.AppendAllText(dataFile, $"{gen} {density} {simulation.CountLiveCells()}\n");
                    simulation.NextGeneration();
                }
            }

            GenerateMultiDensityPlot(dataFile, plotFile);
            Console.WriteLine($"График для разных плотностей сохранен в: {plotFile}");
        }

        private static void GenerateMultiDensityPlot(string dataFile, string plotFile)
        {
            var plotData = File.ReadAllLines(dataFile)
                .Skip(1)
                .Select(line => line.Split(' '))
                .Where(parts => parts.Length == 3)
                .Select(parts => new {
                    Generation = int.Parse(parts[0]),
                    Density = double.Parse(parts[1]),
                    CellCount = int.Parse(parts[2])
                }).ToList();

            var plot = new Plot();
            var colors = new Color[] { Colors.Red, Colors.Orange, Colors.Gold, Colors.Green, Colors.Blue,
                             Colors.Indigo, Colors.Violet, Colors.Gray, Colors.Black };

            var densityGroups = plotData.GroupBy(x => x.Density).OrderBy(g => g.Key).ToList();

            for (int i = 0; i < densityGroups.Count; i++)
            {
                var group = densityGroups[i];
                var scatter = plot.Add.Scatter(
                    group.Select(x => (double)x.Generation).ToArray(),
                    group.Select(x => (double)x.CellCount).ToArray());

                scatter.Color = colors[i];
                scatter.LegendText = $"Плотность {group.Key:F1}";
                scatter.LineWidth = 2;
                scatter.MarkerSize = 0;
            }

            plot.Title("Динамика популяции клеток для разных плотностей", size: 16);
            plot.XLabel("Номер поколения", size: 14);
            plot.YLabel("Количество живых клеток", size: 14);

            plot.Legend.IsVisible = true;
            plot.Legend.Alignment = Alignment.UpperRight;
            plot.Axes.AutoScale();

            plot.SavePng(plotFile, 1000, 600);
        }

        private static void GeneratePopulationPlot(string dataFile, string plotFile)
        {
            var plotData = File.ReadAllLines(dataFile)
                .Skip(1)
                .Select(line => line.Split(' '))
                .Where(parts => parts.Length == 2)
                .Select(parts => new {
                    Generation = int.Parse(parts[0]),
                    CellCount = int.Parse(parts[1])
                }).ToList();

            var plot = new Plot();
            var scatter = plot.Add.Scatter(
                plotData.Select(x => (double)x.Generation).ToArray(),
                plotData.Select(x => (double)x.CellCount).ToArray());

            scatter.Color = Colors.Blue;
            scatter.LineWidth = 2;
            scatter.MarkerSize = 0;

            plot.Title("Динамика популяции клеток (плотность 0.6)", size: 16);
            plot.XLabel("Номер поколения", size: 14);
            plot.YLabel("Количество живых клеток", size: 14);

            plot.Axes.AutoScale();
            plot.SavePng(plotFile, 800, 500);
        }

        private static void LoadConfiguration(string configPath)
        {
            try
            {
                string jsonConfig = File.ReadAllText(configPath);
                config = JsonSerializer.Deserialize<SimulationConfig>(jsonConfig);
            }
            catch
            {
                config = new SimulationConfig();
            }
        }

        private static void InitializeSimulation(string initialStateFile = null)
        {
            currentGeneration = 1;
            generationsToStableState = 1;
            currentSimulation = new GameOfLife(
                config.GridWidth,
                config.GridHeight,
                config.CellDimension,
                config.InitialCellDensity);

            if (!string.IsNullOrEmpty(initialStateFile) && File.Exists(initialStateFile))
            {
                try
                {
                    currentSimulation.ImportState(initialStateFile);
                    Console.WriteLine($"Начальное состояние загружено из {initialStateFile}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка загрузки: {ex.Message}");
                    currentSimulation.Randomize(config.InitialCellDensity);
                }
            }
        }

        private static void RunMainSimulationLoop(string stabilityDataPath = null, bool analyzeClusters = true)
        {
            while (true)
            {
                try
                {
                    if (Console.KeyAvailable)
                    {
                        ProcessUserInput();
                    }

                    DisplayCurrentState();

                    if (stabilityAnalyzer.CheckForStableState(currentSimulation))
                    {
                        Console.WriteLine($"\nСистема стабилизировалась на поколении: {generationsToStableState}");

                        if (stabilityDataPath != null)
                        {
                            stabilityAnalyzer.SaveStabilityData(generationsToStableState, stabilityDataPath);
                        }

                        if (analyzeClusters)
                        {
                            AnalyzeClusterPatterns();
                        }
                        break;
                    }

                    currentSimulation.NextGeneration();
                    currentGeneration++;
                    generationsToStableState++;
                    Thread.Sleep(config.UpdateDelay);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                    Console.ReadKey();
                    break;
                }
            }
        }

        private static void ProcessUserInput()
        {
            var key = Console.ReadKey(true).Key;
            switch (key)
            {
                case ConsoleKey.S:
                    currentSimulation.ExportState("board.txt");
                    Console.WriteLine("\nСостояние сохранено");
                    break;
                case ConsoleKey.L:
                    currentSimulation.ImportState("board.txt");
                    Console.WriteLine("\nСостояние загружено");
                    break;
                case ConsoleKey.Escape:
                    Environment.Exit(0);
                    break;
            }
        }

        private static void DisplayCurrentState()
        {
            Console.Clear();
            for (int y = 0; y < currentSimulation.Height; y++)
            {
                for (int x = 0; x < currentSimulation.Width; x++)
                {
                    Console.Write(currentSimulation.GetCellState(x, y) ? '■' : ' ');
                }
                Console.WriteLine();
            }
            Console.WriteLine($"Поколение: {currentGeneration}");
        }

        private static void AnalyzeClusterPatterns()
        {
            var clusters = patternRecognizer.DetectClusters(currentSimulation);
            Console.WriteLine($"\nНайдено кластеров: {clusters.Count}");

            foreach (var cluster in clusters.OrderByDescending(c => c.Count))
            {
                Console.WriteLine($"{patternRecognizer.IdentifyPattern(cluster)} (размер: {cluster.Count})");
            }
        }

        private static void AnalyzePopulationDynamics(string dataFilePath, string plotFilePath)
        {
            double cellDensity = 0.6;
            int maxGenerations = 600;

            File.WriteAllText(dataFilePath, "Поколение Количество_клеток\n");
            var simulation = new GameOfLife(50, 20, 1, cellDensity);

            for (int gen = 0; gen < maxGenerations && !stabilityAnalyzer.CheckForStableState(simulation); gen++)
            {
                File.AppendAllText(dataFilePath, $"{gen} {simulation.CountLiveCells()}\n");
                simulation.NextGeneration();
            }

            GeneratePopulationPlot(dataFilePath, plotFilePath);
            Console.WriteLine($"График сохранен в: {plotFilePath}");
        }

        
    }
}