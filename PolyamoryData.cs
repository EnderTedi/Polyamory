namespace Polyamory
{
    internal class PolyamoryData
    {
        public bool IsPolyamorous { get; set; } = true;

        public Dictionary<string, Array>? Exclusions { get; set; }
        public Dictionary<string, Array>? Inclusions { get; set; }
    }
}
