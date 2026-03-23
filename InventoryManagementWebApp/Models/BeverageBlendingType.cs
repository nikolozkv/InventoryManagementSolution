using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagementWebApp.Models
{
    public class BeverageBlendingType
    {
        [Key]
        public int BlendingTypeID { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public List<Beverage>? Beverages { get; set; } // Many-to-Many კავშირი
    }
}
