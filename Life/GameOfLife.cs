using System;
using System.Collections.Generic;
using System.IO;

namespace CellularAutomata
{
    public class GameOfLife
    {
        private readonly Cell[,] gameGrid;
        private readonly Random randomGenerator = new Random();

        public int Width { get; }
        public int Height { get; }
        public int CellSize { get; }

        public GameOfLife(int width, int height, int cellSize, double initialDensity = 0.1)
        {
            Width = width / cellSize;
            Height = height / cellSize;
            CellSize = cellSize;

            gameGrid = new Cell[Width, Height];
            InitializeGrid();
            ConnectNeighbors();
            Randomize(initialDensity);
        }

        public bool GetCellState(int x, int y) => gameGrid[x, y].IsAlive;

        private void InitializeGrid()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    gameGrid[x, y] = new Cell();
                }
            }
        }

        private void ConnectNeighbors()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;

                            int nx = (x + dx + Width) % Width;
                            int ny = (y + dy + Height) % Height;
                            gameGrid[x, y].AddNeighbor(gameGrid[nx, ny]);
                        }
                    }
                }
            }
        }

        public void Randomize(double density)
        {
            foreach (var cell in gameGrid)
            {
                cell.IsAlive = randomGenerator.NextDouble() < density;
            }
        }

        public void NextGeneration()
        {
            foreach (var cell in gameGrid)
            {
                cell.CalculateNextState();
            }

            foreach (var cell in gameGrid)
            {
                cell.UpdateState();
            }
        }

        public void ImportState(string filePath)
        {
            using var fileReader = new StreamReader(filePath);
            var dimensions = fileReader.ReadLine().Split(' ');
            int fileWidth = int.Parse(dimensions[0]);
            int fileHeight = int.Parse(dimensions[1]);

            for (int y = 0; y < Math.Min(fileHeight, Height); y++)
            {
                string line = fileReader.ReadLine();
                for (int x = 0; x < Math.Min(fileWidth, Width); x++)
                {
                    gameGrid[x, y].IsAlive = line[x] == '1';
                }
            }
        }

        public void ExportState(string filePath)
        {
            using var fileWriter = new StreamWriter(filePath);
            fileWriter.WriteLine($"{Width} {Height} {CellSize}");

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    fileWriter.Write(gameGrid[x, y].IsAlive ? '1' : '0');
                }
                fileWriter.WriteLine();
            }
        }

        public int CountLiveCells()
        {
            int count = 0;
            foreach (var cell in gameGrid)
            {
                if (cell.IsAlive) count++;
            }
            return count;
        }

        public void LoadPattern(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Файл {filePath} не найден");
            }

            string[] patternLines = File.ReadAllLines(filePath);
            for (int y = 0; y < Math.Min(patternLines.Length, Height); y++)
            {
                for (int x = 0; x < Math.Min(patternLines[y].Length, Width); x++)
                {
                    gameGrid[x, y].IsAlive = patternLines[y][x] == '1';
                }
            }
        }
    }
}