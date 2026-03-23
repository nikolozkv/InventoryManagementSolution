using System;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagementWebApp.Models
{
    public class CreateWineOperationViewModel
    {
        [Required]
        public int OperationDefID { get; set; }

        [Required]
        public int BarrelID { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Required]
        [MaxLength(50)]
        public string DocumentNumber { get; set; }

        [Required]
        public int DocumentTypeID { get; set; }

        public int? ExecutedByUserID { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; }

        public int? BeverageID { get; set; }
        public int HarvestYear { get; set; }
        public decimal? YearAPercent { get; set; }
        public decimal? PureAPercent { get; set; }
        public int? OppositeBarrelID { get; set; }
        public int? OppositeCompanyID { get; set; }

        // მხოლოდ შედეგისთვის; არ არის შესავსები ველი
        [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
        public string Message { get; set; }
    }
}
