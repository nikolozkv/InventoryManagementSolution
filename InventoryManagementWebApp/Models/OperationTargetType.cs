using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementWebApp.Models
{
    public class OperationTargetType
    {
        [Key]
        public byte Code { get; set; } // 1, 2, 3, 4, 5

        [Required]
        [StringLength(100)]
        public string Description { get; set; }
    }
}
