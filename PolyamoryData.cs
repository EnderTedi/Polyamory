namespace Polyamory
{
    internal class PolyamoryData
    {
        public bool IsPolyamorous { get; set; } = true;

        public string[]? PositiveChemistry { get; set; }
        public string[]? NegativeChemistry { get; set; }

        public bool CanGoOutInTheSun { get; set; } = false;
    }
}
