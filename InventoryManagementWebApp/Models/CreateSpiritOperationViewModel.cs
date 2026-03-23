using System;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagementWebApp.Models
{
    public class CreateSpiritOperationViewModel
    {
        [Required]
        public int BarrelID { get; set; }
        [Required]
        public int OperationDefID { get; set; }

        [Required(ErrorMessage = "მიუთითეთ რაოდენობა (AA ლიტრებში)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "რაოდენობა უნდა იყოს 0-ზე მეტი")]
        public decimal Quantity { get; set; } // AA ლიტრები

        [Required(ErrorMessage = "მიუთითეთ თარიღი")]
        public DateTime TransactionDate { get; set; } = DateTime.Now.Date;

        [Required]
        public int DocumentTypeID { get; set; }

        [Required]
        public string? DocumentNumber { get; set; }
        public int? ExecutedByUserID { get; set; }
        public int? OppositeBarrelID { get; set; }

        // --- სპეციფიკური ველები დისტილაციისთვის (OperType = 5) ---
        public DateTime? DistillationDate { get; set; }
        public decimal? WineAlcPercent { get; set; }
        public decimal? LossPercent { get; set; } // ახალი დამატებული ველი
    }
}