namespace InventoryManagementWebApp.Models
{
    public class UserModel
    {
        public int UserId { get; set; } // Add this! Your table likely has an ID.
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PasswordSalt { get; set; } = string.Empty; // Make sure your DB has this column
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public bool IsActive { get; set; } = true; // Assuming users are active by default


        public int AllowedProductsMask { get; set; } // ბაზიდან წამოღებული მთავარი რიცხვი

        // დამხმარე ველები CheckBox-ებისთვის (View-სთვის)
        public bool CanAccessWine { get; set; }
        public bool CanAccessSparkling { get; set; }
        public bool CanAccessSpirit { get; set; }
        public bool CanAccessWineBased { get; set; }
    }
}