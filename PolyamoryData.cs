namespace Polyamory
{
    internal class PolyamoryData
    {
        public bool IsPolyamorous { get; set; } = true;
        public bool UseBouquetRejectDialogue { get; set; } = false;
        public bool UsePedantRejectDialogue { get; set; } = false;

        public string[]? PositiveChemistry { get; set; }
        public string[]? NegativeChemistry { get; set; }
    }
}
