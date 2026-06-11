namespace DualMystery
{
    public class Clue
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsDiscovered { get; set; }
        public string DiscoveredBy { get; set; } // "A" 或 "B"
        public string SharedTo { get; set; }      // null=未分享, "A"/"B"=已分享给谁
        public bool IsShared { get { return !string.IsNullOrEmpty(SharedTo); } }
    }
}