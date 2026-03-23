using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementWebApp.Models
{
    public class Barrel
    {

        [Key]
        public int BarrelID { get; set; } // უნიკალური ID

        [Required]
        public int CompanyID { get; set; } // რომელი კომპანიის კასრია

        [Required]
        public int BeverageID { get; set; } // რომელი ღვინო ინახება

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal CurrentVolume { get; set; } = 0; // ✅ ახალი კასრი ცარიელი

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now; // კასრის შექმნის თარიღი
        public DateTime? UpdatedDate { get; set; }

        [ForeignKey("CompanyID")]
        public Company? Company { get; set; } // კომპანია Foreign Key

        [ForeignKey("BeverageID")]
        public Beverage? Beverage { get; set; } // სასმელი Foreign Key
        public bool IsActive { get; set; } = true;  // ✅ აქტიურობა (თუ კასრი აღარაა გამოყენებაში, False)
        public int? CreatedByUserID { get; set; }  // ვინ შექმნა
        public bool IsDeletable { get; set; } = true;
        public int? UpdatedByUserID { get; set; }  // ვინ განაახლა

        [ForeignKey("CreatedByUserID")]
        public virtual User? CreatedBy { get; set; }

        [ForeignKey("UpdatedByUserID")]
        public virtual User? UpdatedBy { get; set; }

        // 1. დიაპაზონი იწყება 0-დან (რომ 0 არ დაიბლოკოს)
        // 2. ტიპი არის int? (რომ ცარიელი ველის დატოვება შეძლოთ UI-ში)
        [Range(0, 3000, ErrorMessage = "მიუთითეთ რეალური წელი (1900+) ან 0")]
        public int? Year { get; set; } // წელი

        [Column(TypeName = "decimal(7,2)")]
        public decimal? YearPercentage { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal? PurePercentage { get; set; } = 0;


        // სპირტისთვის საჭირო ახალი ველები:
        //public decimal? CurrentPureVolume { get; set; } // AA ლიტრების მიმდინარე ჯამი
        public DateTime? WeightedAvgDate { get; set; }  // საშუალო თარიღი (ასაკისთვის)

        // ბიტური ნიღაბი (რომელიც უკვე გვაქვს ბაზაში)
        public int ProductTypeBitValue { get; set; }
    }
}
