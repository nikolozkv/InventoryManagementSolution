using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagementWebApp.Models
{
    public class OperationDetailsViewModel
    {
        public int BarrelID { get; set; }

        public string BarrelName { get; set; } = string.Empty;

        public decimal CurrentVolume { get; set; }

        public int? Year { get; set; }

        public List<Operation> Operations { get; set; } = new();

        public List<OperationDefinition> OperationDefinitions { get; set; } = new();

        public List<DocumentType> DocumentTypes { get; set; } = new();

        public List<Company> Companies { get; set; } = new();

        // ახალი ოპერაციის დამატება
        [Required]
        public int OperationDefID { get; set; }

        public int? DocumentTypeID { get; set; }

        [MaxLength(50)]
        public string DocumentNumber { get; set; } = string.Empty;

        [Required]
        public decimal Quantity { get; set; }

        public string Math { get; set; } = string.Empty;

        [Range(0, 100)]
        public decimal? YearPercentage { get; set; }

        [Range(0, 100)]
        public decimal? PurePercentage { get; set; }

        public int? SourceCompanyID { get; set; }

        public int? SourceBarrelID { get; set; }
    }
}
