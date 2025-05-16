namespace CellularAutomata
{
    public class SimulationConfig
    {
        public int GridWidth { get; set; } = 50;
        public int GridHeight { get; set; } = 20;
        public int CellDimension { get; set; } = 1;
        public double InitialCellDensity { get; set; } = 0.5;
        public int UpdateDelay { get; set; } = 2;
    }
}