using System.ComponentModel.DataAnnotations;
using InventoryManagementWebApp.Models;

namespace InventoryManagementWebApp.Models
{
    public class OperationDefinition
    {
        [Key] // ✅ ეს ხაზს უსვამს რომ ეს არის primary key
        public int OperationDefID { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [RegularExpression(@"[+-]", ErrorMessage = "არითმეტიკა უნდა იყოს + ან -")]
        public string Math { get; set; }

        [Range(1, 5)]
        public byte OperType { get; set; }

        public string? NameOpposite { get; set; }

        public bool IsActive { get; set; } = true;

        public bool PreserveBarrelState { get; set; }

        public int? TypeCodeMask { get; set; }

        public int? Position { get; set; }
    }
}