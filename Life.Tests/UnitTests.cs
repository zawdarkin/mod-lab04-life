using Microsoft.VisualStudio.TestTools.UnitTesting;
using LifeSimulation;
using System.Text.Json;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace LifeSimulation.Tests
{
    [TestClass]
    public class LifeCellTests
    {
        [TestMethod]
        public void LiveCellWithOneNeighbor_Dies()
        {
            var cell = new LifeCell { Alive = true };
            cell.AdjacentCells.AddRange(Enumerable.Repeat(new LifeCell { Alive = true }, 1));
            cell.CalculateNextState();
            Assert.IsFalse(cell.NextState);
        }

        [TestMethod]
        public void LiveCellWithTwoNeighbors_StaysAlive()
        {
            var cell = new LifeCell { Alive = true };
            cell.AdjacentCells.AddRange(Enumerable.Repeat(new LifeCell { Alive = true }, 2));
            cell.CalculateNextState();
            Assert.IsTrue(cell.NextState);
        }

        [TestMethod]
        public void LiveCellWithFourNeighbors_Dies()
        {
            var cell = new LifeCell { Alive = true };
            cell.AdjacentCells.AddRange(Enumerable.Repeat(new LifeCell { Alive = true }, 4));
            cell.CalculateNextState();
            Assert.IsFalse(cell.NextState);
        }

        [TestMethod]
        public void DeadCellWithThreeNeighbors_BecomesAlive()
        {
            var cell = new LifeCell { Alive = false };
            cell.AdjacentCells.AddRange(Enumerable.Repeat(new LifeCell { Alive = true }, 3));
            cell.CalculateNextState();
            Assert.IsTrue(cell.NextState);
        }
    }

    [TestClass]
    public class LifeGridTests
    {
        string testDir = Directory.GetCurrentDirectory();

        [TestMethod]
        public void GridConstructor_CalculatesCorrectDimensions()
        {
            var grid = new LifeGrid(100, 50, 2, 0.1);
            Assert.AreEqual(50, grid.Width);
            Assert.AreEqual(25, grid.Height);
        }

        [TestMethod]
        public void CenterCell_HasEightNeighbors()
        {
            var grid = new LifeGrid(3, 3, 1, 0.1);
            var centerCell = grid.GetCell(1, 1);
            Assert.AreEqual(8, centerCell.AdjacentCells.Count);
        }

        [TestMethod]
        public void CornerCell_HasCorrectWrappedNeighbors()
        {
            var grid = new LifeGrid(3, 3, 1, 0.1);
            var cornerCell = grid.GetCell(0, 0);
            Assert.IsTrue(cornerCell.AdjacentCells.Contains(grid.GetCell(2, 2)));
            Assert.IsTrue(cornerCell.AdjacentCells.Contains(grid.GetCell(0, 2)));
            Assert.IsTrue(cornerCell.AdjacentCells.Contains(grid.GetCell(2, 0)));
        }

        [TestMethod]
        public void GridInitialization_RespectsDensityParameter()
        {
            var grid = new LifeGrid(100, 100, 1, 0.3);
            double aliveRatio = 0;
            for (int x = 0; x < grid.Width; x++)
                for (int y = 0; y < grid.Height; y++)
                    if (grid.GetCell(x, y).Alive) aliveRatio++;
            
            aliveRatio /= (grid.Width * grid.Height);
            Assert.IsTrue(aliveRatio >= 0.25 && aliveRatio <= 0.35);
        }

        [TestMethod]
        public void LoadPattern_NonExistentFile_ThrowsException()
        {
            var grid = new LifeGrid(100, 100, 1, 0.1);
            var exception = Assert.ThrowsException<Exception>(() => grid.LoadPattern("nonexistent_pattern.txt"));
            Assert.AreEqual("File nonexistent_pattern.txt not found", exception.Message);
        }

        [TestMethod]
        public void ExportState_NonExistentDirectory_ThrowsException()
        {
            var grid = new LifeGrid(100, 100, 1, 0.1);
            var exception = Assert.ThrowsException<DirectoryNotFoundException>(() => grid.ExportState("nonexistent_directory/board.txt"));
            Assert.IsTrue(exception.Message.Contains("Could not find a part of the path"));
        }

        [TestMethod]
        public void ImportState_NonExistentFile_ThrowsException()
        {
            var grid = new LifeGrid(100, 100, 1, 0.1);
            var exception = Assert.ThrowsException<FileNotFoundException>(() => grid.ImportState("nonexistent_board.txt"));
            Assert.IsTrue(exception.Message.Contains("Could not find file"));
        }

        [TestMethod]
        public void LoadPattern_GliderPattern_LoadsCorrectly()
        {
            string patternsDir = Path.Combine(testDir, "..", "..", "..", "..", "Life", "patterns");
            string patternPath = Path.Combine(patternsDir, "glider.txt");

            var grid = new LifeGrid(10, 10, 1, 0);
            grid.LoadPattern(patternPath);

            Assert.IsTrue(grid.GetCell(1, 0).Alive);
            Assert.IsTrue(grid.GetCell(2, 1).Alive);
            Assert.IsTrue(grid.GetCell(0, 2).Alive);
            Assert.IsTrue(grid.GetCell(1, 2).Alive);
            Assert.IsTrue(grid.GetCell(2, 2).Alive);
        }

        [TestMethod]
        public void ImportExportState_RoundTrip_Succeeds()
        {
            string projectDir = Path.Combine(testDir, "..", "..", "..", "..", "Life");
            string boardPath = Path.Combine(projectDir, "board.txt");

            var grid = new LifeGrid(50, 20, 1, 0.1);

            grid.ImportState(boardPath);
            grid.ExportState(boardPath);
            Assert.IsTrue(grid.Width > 0 && grid.Height > 0);
        }
    }

    [TestClass]
    public class AnalyzerTests
    {
        string testDir = Directory.GetCurrentDirectory();

        [TestMethod]
        public void FindClusters_TwoAdjacentCells_ReturnsOneCluster()
        {
            var grid = new LifeGrid(5, 5, 1, 0);
            grid.GetCell(1, 1).Alive = true;
            grid.GetCell(1, 2).Alive = true;

            var clusters = new ClusterAnalyzer().FindClusters(grid);
            Assert.AreEqual(1, clusters.Count);
            Assert.AreEqual(2, clusters[0].Count);
        }

        [TestMethod]
        public void FindClusters_TwoDistantCells_ReturnsTwoClusters()
        {
            var grid = new LifeGrid(5, 5, 1, 0);
            grid.GetCell(1, 1).Alive = true;
            grid.GetCell(4, 4).Alive = true;

            var clusters = new ClusterAnalyzer().FindClusters(grid);
            Assert.AreEqual(2, clusters.Count);
            Assert.AreEqual(1, clusters[0].Count);
            Assert.AreEqual(1, clusters[1].Count);
        }

        [TestMethod]
        public void ClassifyCluster_ThreeVerticalCells_IdentifiesAsBlinker()
        {
            string patternsDir = Path.Combine(testDir, "..", "..", "..", "..", "Life", "patterns");

            var cluster = new HashSet<(int, int)> { (1, 0), (1, 1), (1, 2) };
            string type = new ClusterAnalyzer().ClassifyCluster(cluster, patternsDir);
            Assert.AreEqual("blinker", type);
        }
    }

    [TestClass]
    public class SettingsTests
    {
        string testDir = Directory.GetCurrentDirectory();

        [TestMethod]
        public void Settings_Deserialization_MatchesExpectedValues()
        {
            var expectedSettings = new SimulationSettings
            {
                Width = 50,
                Height = 20,
                CellSize = 1,
                InitialDensity = 0.5,
                UpdateInterval = 3
            };

            string projectDir = Path.Combine(testDir, "..", "..", "..", "..", "Life");
            string settingsPath = Path.Combine(projectDir, "config.json");
            string json = File.ReadAllText(settingsPath);
            var actualSettings = JsonSerializer.Deserialize<SimulationSettings>(json);

            Assert.AreEqual(expectedSettings.Width, actualSettings?.Width);
            Assert.AreEqual(expectedSettings.Height, actualSettings?.Height);
            Assert.AreEqual(expectedSettings.CellSize, actualSettings?.CellSize);
            Assert.AreEqual(expectedSettings.InitialDensity, actualSettings?.InitialDensity);
            Assert.AreEqual(expectedSettings.UpdateInterval, actualSettings?.UpdateInterval);
        }
    }

    [TestClass]
    public class IterativeTests
    {
        string testDir = Directory.GetCurrentDirectory();

        [TestMethod]
        public void Glider_AfterFourGenerations_MaintainsCellCount()
        {
            string patternsDir = Path.Combine(testDir, "..", "..", "..", "..", "Life", "patterns");
            var grid = new LifeGrid(10, 10, 1, 0);
            grid.LoadPattern(Path.Combine(patternsDir, "glider.txt"));

            int initialCount = 0;
            for (int x = 0; x < grid.Width; x++)
                for (int y = 0; y < grid.Height; y++)
                    if (grid.GetCell(x, y).Alive) initialCount++;

            for (int i = 0; i < 4; i++)
                grid.AdvanceGeneration();

            int finalCount = 0;
            for (int x = 0; x < grid.Width; x++)
                for (int y = 0; y < grid.Height; y++)
                    if (grid.GetCell(x, y).Alive) finalCount++;

            Assert.AreEqual(initialCount, finalCount);
        }

        [TestMethod]
        public void Block_AfterOneGeneration_RemainsUnchanged()
        {
            string patternsDir = Path.Combine(testDir, "..", "..", "..", "..", "Life", "patterns");
            var grid = new LifeGrid(4, 4, 1, 0);
            grid.LoadPattern(Path.Combine(patternsDir, "block.txt"));

            var before = new List<(int, int)>();
            for (int x = 0; x < grid.Width; x++)
                for (int y = 0; y < grid.Height; y++)
                    if (grid.GetCell(x, y).Alive) before.Add((x, y));

            grid.AdvanceGeneration();

            var after = new List<(int, int)>();
            for (int x = 0; x < grid.Width; x++)
                for (int y = 0; y < grid.Height; y++)
                    if (grid.GetCell(x, y).Alive) after.Add((x, y));

            CollectionAssert.AreEquivalent(before, after);
        }
    }
}
