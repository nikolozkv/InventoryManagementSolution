namespace InventoryManagementWebApp.Models
{
    public class BeverageViewModel
    {
        public int BeverageID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Sweetness { get; set; } = string.Empty;
        public bool IsMix { get; set; }
        public bool IsActive { get; set; }
        public decimal? PurePercentage { get; set; }
        public decimal? YearPercentage { get; set; }
    }
}
