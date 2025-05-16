using System.Collections.Generic;
using System.Linq;

namespace CellularAutomata
{
    public class Cell
    {
        public bool IsAlive { get; set; }
        public bool NextState { get; private set; }
        public List<Cell> Neighbors { get; } = new List<Cell>();

        public void AddNeighbor(Cell neighbor)
        {
            Neighbors.Add(neighbor);
        }

        public void CalculateNextState()
        {
            int liveNeighbors = Neighbors.Count(c => c.IsAlive);

            if (IsAlive)
            {
                NextState = liveNeighbors == 2 || liveNeighbors == 3;
            }
            else
            {
                NextState = liveNeighbors == 3;
            }
        }

        public void UpdateState()
        {
            IsAlive = NextState;
        }
    }
}