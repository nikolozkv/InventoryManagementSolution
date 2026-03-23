using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementWebApp.Models
{
    public class Operation
    {
        [Key]
        public int OperationID { get; set; }
        public int? LinkedOperationID { get; set; }
        public int? DocumentTypeID { get; set; }

        [ForeignKey("DocumentTypeID")]
        public virtual DocumentType? DocumentType { get; set; }

        public int BarrelID { get; set; }

        public int OperationDefID { get; set; }

        [MaxLength(50)]
        public string? DocumentNumber { get; set; }

        public int BeverageID { get; set; }

        public int HarvestYear { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "date")]
        public DateTime TransactionDate { get; set; } = DateTime.Today;

        public int? ExecutedByUserID { get; set; }

        public int? SourceCompanyID { get; set; }

        public int? SourceBarrelID { get; set; }

        [Column(TypeName = "char(1)")]
        public string Math { get; set; } // უნდა იყოს 1 სიმბოლო: "+" ან "-"

        [MaxLength(100)]
        public string? OperName { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? VolumeLeft { get; set; }

        [Column(TypeName = "decimal(7,2)")]
        public decimal? YearPercentage { get; set; }

        [Column(TypeName = "decimal(7,2)")]
        public decimal? PurePercentage { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? PureAVolume { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? YearAVolume { get; set; }

        [Column(TypeName = "decimal(7,2)")]
        public decimal? PureAPercent { get; set; }

        [Column(TypeName = "decimal(7,2)")]
        public decimal? YearAPercent { get; set; }

        [ForeignKey("OperationDefID")]
        public virtual OperationDefinition OperationDefinition { get; set; } = null!;

        [ForeignKey("BarrelID")]
        public virtual Barrel Barrel { get; set; } = null!;

        [ForeignKey("BeverageID")]
        public virtual Beverage Beverage { get; set; } = null!;
    }
}
