using System.Collections.Generic;

namespace InventoryManagementWebApp.Models
{
    public class OperationsPageViewModel
    {
        // სამუშაო ძირითადი კასრი
        public int BarrelID { get; set; }

        // Header - კომპანია
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyTypeName { get; set; } = string.Empty;
        public string CompanyLot { get; set; } = string.Empty;
        public int CompanyID { get; set; }


        // Header - კასრი
        public int BeverageID { get; set; } // Fixed type to int
        public string BeverageName { get; set; } = string.Empty;
        public int HarvestYear { get; set; } // Fixed type to int
        public decimal CurrentVolume { get; set; }
        public decimal YearPercentage { get; set; }
        public decimal PurePercentage { get; set; }
        public decimal YearAPercentage { get; set; }
        public decimal PureAPercentage { get; set; }

        // ღვინის აღწერა
        public string ProductType { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Sweetness { get; set; } = string.Empty;

        // ოპერაციების სია
        public List<OperationItemViewModel> Operations { get; set; } = new();

        // არსებულ ველებთან ერთად ჩასვით ეს:
        public CreateWineOperationViewModel NewOperation { get; set; } = new();

        public int DocumentTypeID { get; set; }
        public string? DocumentNumber { get; set; }

    }
}
