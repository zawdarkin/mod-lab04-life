using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CellularAutomata;

namespace CellularAutomataTests
{
    [TestClass]
    public class CellTests
    {
        [TestMethod]
        public void Cell_StaysDead_WithNoNeighbors()
        {
            var cell = new Cell();
            cell.CalculateNextState();
            Assert.IsFalse(cell.NextState);
        }

        [TestMethod]
        public void Cell_Resurrects_WithExactlyThreeNeighbors()
        {
            var cell = new Cell { IsAlive = false };
            for (int i = 0; i < 3; i++)
                cell.AddNeighbor(new Cell { IsAlive = true });
            
            cell.CalculateNextState();
            Assert.IsTrue(cell.NextState);
        }

        [TestMethod]
        public void Cell_Dies_FromOverpopulation()
        {
            var cell = new Cell { IsAlive = true };
            for (int i = 0; i < 5; i++)
                cell.AddNeighbor(new Cell { IsAlive = true });
            
            cell.CalculateNextState();
            Assert.IsFalse(cell.NextState);
        }

        [TestMethod]
        public void Cell_UpdatesState_Correctly()
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
        private string testDir = Path.Combine(Directory.GetCurrentDirectory(), "TestData");

        [TestInitialize]
        public void Setup()
        {
            Directory.CreateDirectory(testDir);
            File.WriteAllText(Path.Combine(testDir, "empty_board.txt"), "3 3\n000\n000\n000");
            File.WriteAllText(Path.Combine(testDir, "glider_board.txt"), "5 5\n00000\n00100\n00010\n01110\n00000");
        }

        [TestMethod]
        public void Game_Initializes_WithCorrectDimensions()
        {
            var game = new GameOfLife(100, 50, 2);
            Assert.AreEqual(50, game.Width);
            Assert.AreEqual(25, game.Height);
        }

        [TestMethod]
        public void Game_CountsLiveCells_Accurately()
        {
            var game = new GameOfLife(3, 3, 1);
            game.LoadPattern(Path.Combine(testDir, "glider_board.txt"));
            Assert.AreEqual(5, game.CountLiveCells());
        }

        [TestMethod]
        public void Game_LoadsPattern_FromFile()
        {
            var game = new GameOfLife(5, 5, 1);
            game.LoadPattern(Path.Combine(testDir, "glider_board.txt"));
            Assert.IsTrue(game.GetCellState(2, 1));
        }

        [TestMethod]
        public void Game_ThrowsException_ForInvalidPatternFile()
        {
            var game = new GameOfLife(10, 10, 1);
            Assert.ThrowsException<FileNotFoundException>(() => 
                game.LoadPattern("invalid_path.txt"));
        }

        [TestMethod]
        public void Game_ExportsAndImportsState_Identically()
        {
            var game1 = new GameOfLife(5, 5, 1);
            game1.LoadPattern(Path.Combine(testDir, "glider_board.txt"));
            game1.ExportState(Path.Combine(testDir, "exported.txt"));

            var game2 = new GameOfLife(5, 5, 1);
            game2.ImportState(Path.Combine(testDir, "exported.txt"));
            
            Assert.AreEqual(game1.CountLiveCells(), game2.CountLiveCells());
        }
    }

    [TestClass]
    public class PatternRecognizerTests
    {
        private string patternsDir = Path.Combine(Directory.GetCurrentDirectory(), "TestPatterns");

        [TestInitialize]
        public void Setup()
        {
            Directory.CreateDirectory(patternsDir);
            File.WriteAllText(Path.Combine(patternsDir, "blinker.txt"), "010\n010\n010");
            File.WriteAllText(Path.Combine(patternsDir, "block.txt"), "11\n11");
        }

        [TestMethod]
        public void Recognizer_Detects_BlinkerPattern()
        {
            var game = new GameOfLife(3, 3, 1);
            game.LoadPattern(Path.Combine(patternsDir, "blinker.txt"));
            
            var recognizer = new PatternRecognizer();
            var clusters = recognizer.DetectClusters(game);
            var pattern = recognizer.IdentifyPattern(clusters.First(), patternsDir);
            
            Assert.AreEqual("blinker", pattern);
        }

        [TestMethod]
        public void Recognizer_Detects_SingleCell_AsUnknown()
        {
            var cluster = new HashSet<(int, int)> { (0, 0) };
            var recognizer = new PatternRecognizer();
            var result = recognizer.IdentifyPattern(cluster);
            
            Assert.IsTrue(result.Contains("Неизвестный паттерн"));
        }

        [TestMethod]
        public void Recognizer_NormalizesCoordinates_Correctly()
        {
            var cluster = new HashSet<(int, int)> { (5, 10), (6, 10), (5, 11) };
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
        public void Analyzer_DetectsStableState_AfterFiveGenerations()
        {
            var game = new GameOfLife(3, 3, 1);
            var analyzer = new StabilityAnalyzer();
            
            for (int i = 0; i < 5; i++)
                analyzer.CheckForStableState(game);
            
            Assert.IsTrue(analyzer.CheckForStableState(game));
        }

        [TestMethod]
        public void Analyzer_DoesNotDetectStability_ForChangingPopulation()
        {
            var game = new GameOfLife(10, 10, 1, 0.5);
            var analyzer = new StabilityAnalyzer();
            Assert.IsFalse(analyzer.CheckForStableState(game));
        }
    }

    [TestClass]
    public class SimulationConfigTests
    {
        [TestMethod]
        public void Config_SerializesToJson_AndBack()
        {
            var config = new SimulationConfig
            {
                GridWidth = 100,
                GridHeight = 50,
                CellDimension = 2,
                InitialCellDensity = 0.3,
                UpdateDelay = 5
            };

            string json = JsonSerializer.Serialize(config);
            var deserialized = JsonSerializer.Deserialize<SimulationConfig>(json);
            
            Assert.AreEqual(config.GridWidth, deserialized.GridWidth);
        }
    }
}
