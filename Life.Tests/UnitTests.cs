
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CellularAutomata;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace CellularAutomata.Tests
{
    [TestClass]
    public class CellTests
    {
        [TestMethod]
        public void Cell_InitialState_ShouldBeDead()
        {
            var cell = new Cell();
            Assert.IsFalse(cell.IsAlive);
        }

        [TestMethod]
        public void Cell_AddNeighbor_IncreasesNeighborCount()
        {
            var cell1 = new Cell();
            var cell2 = new Cell();
            
            cell1.AddNeighbor(cell2);
            
            Assert.AreEqual(1, cell1.Neighbors.Count);
            Assert.IsTrue(cell1.Neighbors.Contains(cell2));
        }

        [TestMethod]
        public void DeadCell_WithExactlyThreeLiveNeighbors_BecomesAlive()
        {
            var cell = new Cell { IsAlive = false };
            for (int i = 0; i < 3; i++)
            {
                cell.AddNeighbor(new Cell { IsAlive = true });
            }
            
            cell.CalculateNextState();
            Assert.IsTrue(cell.NextState);
        }

        [TestMethod]
        public void LiveCell_WithTwoLiveNeighbors_StaysAlive()
        {
            var cell = new Cell { IsAlive = true };
            for (int i = 0; i < 2; i++)
            {
                cell.AddNeighbor(new Cell { IsAlive = true });
            }
            
            cell.CalculateNextState();
            Assert.IsTrue(cell.NextState);
        }

        [TestMethod]
        public void LiveCell_WithFourLiveNeighbors_Dies()
        {
            var cell = new Cell { IsAlive = true };
            for (int i = 0; i < 4; i++)
            {
                cell.AddNeighbor(new Cell { IsAlive = true });
            }
            
            cell.CalculateNextState();
            Assert.IsFalse(cell.NextState);
        }

        [TestMethod]
        public void UpdateState_ChangesIsAliveToNextState()
        {
            var cell = new Cell { IsAlive = false };
            cell.NextState = true;
            
            cell.UpdateState();
            Assert.IsTrue(cell.IsAlive);
        }
    }

    [TestClass]
    public class GameOfLifeTests
    {
        [TestMethod]
        public void GameOfLife_Constructor_SetsCorrectDimensions()
        {
            var game = new GameOfLife(100, 50, 2);
            
            Assert.AreEqual(50, game.Width);  // 100 / 2
            Assert.AreEqual(25, game.Height); // 50 / 2
        }

        [TestMethod]
        public void GetCellState_ReturnsCorrectState()
        {
            var game = new GameOfLife(10, 10, 1);
            game.Randomize(1.0); // All cells alive
            
            Assert.IsTrue(game.GetCellState(0, 0));
        }

        [TestMethod]
        public void ConnectNeighbors_CreatesEightNeighborsForCenterCell()
        {
            var game = new GameOfLife(3, 3, 1);
            var centerCell = game.gameGrid[1, 1];
            
            Assert.AreEqual(8, centerCell.Neighbors.Count);
        }

        [TestMethod]
        public void ConnectNeighbors_WrapsAroundEdges()
        {
            var game = new GameOfLife(3, 3, 1);
            var cornerCell = game.gameGrid[0, 0];
            
            // Should include opposite corner (2,2) due to wrapping
            Assert.IsTrue(cornerCell.Neighbors.Contains(game.gameGrid[2, 2]));
        }

        [TestMethod]
        public void Randomize_RespectsDensityParameter()
        {
            var game = new GameOfLife(100, 100, 1);
            game.Randomize(0.3);
            
            int aliveCount = 0;
            for (int x = 0; x < game.Width; x++)
                for (int y = 0; y < game.Height; y++)
                    if (game.GetCellState(x, y)) aliveCount++;
            
            double actualDensity = (double)aliveCount / (game.Width * game.Height);
            Assert.IsTrue(actualDensity > 0.25 && actualDensity < 0.35);
        }

        [TestMethod]
        public void NextGeneration_UpdatesAllCells()
        {
            var game = new GameOfLife(10, 10, 1);
            game.Randomize(0.5);
            var initialStates = new bool[10,10];
            
            for (int x = 0; x < 10; x++)
                for (int y = 0; y < 10; y++)
                    initialStates[x,y] = game.GetCellState(x, y);
            
            game.NextGeneration();
            
            bool anyChanged = false;
            for (int x = 0; x < 10; x++)
                for (int y = 0; y < 10; y++)
                    if (game.GetCellState(x, y) != initialStates[x,y])
                        anyChanged = true;
            
            Assert.IsTrue(anyChanged);
        }

        [TestMethod]
        public void ImportExport_RoundTrip_PreservesState()
        {
            var game1 = new GameOfLife(10, 10, 1);
            game1.Randomize(0.5);
            
            string tempFile = Path.GetTempFileName();
            game1.ExportState(tempFile);
            
            var game2 = new GameOfLife(10, 10, 1);
            game2.ImportState(tempFile);
            
            for (int x = 0; x < 10; x++)
                for (int y = 0; y < 10; y++)
                    Assert.AreEqual(game1.GetCellState(x, y), game2.GetCellState(x, y));
            
            File.Delete(tempFile);
        }

        [TestMethod]
        public void CountLiveCells_ReturnsCorrectCount()
        {
            var game = new GameOfLife(10, 10, 1);
            game.Randomize(0.0); // All dead
            
            // Manually set 3 cells to alive
            game.gameGrid[0, 0].IsAlive = true;
            game.gameGrid[1, 1].IsAlive = true;
            game.gameGrid[2, 2].IsAlive = true;
            
            Assert.AreEqual(3, game.CountLiveCells());
        }
    }

    [TestClass]
    public class PatternRecognizerTests
    {
        [TestMethod]
        public void DetectClusters_FindsSingleCluster()
        {
            var game = new GameOfLife(5, 5, 1);
            game.gameGrid[1, 1].IsAlive = true;
            game.gameGrid[1, 2].IsAlive = true;
            game.gameGrid[2, 1].IsAlive = true;
            
            var recognizer = new PatternRecognizer();
            var clusters = recognizer.DetectClusters(game);
            
            Assert.AreEqual(1, clusters.Count);
            Assert.AreEqual(3, clusters[0].Count);
        }

        [TestMethod]
        public void DetectClusters_FindsMultipleIsolatedClusters()
        {
            var game = new GameOfLife(5, 5, 1);
            game.gameGrid[0, 0].IsAlive = true;
            game.gameGrid[4, 4].IsAlive = true;
            
            var recognizer = new PatternRecognizer();
            var clusters = recognizer.DetectClusters(game);
            
            Assert.AreEqual(2, clusters.Count);
            Assert.AreEqual(1, clusters[0].Count);
            Assert.AreEqual(1, clusters[1].Count);
        }

        [TestMethod]
        public void NormalizeClusterCoordinates_ShiftsToOrigin()
        {
            var cluster = new HashSet<(int, int)> { (3, 5), (4, 5), (3, 6) };
            var recognizer = new PatternRecognizer();
            
            var normalized = recognizer.NormalizeClusterCoordinates(cluster);
            
            Assert.IsTrue(normalized.Contains((0, 0)));
            Assert.IsTrue(normalized.Contains((1, 0)));
            Assert.IsTrue(normalized.Contains((0, 1)));
        }
    }

    [TestClass]
    public class StabilityAnalyzerTests
    {
        [TestMethod]
        public void CheckForStableState_DetectsStablePopulation()
        {
            var game = new GameOfLife(10, 10, 1);
            var analyzer = new StabilityAnalyzer();
            
            // Simulate 5 generations with same population
            for (int i = 0; i < 5; i++)
            {
                analyzer.CheckForStableState(game); // All dead = population 0
            }
            
            Assert.IsTrue(analyzer.CheckForStableState(game));
        }

        [TestMethod]
        public void CheckForStableState_ReturnsFalseForChangingPopulation()
        {
            var game = new GameOfLife(10, 10, 1);
            game.Randomize(0.5); // Random population that will change
            var analyzer = new StabilityAnalyzer();
            
            Assert.IsFalse(analyzer.CheckForStableState(game));
        }
    }
}
