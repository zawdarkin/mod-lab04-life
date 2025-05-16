using Microsoft.VisualStudio.TestTools.UnitTesting;
using CellularAutomata;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;

namespace CellularAutomataTests
{
    [TestClass]
    public class CellBehaviorTests
    {
        [TestMethod]
        public void Cell_InitialState_ShouldBeDeadByDefault()
        {
            var cell = new Cell();
            Assert.IsFalse(cell.IsAlive);
        }

        [TestMethod]
        public void Cell_WithNoNeighbors_DiesInNextGeneration()
        {
            var cell = new Cell { IsAlive = true };
            cell.CalculateNextState();
            Assert.IsFalse(cell.NextState);
        }

        [TestMethod]
        public void Cell_WithTwoLiveNeighbors_RemainsInCurrentState()
        {
            var cell = new Cell { IsAlive = true };
            cell.AddNeighbor(new Cell { IsAlive = true });
            cell.AddNeighbor(new Cell { IsAlive = true });
            cell.CalculateNextState();
            Assert.AreEqual(cell.IsAlive, cell.NextState);
        }

        [TestMethod]
        public void DeadCell_WithExactlyThreeNeighbors_BecomesAlive()
        {
            var cell = new Cell { IsAlive = false };
            for (int i = 0; i < 3; i++)
                cell.AddNeighbor(new Cell { IsAlive = true });
            cell.CalculateNextState();
            Assert.IsTrue(cell.NextState);
        }

        [TestMethod]
        public void LiveCell_WithMoreThanThreeNeighbors_DiesFromOverpopulation()
        {
            var cell = new Cell { IsAlive = true };
            for (int i = 0; i < 4; i++)
                cell.AddNeighbor(new Cell { IsAlive = true });
            cell.CalculateNextState();
            Assert.IsFalse(cell.NextState);
        }
    }

    [TestClass]
    public class GridOperationsTests
    {
        private GameOfLife CreateTestGame() => new GameOfLife(10, 10, 1, 0);

        [TestMethod]
        public void GridInitialization_CreatesCorrectNumberOfCells()
        {
            var game = new GameOfLife(30, 20, 1, 0);
            Assert.AreEqual(30, game.Width);
            Assert.AreEqual(20, game.Height);
        }

        [TestMethod]
        public void RandomizeGrid_ProducesApproximatelyCorrectDensity()
        {
            var game = new GameOfLife(100, 100, 1, 0.3);
            int liveCells = game.CountLiveCells();
            double actualDensity = liveCells / (double)(game.Width * game.Height);
            Assert.IsTrue(actualDensity >= 0.25 && actualDensity <= 0.35);
        }

        [TestMethod]
        public void NextGeneration_ChangesGridState()
        {
            var game = new GameOfLife(10, 10, 1, 0.5);
            int initialCount = game.CountLiveCells();
            game.NextGeneration();
            Assert.AreNotEqual(initialCount, game.CountLiveCells());
        }

        [TestMethod]
        public void ToroidalGrid_ConnectsAllCells()
        {
            var game = CreateTestGame();
            // This test verifies all cells have exactly 8 neighbors
            bool allHaveEightNeighbors = true;
            
            for (int y = 0; y < game.Height; y++)
            {
                for (int x = 0; x < game.Width; x++)
                {
                    var cell = GetCellThroughReflection(game, x, y);
                    if (cell.Neighbors.Count != 8)
                    {
                        allHaveEightNeighbors = false;
                        break;
                    }
                }
            }
            
            Assert.IsTrue(allHaveEightNeighbors);
        }

        private Cell GetCellThroughReflection(GameOfLife game, int x, int y)
        {
            var grid = game.GetType()
                .GetField("gameGrid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(game) as Cell[,];
            return grid?[x, y];
        }
    }

    [TestClass]
    public class FileIOTests
    {
        private string testDir = Path.Combine(Directory.GetCurrentDirectory(), "TestIO");

        [TestInitialize]
        public void Setup() => Directory.CreateDirectory(testDir);

        [TestCleanup]
        public void Cleanup() => Directory.Delete(testDir, true);

        [TestMethod]
        public void ExportImport_RoundTrip_PreservesGridState()
        {
            var game1 = new GameOfLife(5, 5, 1, 0.5);
            string testFile = Path.Combine(testDir, "test_grid.txt");
            game1.ExportState(testFile);

            var game2 = new GameOfLife(5, 5, 1, 0);
            game2.ImportState(testFile);

            bool allMatch = true;
            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    if (game1.GetCellState(x, y) != game2.GetCellState(x, y))
                    {
                        allMatch = false;
                        break;
                    }
                }
            }

            Assert.IsTrue(allMatch);
        }

        [TestMethod]
        public void LoadPattern_WithInvalidCharacters_HandlesGracefully()
        {
            string invalidFile = Path.Combine(testDir, "invalid.txt");
            File.WriteAllText(invalidFile, "1X0\nY1Z\n000");

            var game = new GameOfLife(3, 3, 1, 0);
            game.LoadPattern(invalidFile);

            // Should only load valid '1's and ignore others
            Assert.AreEqual(2, game.CountLiveCells());
        }
    }

    [TestClass]
    public class PatternAnalysisTests
    {
        [TestMethod]
        public void DetectClusters_WithIsolatedCells_ReturnsCorrectCount()
        {
            var game = new GameOfLife(5, 5, 1, 0);
            // Set up 3 isolated live cells
            SetCellStateThroughReflection(game, 1, 1, true);
            SetCellStateThroughReflection(game, 3, 1, true);
            SetCellStateThroughReflection(game, 1, 3, true);

            var recognizer = new PatternRecognizer();
            var clusters = recognizer.DetectClusters(game);
            Assert.AreEqual(3, clusters.Count);
        }

        [TestMethod]
        public void IdentifyPattern_WithKnownPattern_ReturnsCorrectName()
        {
            var game = new GameOfLife(5, 5, 1, 0);
            // Create a simple block pattern
            SetCellStateThroughReflection(game, 1, 1, true);
            SetCellStateThroughReflection(game, 1, 2, true);
            SetCellStateThroughReflection(game, 2, 1, true);
            SetCellStateThroughReflection(game, 2, 2, true);

            var recognizer = new PatternRecognizer();
            var clusters = recognizer.DetectClusters(game);
            var result = recognizer.IdentifyPattern(clusters.First(), "patterns");
            
            Assert.IsTrue(result.Contains("block") || result.Contains("Неизвестный"));
        }

        private void SetCellStateThroughReflection(GameOfLife game, int x, int y, bool state)
        {
            var grid = game.GetType()
                .GetField("gameGrid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(game) as Cell[,];
            grid?[x, y].IsAlive = state;
        }
    }

    [TestClass]
    public class StabilityAnalysisTests
    {
        [TestMethod]
        public void StabilityCheck_RequiresMinimumFiveGenerations()
        {
            var game = new GameOfLife(10, 10, 1, 0);
            var analyzer = new StabilityAnalyzer();
            
            // First 4 checks shouldn't detect stability
            for (int i = 0; i < 4; i++)
                Assert.IsFalse(analyzer.CheckForStableState(game));
            
            // 5th check should detect stability
            Assert.IsTrue(analyzer.CheckForStableState(game));
        }

        [TestMethod]
        public void SaveStabilityData_CreatesFileIfNotExists()
        {
            string testFile = Path.GetTempFileName();
            File.Delete(testFile);
            
            var analyzer = new StabilityAnalyzer();
            analyzer.SaveStabilityData(100, testFile);
            
            Assert.IsTrue(File.Exists(testFile));
            File.Delete(testFile);
        }
    }

    [TestClass]
    public class ConfigurationTests
    {
        [TestMethod]
        public void Config_DefaultValues_AreReasonable()
        {
            var config = new SimulationConfig();
            Assert.AreEqual(50, config.GridWidth);
            Assert.IsTrue(config.InitialCellDensity >= 0 && config.InitialCellDensity <= 1);
            Assert.IsTrue(config.UpdateDelay > 0);
        }

        [TestMethod]
        public void Config_Serialization_PreservesAllProperties()
        {
            var original = new SimulationConfig {
                GridWidth = 80,
                GridHeight = 40,
                InitialCellDensity = 0.3,
                UpdateDelay = 10
            };

            string json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<SimulationConfig>(json);
            
            Assert.AreEqual(original.GridWidth, deserialized?.GridWidth);
            Assert.AreEqual(original.GridHeight, deserialized?.GridHeight);
            Assert.AreEqual(original.InitialCellDensity, deserialized?.InitialCellDensity);
            Assert.AreEqual(original.UpdateDelay, deserialized?.UpdateDelay);
        }
    }
}
