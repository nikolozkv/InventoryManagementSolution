using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementWebApp.Models
{
    public class OperationMathType
    {
        [Key]
        [StringLength(1)]
        public string Code { get; set; } // + ან -

        [Required]
        [StringLength(100)]
        public string Description { get; set; }
    }
}
