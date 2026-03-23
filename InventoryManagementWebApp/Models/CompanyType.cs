using System;
using System.Collections.Generic;

namespace InventoryManagementWebApp.Models
{
    public class CompanyType
    {
        public int CompanyTypeID { get; set; } // Primary Key
        public string Name { get; set; } = string.Empty; // Company Type Name
        public string FullName { get; set; } = string.Empty; // Company Type Full Name

        // Navigation Property
        public virtual ICollection<Company> Companies { get; set; } = new List<Company>();
    }
}

