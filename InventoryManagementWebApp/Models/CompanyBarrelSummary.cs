using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementWebApp.Models
{
    [Keyless]  // ✅ EF-ს ვუთხარით, რომ ეს Keyless View-ია
    public class CompanyBarrelSummary
    {
        public int CompanyID { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyLot { get; set; } = string.Empty;
        public string IdentifierCode { get; set; } = string.Empty;
        public int CompanyTypeID { get; set; }
        public string CompanyTypeName { get; set; } = string.Empty; // ✅ კომპანიის ტიპი
        public int TotalBarrels { get; set; }
    }
}
