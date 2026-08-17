using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // 👈 აი ეს ხაზი დაამატე აუცილებლად!

namespace InventoryManagementWebApp.Models;

public partial class Company
{
    public int CompanyID { get; set; }

    [Required(ErrorMessage = "კომპანიის ლოტის შეყვანა აუცილებელია!")]
    public string CompanyLot { get; set; }

    public string Name { get; set; } = string.Empty;

    public int CompanyTypeID { get; set; } // Foreign Key

    [Required(ErrorMessage = "საიდენტიფიკაციო კოდის შეყვანა აუცილებელია!")]
    public string IdentifierCode { get; set; } = string.Empty;

    public string? ContactInfo { get; set; }

    public string? Address { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    // Navigation Property
    public virtual CompanyType? CompanyType { get; set; } = null!;
}