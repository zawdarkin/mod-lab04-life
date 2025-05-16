using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CellularAutomata.Tests
{
    [TestClass]
    public class UnitTests
    {
        // Test 1: Cell stays alive with 2 or 3 neighbors
        [TestMethod]
        public void Cell_StaysAlive_With2or3Neighbors()
        {
            var cell = new Cell { IsAlive = true };
            var neighbors = Enumerable.Range(0, 8).Select(_ => new Cell()).ToList();
            
            // Test with 2 neighbors
            neighbors[0].IsAlive = true;
            neighbors[1].IsAlive = true;
            cell.Neighbors.AddRange(neighbors);
            cell.CalculateNextState();
            Assert.IsTrue(cell.NextState);

            // Test with 3 neighbors
            neighbors[2].IsAlive = true;
            cell.CalculateNextState();
            Assert.IsTrue(cell.NextState);
        }

        // Test 2: Cell dies with fewer than 2 neighbors
        [TestMethod]
        public void Cell_Dies_WithFewerThan2Neighbors()
        {
            var cell = new Cell { IsAlive = true };
            var neighbors = Enumerable.Range(0, 8).Select(_ => new Cell()).ToList();
            
            // Test with 1 neighbor
            neighbors[0].IsAlive = true;
            cell.Neighbors.AddRange(neighbors);
            cell.CalculateNextState();
            Assert.IsFalse(cell.NextState);

            // Test with 0 neighbors
            neighbors[0].IsAlive = false;
            cell.CalculateNextState();
            Assert.IsFalse(cell.NextState);
        }

        // Test 3: Cell dies with more than 3 neighbors
        [TestMethod]
        public void Cell_Dies_WithMoreThan3Neighbors()
        {
            var cell = new Cell { IsAlive = true };
            var neighbors = Enumerable.Range(0, 8).Select(_ => new Cell()).ToList();
            
            // Test with 4 neighbors
            for (int i = 0; i < 4; i++) neighbors[i].IsAlive = true;
            cell.Neighbors.AddRange(neighbors);
            cell.CalculateNextState();
            Assert.IsFalse(cell.NextState);
        }

        // Test 4: Dead cell becomes alive with exactly 3 neighbors
        [TestMethod]
        public void DeadCell_BecomesAlive_WithExactly3Neighbors()
        {
            var cell = new Cell { IsAlive = false };
            var neighbors = Enumerable.Range(0, 8).Select(_ => new Cell()).ToList();
            
            // Test with 3 neighbors
            for (int i = 0; i < 3; i++) neighbors[i].IsAlive = true;
            cell.Neighbors.AddRange(neighbors);
            cell.CalculateNextState();
            Assert.IsTrue(cell.NextState);
        }

        // Test 5: GameOfLife grid initialization
        [TestMethod]
        public void GameOfLife_InitializesGridCorrectly()
        {
            var game = new GameOfLife(10, 10, 1, 0.5);
            
            Assert.AreEqual(10, game.Width);
            Assert.AreEqual(10, game.Height);
            Assert.IsTrue(game.CountLiveCells() > 0); // Should have some live cells with density 0.5
        }

        // Test 6: GameOfLife next generation updates correctly
        [TestMethod]
        public void GameOfLife_NextGeneration_UpdatesCorrectly()
        {
            var game = new GameOfLife(3, 3, 1, 0);
            
            // Create a blinker pattern (vertical)
            game.GetCellState(1, 0).IsAlive = true;
            game.GetCellState(1, 1).IsAlive = true;
            game.GetCellState(1, 2).IsAlive = true;
            
            int initialCount = game.CountLiveCells();
            game.NextGeneration();
            
            Assert.AreEqual(initialCount, game.CountLiveCells()); // Count should remain the same
            Assert.IsTrue(game.GetCellState(0, 1)); // Should now be horizontal
            Assert.IsTrue(game.GetCellState(1, 1));
            Assert.IsTrue(game.GetCellState(2, 1));
        }

        // Test 7: StabilityAnalyzer detects stable state
        [TestMethod]
        public void StabilityAnalyzer_DetectsStableState()
        {
            var analyzer = new StabilityAnalyzer();
            var game = new GameOfLife(3, 3, 1, 0);
            
            // Create a block pattern (stable)
            game.GetCellState(0, 0).IsAlive = true;
            game.GetCellState(0, 1).IsAlive = true;
            game.GetCellState(1, 0).IsAlive = true;
            game.GetCellState(1, 1).IsAlive = true;
            
            for (int i = 0; i < 5; i++)
            {
                bool isStable = analyzer.CheckForStableState(game);
                if (i < 4) Assert.IsFalse(isStable);
                else Assert.IsTrue(isStable);
            }
        }

        // Test 8: PatternRecognizer detects known patterns
        [TestMethod]
        public void PatternRecognizer_DetectsKnownPatterns()
        {
            var recognizer = new PatternRecognizer();
            var game = new GameOfLife(4, 3, 1, 0);
            
            // Create a block pattern
            game.GetCellState(0, 0).IsAlive = true;
            game.GetCellState(0, 1).IsAlive = true;
            game.GetCellState(1, 0).IsAlive = true;
            game.GetCellState(1, 1).IsAlive = true;
            
            var clusters = recognizer.DetectClusters(game);
            var pattern = recognizer.IdentifyPattern(clusters.First());
            
            Assert.AreEqual("block", pattern.ToLower());
        }

        // Test 9: GameOfLife imports state correctly
        [TestMethod]
        public void GameOfLife_ImportsStateCorrectly()
        {
            string testFile = "test_import.txt";
            File.WriteAllText(testFile, "3 3\n111\n000\n111");
            
            var game = new GameOfLife(3, 3, 1, 0);
            game.ImportState(testFile);
            
            Assert.IsTrue(game.GetCellState(0, 0));
            Assert.IsTrue(game.GetCellState(1, 0));
            Assert.IsTrue(game.GetCellState(2, 0));
            Assert.IsFalse(game.GetCellState(0, 1));
            Assert.IsTrue(game.GetCellState(0, 2));
            
            File.Delete(testFile);
        }

        // Test 10: GameOfLife exports state correctly
        [TestMethod]
        public void GameOfLife_ExportsStateCorrectly()
        {
            string testFile = "test_export.txt";
            var game = new GameOfLife(2, 2, 1, 0);
            
            game.GetCellState(0, 0).IsAlive = true;
            game.GetCellState(1, 1).IsAlive = true;
            game.ExportState(testFile);
            
            string[] lines = File.ReadAllLines(testFile);
            Assert.AreEqual("2 2 1", lines[0]);
            Assert.AreEqual("10", lines[1]);
            Assert.AreEqual("01", lines[2]);
            
            File.Delete(testFile);
        }

        // Test 11: SimulationConfig loads from JSON correctly
        [TestMethod]
        public void SimulationConfig_LoadsFromJsonCorrectly()
        {
            string testFile = "test_config.json";
            File.WriteAllText(testFile, "{\"GridWidth\":30,\"GridHeight\":20,\"CellDimension\":2,\"InitialCellDensity\":0.3,\"UpdateDelay\":100}");
            
            string jsonConfig = File.ReadAllText(testFile);
            var config = JsonSerializer.Deserialize<SimulationConfig>(jsonConfig);
            
            Assert.AreEqual(30, config.GridWidth);
            Assert.AreEqual(20, config.GridHeight);
            Assert.AreEqual(2, config.CellDimension);
            Assert.AreEqual(0.3, config.InitialCellDensity);
            Assert.AreEqual(100, config.UpdateDelay);
            
            File.Delete(testFile);
        }

        // Test 12: GameOfLife counts live cells correctly
        [TestMethod]
        public void GameOfLife_CountsLiveCellsCorrectly()
        {
            var game = new GameOfLife(3, 3, 1, 0);
            
            game.GetCellState(0, 0).IsAlive = true;
            game.GetCellState(1, 1).IsAlive = true;
            game.GetCellState(2, 2).IsAlive = true;
            
            Assert.AreEqual(3, game.CountLiveCells());
        }

        // Test 13: PatternRecognizer normalizes cluster coordinates
        [TestMethod]
        public void PatternRecognizer_NormalizesClusterCoordinates()
        {
            var recognizer = new PatternRecognizer();
            var cluster = new HashSet<(int x, int y)> { (5, 10), (6, 10), (5, 11) };
            
            var normalized = recognizer.NormalizeClusterCoordinates(cluster);
            
            Assert.IsTrue(normalized.Contains((0, 0)));
            Assert.IsTrue(normalized.Contains((1, 0)));
            Assert.IsTrue(normalized.Contains((0, 1)));
        }

        // Test 14: StabilityAnalyzer saves data correctly
        [TestMethod]
        public void StabilityAnalyzer_SavesDataCorrectly()
        {
            string testFile = "test_stability.txt";
            var analyzer = new StabilityAnalyzer();
            
            analyzer.SaveStabilityData(42, testFile);
            string[] lines = File.ReadAllLines(testFile);
            
            Assert.IsTrue(lines[0].Contains("42"));
            Assert.IsTrue(lines[1].Contains("Среднее поколений до стабильности"));
            
            File.Delete(testFile);
        }

        // Test 15: GameOfLife loads patterns correctly
        [TestMethod]
        public void GameOfLife_LoadsPatternsCorrectly()
        {
            string testFile = "test_pattern.txt";
            File.WriteAllText(testFile, "010\n111\n010"); // Cross pattern
            
            var game = new GameOfLife(3, 3, 1, 0);
            game.LoadPattern(testFile);
            
            Assert.IsFalse(game.GetCellState(0, 0));
            Assert.IsTrue(game.GetCellState(1, 0));
            Assert.IsFalse(game.GetCellState(2, 0));
            Assert.IsTrue(game.GetCellState(0, 1));
            Assert.IsTrue(game.GetCellState(1, 1));
            Assert.IsTrue(game.GetCellState(2, 1));
            Assert.IsFalse(game.GetCellState(0, 2));
            Assert.IsTrue(game.GetCellState(1, 2));
            Assert.IsFalse(game.GetCellState(2, 2));
            
            File.Delete(testFile);
        }

        // Test 16: PatternRecognizer detects connected cells correctly
        [TestMethod]
        public void PatternRecognizer_DetectsConnectedCells()
        {
            var recognizer = new PatternRecognizer();
            var game = new GameOfLife(5, 5, 1, 0);
            
            // Create two separate clusters
            game.GetCellState(1, 1).IsAlive = true;
            game.GetCellState(1, 2).IsAlive = true;
            game.GetCellState(4, 4).IsAlive = true;
            game.GetCellState(4, 3).IsAlive = true;
            
            var clusters = recognizer.DetectClusters(game);
            Assert.AreEqual(2, clusters.Count);
        }

        // Test 17: GameOfLife handles toroidal world correctly
        [TestMethod]
        public void GameOfLife_HandlesToroidalWorld()
        {
            var game = new GameOfLife(3, 3, 1, 0);
            
            // Single cell in top-left corner should have neighbors including bottom-right
            var cell = game.GetCellState(0, 0);
            Assert.AreEqual(8, cell.Neighbors.Count);
            Assert.IsTrue(cell.Neighbors.Contains(game.GetCellState(2, 2))); // Wrapped neighbor
        }
    }
}
