namespace InventoryManagementWebApp.Models.ViewModels
{
    public class OperationPageHeaderViewModel
    {
        public string CompanyTypeName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyLot { get; set; } = string.Empty;

        public string BeverageName { get; set; } = string.Empty;
        public int HarvestYear { get; set; }
        public decimal? YearPercentage { get; set; }
        public decimal? PurePercentage { get; set; }
        public decimal CurrentVolume { get; set; }

        public string ProductType { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Sweetness { get; set; } = string.Empty;
    }
}
