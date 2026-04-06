namespace InventoryManagementWebApp.Models
{
    public class BarrelViewModel
    {
        public int BarrelID { get; set; }
        public string BeverageName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty; // თუ ბაზა აბრუნებს
        public string Color { get; set; } = string.Empty;
        public string Sweetness { get; set; } = string.Empty;
        public decimal CurrentVolume { get; set; }
        public int? Year { get; set; }
        public decimal? YearPercentage { get; set; }
        public decimal? PurePercentage { get; set; }
        public decimal CurrentAlcPercent { get; set; }
        public bool IsDeletable { get; set; }
        public bool IsActive { get; set; }

        // ⚠️ მნიშვნელოვანია: თუ ბაზა უკვე აბრუნებს ამ ორ სვეტს, ისინი აქ უნდა იყოს!
        public DateTime? WeightedAvgDate { get; set; }
        public int? ProductTypeBitValue { get; set; }
    }
}