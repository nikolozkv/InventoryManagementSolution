using System.ComponentModel.DataAnnotations;
using BCrypt.Net;


namespace InventoryManagementWebApp.Models
{
    public class User
    {
        //[Key]
        public int UserID { get; set; }

        [Required]
        [MaxLength(50)]
        //public string Username { get; set; } = string.Empty;
        public string Username { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        public string PasswordHash { get; set; } = null!; // ✅ პაროლის ჰეში
        public string Role { get; set; } = "User"; // ✅ როლის ველი    }
        public bool VerifyPassword(string password)
        {
            return BCrypt.Net.BCrypt.Verify(password, PasswordHash);
        }
    }

}
