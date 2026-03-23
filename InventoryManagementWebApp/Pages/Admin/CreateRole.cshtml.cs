using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;

namespace InventoryManagementWebApp.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class CreateRoleModel : PageModel
    {
        [BindProperty]
        public string RoleName { get; set; } = string.Empty;

        [BindProperty]
        public List<string> SelectedPages { get; set; } = new List<string>();

        public List<string> AccessiblePages { get; set; } = new List<string>();

        private readonly IConfiguration _config;

        public CreateRoleModel(IConfiguration config)
        {
            _config = config;
        }

        public void OnGet()
        {
            // Static list of main pages
            AccessiblePages = new List<string>
            {
                "??????? (Home)",
                "???????????",
                "?????????",
                "???????????",
                "????????",
                "??????????"
            };
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                OnGet(); // Reload the accessible pages
                return Page();
            }

            string accessiblePages = string.Join(",", SelectedPages);

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var cmd = new SqlCommand("INSERT INTO Roles (RoleName, AccessiblePages) VALUES (@RoleName, @AccessiblePages)", conn);
            cmd.Parameters.AddWithValue("@RoleName", RoleName);
            cmd.Parameters.AddWithValue("@AccessiblePages", string.IsNullOrEmpty(accessiblePages) ? DBNull.Value : accessiblePages);

            try
            {
                cmd.ExecuteNonQuery();
                TempData["Message"] = "Role created successfully!";
                return RedirectToPage("/Admin/Roles");
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError(string.Empty, "Error creating role: " + ex.Message);
                OnGet(); // Reload the accessible pages
                return Page();
            }
        }
    }
}