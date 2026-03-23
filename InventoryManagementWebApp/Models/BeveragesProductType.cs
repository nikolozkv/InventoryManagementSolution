using System.ComponentModel.DataAnnotations;

namespace InventoryManagementWebApp.Models
{
    public class BeverageProductType
    {
        [Key]
        public int ProductTypeID { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        public string? TypeCode { get; set; }
        public int BitValue { get; set; }
        public int Position { get; set; } // დაემატა
        public string? Note { get; set; }
    }
}
