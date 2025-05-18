using Microsoft.VisualStudio.TestTools.UnitTesting;
using CellularAutomata;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CellularAutomata.Tests
{
    [TestClass]
    public class ComprehensiveCellTests
    {
        [TestMethod]
        public void NewCell_HasNoNeighbors()
        {
            var cell = new Cell();
            Assert.AreEqual(0, cell.Neighbors.Count);
        }

        [TestMethod]
        public void Cell_CanToggleState()
        {
            var cell = new Cell();
            cell.IsAlive = true;
            Assert.IsTrue(cell.IsAlive);
            cell.IsAlive = false;
            Assert.IsFalse(cell.IsAlive);
        }

        [TestMethod]
        public void DeadCell_WithTwoNeighbors_StaysDead()
        {
            var cell = new Cell { IsAlive = false };
            cell.Neighbors.Add(new Cell { IsAlive = true });
            cell.Neighbors.Add(new Cell { IsAlive = true });
            
            cell.CalculateNextState();
            Assert.IsFalse(cell.NextState);
        }

        [TestMethod]
        public void LiveCell_WithOneNeighbor_Dies()
        {
            var cell = new Cell { IsAlive = true };
            cell.Neighbors.Add(new Cell { IsAlive = true });
            
            cell.CalculateNextState();
            Assert.IsFalse(cell.NextState);
        }

        [TestMethod]
        public void LiveCell_WithThreeNeighbors_Lives()
        {
            var cell = new Cell { IsAlive = true };
            for (int i = 0; i < 3; i++)
                cell.Neighbors.Add(new Cell { IsAlive = true });
            
            cell.CalculateNextState();
            Assert.IsTrue(cell.NextState);
        }
    }

    [TestClass]
    public class GameOfLifeSimulationTests
    {
        [TestMethod]
        public void GridInitialization_CreatesCorrectDimensions()
        {
            var game = new GameOfLife(200, 100, 5);
            Assert.AreEqual(40, game.Width);
            Assert.AreEqual(20, game.Height);
        }

        [TestMethod]
        public void Randomize_CreatesApproximatelyCorrectDensity()
        {
            var game = new GameOfLife(100, 100, 1);
            game.Randomize(0.25);
            int liveCells = game.CountLiveCells();
            double actualDensity = liveCells / (double)(game.Width * game.Height);
            
            Assert.IsTrue(actualDensity > 0.2 && actualDensity < 0.3);
        }

        [TestMethod]
        public void ToroidalGrid_ConnectsEdgesCorrectly()
        {
            var game = new GameOfLife(3, 3, 1);
            var topLeftCell = game.GetCellState(0, 0);
            var bottomRightCell = game.GetCellState(2, 2);
            
            // Should be neighbors in a toroidal grid
            Assert.IsTrue(game.GetCellState(0, 0).Neighbors.Contains(game.GetCellState(2, 2)));
        }

        [TestMethod]
        public void ImportState_HandlesSmallerGrid()
        {
            var game = new GameOfLife(10, 10, 1);
            string testFile = Path.GetTempFileName();
            File.WriteAllText(testFile, "2 2\n11\n00");
            
            game.ImportState(testFile);
            Assert.IsTrue(game.GetCellState(0, 0));
            Assert.IsTrue(game.GetCellState(1, 0));
            Assert.IsFalse(game.GetCellState(0, 1));
            
            File.Delete(testFile);
        }

        [TestMethod]
        public void ExportState_CreatesValidFile()
        {
            var game = new GameOfLife(2, 2, 1);
            game.Randomize(1.0);
            string testFile = Path.GetTempFileName();
            
            game.ExportState(testFile);
            var lines = File.ReadAllLines(testFile);
            Assert.AreEqual(3, lines.Length); // Header + 2 rows
            
            File.Delete(testFile);
        }
    }

    [TestClass]
    public class PatternRecognitionTests
    {
        [TestMethod]
        public void DetectClusters_FindsNoClustersInEmptyGrid()
        {
            var game = new GameOfLife(10, 10, 1);
            var recognizer = new PatternRecognizer();
            
            var clusters = recognizer.DetectClusters(game);
            Assert.AreEqual(0, clusters.Count);
        }

        [TestMethod]
        public void NormalizeCluster_ShiftsToOrigin()
        {
            var cluster = new HashSet<(int, int)> { (5, 3), (6, 3), (6, 4) };
            var recognizer = new PatternRecognizer();
            
            var normalized = recognizer.NormalizeClusterCoordinates(cluster);
            Assert.IsTrue(normalized.Contains((0, 0)));
            Assert.IsTrue(normalized.Contains((1, 0)));
            Assert.IsTrue(normalized.Contains((1, 1)));
        }

        [TestMethod]
        public void RotateCluster_ProducesCorrectRotations()
        {
            var cluster = new HashSet<(int, int)> { (0, 0), (1, 0), (2, 0) }; // Horizontal line
            var recognizer = new PatternRecognizer();
            
            var rotated90 = recognizer.RotateCluster(cluster, 1);
            Assert.IsTrue(rotated90.Contains((0, 0)));
            Assert.IsTrue(rotated90.Contains((0, 1)));
            Assert.IsTrue(rotated90.Contains((0, 2)));
        }
    }

    [TestClass]
    public class StabilityAnalysisTests
    {
        [TestMethod]
        public void StabilityCheck_RequiresMinimumGenerations()
        {
            var game = new GameOfLife(10, 10, 1);
            var analyzer = new StabilityAnalyzer();
            
            Assert.IsFalse(analyzer.CheckForStableState(game));
            for (int i = 0; i < 4; i++)
                analyzer.CheckForStableState(game);
            Assert.IsFalse(analyzer.CheckForStableState(game));
            
            analyzer.CheckForStableState(game);
            Assert.IsTrue(analyzer.CheckForStableState(game));
        }

        [TestMethod]
        public void SaveStabilityData_CreatesFileWithAverage()
        {
            var analyzer = new StabilityAnalyzer();
            string testFile = Path.GetTempFileName();
            
            analyzer.SaveStabilityData(10, testFile);
            analyzer.SaveStabilityData(20, testFile);
            
            var lines = File.ReadAllLines(testFile);
            Assert.IsTrue(lines.Last().Contains("Среднее поколений до стабильности: 15"));
            
            File.Delete(testFile);
        }
    }

    [TestClass]
    public class SimulationConfigTests
    {
        [TestMethod]
        public void Config_LoadsDefaultValues()
        {
            var config = new SimulationConfig();
            Assert.AreEqual(50, config.GridWidth);
            Assert.AreEqual(20, config.GridHeight);
            Assert.AreEqual(1, config.CellDimension);
            Assert.AreEqual(0.5, config.InitialCellDensity);
        }

        [TestMethod]
        public void Config_SerializationRoundTrip()
        {
            var original = new SimulationConfig {
                GridWidth = 100,
                GridHeight = 50,
                CellDimension = 2,
                InitialCellDensity = 0.3,
                UpdateDelay = 5
            };
            
            string json = JsonSerializer.Serialize(original);
            var loaded = JsonSerializer.Deserialize<SimulationConfig>(json);
            
            Assert.AreEqual(original.GridWidth, loaded.GridWidth);
            Assert.AreEqual(original.InitialCellDensity, loaded.InitialCellDensity);
        }
    }

    [TestClass]
    public class IntegrationTests
    {
        [TestMethod]
        public void FullSimulation_ReachesStableState()
        {
            var game = new GameOfLife(20, 20, 1);
            game.Randomize(0.2);
            var analyzer = new StabilityAnalyzer();
            
            bool stable = false;
            for (int i = 0; i < 100; i++)
            {
                game.NextGeneration();
                if (analyzer.CheckForStableState(game))
                {
                    stable = true;
                    break;
                }
            }
            
            Assert.IsTrue(stable);
        }

        [TestMethod]
        public void PatternRecognition_IdentifiesCommonPatterns()
        {
            var game = new GameOfLife(5, 5, 1);
            game.ImportStateFromArray(new bool[,] {
                { false, false, false, false, false },
                { false, true, true, true, false },
                { false, false, false, false, false },
                { false, false, false, false, false },
                { false, false, false, false, false }
            });
            
            var recognizer = new PatternRecognizer();
            var clusters = recognizer.DetectClusters(game);
            var patternName = recognizer.IdentifyPattern(clusters[0], "patterns");
            
            Assert.IsTrue(patternName.Contains("blinker"));
        }
    }
}
