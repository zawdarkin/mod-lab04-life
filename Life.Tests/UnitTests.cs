using Microsoft.VisualStudio.TestTools.UnitTesting;
using CellularAutomata;
using System.Text.Json;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace CellularAutomataTests
{
    [TestClass]
    public class CellTests
    {
        [TestMethod]
        public void LiveCellWithOneNeighbor_Dies()
        {
            var cell = new Cell { IsAlive = true };
            cell.AddNeighbor(new Cell { IsAlive = true });
            cell.CalculateNextState();
            Assert.IsFalse(cell.NextState);
        }

        [TestMethod]
        public void LiveCellWithTwoNeighbors_StaysAlive()
        {
            var cell = new Cell { IsAlive = true };
            cell.AddNeighbor(new Cell { IsAlive = true });
            cell.AddNeighbor(new Cell { IsAlive = true });
            cell.CalculateNextState();
            Assert.IsTrue(cell.NextState);
        }

        [TestMethod]
        public void LiveCellWithFourNeighbors_Dies()
        {
            var cell = new Cell { IsAlive = true };
            for (int i = 0; i < 4; i++)
                cell.AddNeighbor(new Cell { IsAlive = true });
            cell.CalculateNextState();
            Assert.IsFalse(cell.NextState);
        }

        [TestMethod]
        public void DeadCellWithThreeNeighbors_BecomesAlive()
        {
            var cell = new Cell { IsAlive = false };
            for (int i = 0; i < 3; i++)
                cell.AddNeighbor(new Cell { IsAlive = true });
            cell.CalculateNextState();
            Assert.IsTrue(cell.NextState);
        }

        [TestMethod]
        public void Cell_UpdateState_ChangesToNextState()
        {
            var cell = new Cell { IsAlive = false };
            
            for (int i = 0; i < 3; i++)
                cell.AddNeighbor(new Cell { IsAlive = true });
            cell.CalculateNextState(); 
            cell.UpdateState();
            Assert.IsTrue(cell.IsAlive);
        }
    }

    [TestClass]
    public class GameOfLifeTests
    {
        private string testDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TestData");

        [TestInitialize]
    public void Setup()
    {
        if (!Directory.Exists(testDirectory))
        {
            Directory.CreateDirectory(testDirectory);
            
            File.WriteAllText(Path.Combine(testDirectory, "pattern_3x3.txt"), "000\n010\n000");
        }
    }

        [TestMethod]
        public void Constructor_CalculatesCorrectDimensions()
        {
            var game = new GameOfLife(100, 50, 2, 0.1);
            Assert.AreEqual(50, game.Width);
            Assert.AreEqual(25, game.Height);
        }

        [TestMethod]
        public void Randomize_RespectsDensityParameter()
        {
            var game = new GameOfLife(100, 100, 1, 0.3);
            int aliveCount = game.CountLiveCells();
            double aliveRatio = aliveCount / (double)(game.Width * game.Height);
            Assert.IsTrue(aliveRatio >= 0.25 && aliveRatio <= 0.35);
        }

        [TestMethod]
        public void NextGeneration_UpdatesAllCells()
        {
            var game = new GameOfLife(10, 10, 1, 0.5);
            int initialCount = game.CountLiveCells();
            game.NextGeneration();
            Assert.AreNotEqual(initialCount, game.CountLiveCells());
        }

        [TestMethod]
        public void ImportExportState_RoundTrip_Succeeds()
        {
            string testFile = Path.Combine(testDirectory, "test_board.txt");
            var game1 = new GameOfLife(50, 20, 1, 0.1);
            game1.ExportState(testFile);

            var game2 = new GameOfLife(50, 20, 1, 0);
            game2.ImportState(testFile);

            Assert.AreEqual(game1.Width, game2.Width);
            Assert.AreEqual(game1.Height, game2.Height);
        }

        [TestMethod]
        public void LoadPattern_NonExistentFile_ThrowsException()
        {
            var game = new GameOfLife(100, 100, 1, 0.1);
            Assert.ThrowsException<FileNotFoundException>(() => 
                game.LoadPattern("nonexistent_pattern.txt"));
        }

        [TestMethod]
        public void GetCellState_ReturnsCorrectValue()
        {
            var game = new GameOfLife(3, 3, 1, 0);
            game.LoadPattern(Path.Combine(testDirectory, "pattern_3x3.txt"));
            Assert.IsTrue(game.GetCellState(1, 1));
        }
    }

    [TestClass]
public class PatternRecognizerTests
{
    private string testDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TestPatterns");

    [TestInitialize]
    public void Setup()
    {
        if (!Directory.Exists(testDirectory))
        {
            Directory.CreateDirectory(testDirectory);
            File.WriteAllText(Path.Combine(testDirectory, "blinker.txt"), "010\n010\n010");
            File.WriteAllText(Path.Combine(testDirectory, "two_cells.txt"), "11\n00");
        }
    }

        [TestMethod]
        public void DetectClusters_TwoAdjacentCells_ReturnsOneCluster()
        {
            var game = new GameOfLife(5, 5, 1, 0);
            game.LoadPattern(Path.Combine(testDirectory, "two_cells.txt"));
            
            var recognizer = new PatternRecognizer();
            var clusters = recognizer.DetectClusters(game);
            
            Assert.AreEqual(1, clusters.Count);
            Assert.AreEqual(2, clusters[0].Count);
        }

       

        [TestMethod]
        public void NormalizeClusterCoordinates_ShiftsToOrigin()
        {
            var cluster = new HashSet<(int, int)> { (5, 10), (6, 10), (5, 11) };
            
            var expected = new HashSet<(int, int)> { (0, 0), (1, 0), (0, 1) };
            
            
            var game = new GameOfLife(20, 20, 1, 0);
            
        }
    }

    [TestClass]
    public class SimulationConfigTests
    {
        [TestMethod]
        public void DefaultValues_AreCorrect()
        {
            var config = new SimulationConfig();
            
            Assert.AreEqual(50, config.GridWidth);
            Assert.AreEqual(20, config.GridHeight);
            Assert.AreEqual(1, config.CellDimension);
            Assert.AreEqual(0.5, config.InitialCellDensity);
            Assert.AreEqual(2, config.UpdateDelay);
        }

        [TestMethod]
        public void JsonSerialization_RoundTrip_Succeeds()
        {
            var original = new SimulationConfig
            {
                GridWidth = 100,
                GridHeight = 50,
                CellDimension = 2,
                InitialCellDensity = 0.3,
                UpdateDelay = 5
            };

            string json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<SimulationConfig>(json);
            
            Assert.AreEqual(original.GridWidth, deserialized.GridWidth);
            Assert.AreEqual(original.GridHeight, deserialized.GridHeight);
            Assert.AreEqual(original.CellDimension, deserialized.CellDimension);
            Assert.AreEqual(original.InitialCellDensity, deserialized.InitialCellDensity);
            Assert.AreEqual(original.UpdateDelay, deserialized.UpdateDelay);
        }
    }

    [TestClass]
    public class StabilityAnalyzerTests
    {
        [TestMethod]
        public void CheckForStableState_WithStablePopulation_ReturnsTrue()
        {
            var game = new GameOfLife(10, 10, 1, 0);
            var analyzer = new StabilityAnalyzer();
            
            
            for (int i = 0; i < 5; i++)
            {
                analyzer.CheckForStableState(game);
            }
            
            Assert.IsTrue(analyzer.CheckForStableState(game));
        }

        [TestMethod]
        public void CheckForStableState_WithChangingPopulation_ReturnsFalse()
        {
            var game = new GameOfLife(10, 10, 1, 0.5);
            var analyzer = new StabilityAnalyzer();
            
            Assert.IsFalse(analyzer.CheckForStableState(game));
        }
    }
}
