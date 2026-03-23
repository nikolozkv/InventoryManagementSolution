using System.ComponentModel.DataAnnotations;

namespace InventoryManagementWebApp.Models
{
    public class BeverageCreateViewModel
    {
        [Required]
        public int BeverageID { get; set; }
        
        [MaxLength(255)]
        public string Name { get; set; }

        [Required]
        public int ProductTypeID { get; set; }
        [Required]
        public int? CategoryID { get; set; }
        [Required]
        public int? SubCategoryID { get; set; }
        [Required]
        public int? ColorID { get; set; }
        [Required]
        public int? SweetnessID { get; set; }

        public bool IsMix { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public int UsageState { get; set; } // 0: Free, 1: TypeLocked, 2: FullLocked
    }
}
