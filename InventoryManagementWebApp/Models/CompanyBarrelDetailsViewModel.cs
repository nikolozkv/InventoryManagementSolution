namespace InventoryManagementWebApp.Models
{
    public class CompanyBarrelDetailsViewModel
    {
        public int CompanyID { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyType { get; set; } = string.Empty;
        public string? CompanyLot { get; set; }
        public string? IdentifierCode { get; set; }
        public decimal TotalStock { get; set; }
        public int ArchivedBarrelsCount { get; set; }

        public List<BarrelViewModel> Barrels { get; set; } = new();
    }
}
