using System.ComponentModel.DataAnnotations;

namespace InventoryManagementWebApp.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "First Name is required")]
        [StringLength(50, ErrorMessage = "First Name cannot exceed 50 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last Name is required")]
        [StringLength(50, ErrorMessage = "Last Name cannot exceed 50 characters.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "Password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty; // <-- Fix is right here!

        // You might consider adding a flag here for initial admin creation,
        // but typically admin designation is a post-registration step by another admin.
        // public bool IsAdmin { get; set; } = false; // Careful with this for public registration

        public int AllowedProductsMask { get; set; } // ბაზიდან წამოღებული მთავარი რიცხვი

        // დამხმარე ველები CheckBox-ებისთვის (View-სთვის)
        public bool CanAccessWine { get; set; }
        public bool CanAccessSparkling { get; set; }
        public bool CanAccessSpirit { get; set; }
        public bool CanAccessWineBased { get; set; }
    }
}