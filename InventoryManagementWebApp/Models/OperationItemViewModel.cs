using System;

namespace InventoryManagementWebApp.Models
{
    public class OperationItemViewModel
    {
        public int OperationID { get; set; }
        public DateTime TransactionDate { get; set; }
        public string OperationName { get; set; } = string.Empty;
        public string OperName { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string DocumentTypeName { get; set; } = string.Empty;
        public string Quantity { get; set; } = string.Empty; // ✅ ადრე decimal იყო
        public string Math { get; set; } = string.Empty;  // string (char.ToString())
        public int HarvestYear { get; set; }
        public decimal? YearPercentage { get; set; }
        public decimal? PurePercentage { get; set; }
        public decimal? YearAPercent { get; set; } // Fixed property name to match usage
        public decimal? PureAPercent { get; set; } // Fixed property name to match usage
        public decimal? VolumeLeft { get; set; }  // ✅ ახალ ველად
        public int BarrelID { get; set; }
        public string BeverageName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Sweetness { get; set; } = string.Empty;
        public int? ExecutedByUserID { get; set; }
        public string ExecutedByUserFirstName { get; set; } = string.Empty;
        public string ExecutedByUserLastName { get; set; } = string.Empty;
        public string ExecutedByUserFullName => $"{ExecutedByUserFirstName} {ExecutedByUserLastName}".Trim();
        
        // Linked operation / Source company information
        public int? LinkedOperationID { get; set; }
        public int? SourceCompanyID { get; set; }
        public int? SourceBarrelID { get; set; }
        public string SourceCompanyName { get; set; } = string.Empty;
        public string OperNameWithCompany => !string.IsNullOrEmpty(SourceCompanyName) 
            ? $"{OperName}\n{SourceCompanyName}" 
            : OperName;
    }
}
