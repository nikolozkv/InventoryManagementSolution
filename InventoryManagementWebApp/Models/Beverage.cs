using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementWebApp.Models
{
    public class Beverage
    {
        [Key]
        public int BeverageID { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int ProductTypeID { get; set; }
        public int? CategoryID { get; set; }
        public int? SubCategoryID { get; set; }
        public int? ColorID { get; set; }
        public int? SweetnessID { get; set; }

        public bool IsMix { get; set; } = false;
        public bool IsActive { get; set; } = true;

        // **ნავიგაციური ველები (Foreign Keys)**
        [ForeignKey("ProductTypeID")]
        public BeverageProductType? ProductType { get; set; } // nullable

        [ForeignKey("CategoryID")]
        public BeverageCategory? Category { get; set; } // nullable

        [ForeignKey("SubCategoryID")]
        public BeverageSubCategory? SubCategory { get; set; }

        [ForeignKey("ColorID")]
        public BeverageColor? Color { get; set; } // nullable

        [ForeignKey("SweetnessID")]
        public BeverageSweetnessLevel? Sweetness { get; set; } // nullable

        // **✅ Many-to-Many კავშირი Blending Types-თან**
        public List<BeverageBlendingType>? BlendingTypes { get; set; }
    }
}
