using System.ComponentModel.DataAnnotations;

namespace InventoryManagementWebApp.Models
{
    public class BeverageSubCategory
    {
        [Key]
        public int SubCategoryID { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        public int TypeCodeMask { get; set; } // დაემატა
        public int Position { get; set; }     // დაემატა
        public string? Note { get; set; }
    }
}
