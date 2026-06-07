namespace DualMystery
{
    public class Clue
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsDiscovered { get; set; }
        public string DiscoveredBy { get; set; } // "A" 或 "B"
    }
}