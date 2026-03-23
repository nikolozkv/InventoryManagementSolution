using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementWebApp.Models
{
    [Table("DocumentType")]
    public class DocumentType
    {
        [Key]
        public int DocumentTypeID { get; set; }
        
        public string DocumentName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
