using System;
using System.Collections.Generic;

namespace InventoryManagementWebApp.Models;

public partial class Company
{
    public int CompanyID { get; set; }
    
    public string? CompanyLot { get; set; } // Nullable (optional)
    
    public string Name { get; set; } = string.Empty;

    public int CompanyTypeID { get; set; } // Foreign Key

    public string IdentifierCode { get; set; } = string.Empty;

    public string? ContactInfo { get; set; }

    public string? Address { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    // Navigation Property
    public virtual CompanyType? CompanyType { get; set; } = null!;

}
