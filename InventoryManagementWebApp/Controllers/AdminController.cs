using InventoryManagementWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace InventoryManagementWebApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IConfiguration _config;

        public AdminController(IConfiguration config)
        {
            _config = config;
        }

        // GET: /Admin/Dashboard
        public IActionResult Dashboard()
        {
            ViewBag.Message = "Welcome to the Admin Dashboard!";
            return View();
        }

        // GET: /Admin/Users
        public IActionResult Users(string filter = "active")
        {
            var users = new List<UserModel>();
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            string whereClause = filter switch
            {
                "inactive" => "WHERE UserId NOT IN (4, 10) AND IsActive = 0",
                "all" => "WHERE UserId NOT IN (4, 10)",
                _ => "WHERE UserId NOT IN (4, 10) AND IsActive = 1" // Default to active
            };

            var cmd = new SqlCommand($"SELECT UserId, Username, Email, FirstName, LastName, IsActive, IsAdmin, AllowedProductsMask FROM Users {whereClause} ORDER BY Username", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int mask = reader["AllowedProductsMask"] != DBNull.Value ? Convert.ToInt32(reader["AllowedProductsMask"]) : 0;

                users.Add(new UserModel
                {
                    UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? 0 : reader.GetInt32(reader.GetOrdinal("UserId")),
                    Username = reader["Username"]?.ToString() ?? string.Empty,
                    Email = reader["Email"]?.ToString() ?? string.Empty,
                    FirstName = reader["FirstName"]?.ToString() ?? string.Empty,
                    LastName = reader["LastName"]?.ToString() ?? string.Empty,
                    IsActive = (bool)reader["IsActive"],
                    IsAdmin = (bool)reader["IsAdmin"],
                    AllowedProductsMask = mask,
                    CanAccessWine = (mask & 1) != 0,
                    CanAccessSparkling = (mask & 2) != 0,
                    CanAccessSpirit = (mask & 4) != 0,
                    CanAccessWineBased = (mask & 8) != 0
                });
            }

            ViewBag.CurrentFilter = filter;
            return View(users);
        }

        // GET: /Admin/EditUser/5
        public IActionResult EditUser(int id)
        {
            var user = GetUserById(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: /Admin/EditUser
        // POST: /Admin/EditUser
        [HttpPost]
        public IActionResult EditUser(UserModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.AllowedProductsMask = CalculateProductsMask(
                    model.CanAccessWine,
                    model.CanAccessSparkling,
                    model.CanAccessSpirit,
                    model.CanAccessWineBased
                );

            // ვამოწმებთ, ვინ არის შემოსული და ვის პროფილს არედაქტირებს
            var currentUserName = User.FindFirst(ClaimTypes.Name)?.Value;
            var targetUser = GetUserById(model.UserId);

            if (targetUser == null)
            {
                return NotFound();
            }

            bool isSelfEdit = (targetUser.Username == currentUserName);

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            SqlCommand cmd;

            if (isSelfEdit)
            {
                // საკუთარი თავის რედაქტირებისას სტატუსებს არ ვეხებით
                cmd = new SqlCommand(@"
                    UPDATE Users
                    SET Username = @username, Email = @email, FirstName = @firstName, 
                        LastName = @lastName, AllowedProductsMask = @mask
                    WHERE UserId = @userId", conn);
            }
            else
            {
                // სხვისი პროფილის რედაქტირებისას სტატუსებსაც ვაახლებთ
                cmd = new SqlCommand(@"
                    UPDATE Users
                    SET Username = @username, Email = @email, FirstName = @firstName, 
                        LastName = @lastName, AllowedProductsMask = @mask,
                        IsActive = @isActive, IsAdmin = @isAdmin
                    WHERE UserId = @userId", conn);

                cmd.Parameters.AddWithValue("@isActive", model.IsActive);
                cmd.Parameters.AddWithValue("@isAdmin", model.IsAdmin);
            }

            cmd.Parameters.AddWithValue("@mask", model.AllowedProductsMask);
            cmd.Parameters.AddWithValue("@username", model.Username);
            cmd.Parameters.AddWithValue("@email", model.Email);
            cmd.Parameters.AddWithValue("@firstName", model.FirstName);
            cmd.Parameters.AddWithValue("@lastName", model.LastName);
            cmd.Parameters.AddWithValue("@userId", model.UserId);

            try
            {
                cmd.ExecuteNonQuery();
                TempData["Message"] = "User updated successfully!";
                return RedirectToAction("Users");
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError("", "Error updating user: " + ex.Message);
                return View(model);
            }
        }

        // POST: /Admin/ToggleActive/5
        [HttpPost]
        public IActionResult ToggleActive(int id)
        {
            var currentUserName = User.FindFirst(ClaimTypes.Name)?.Value;
            var targetUser = GetUserById(id);

            if (targetUser == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Users");
            }

            if (currentUserName == targetUser.Username)
            {
                TempData["Error"] = "You cannot deactivate yourself.";
                return RedirectToAction("Users");
            }

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var cmd = new SqlCommand(@"
                UPDATE Users
                SET IsActive = CASE WHEN IsActive = 1 THEN 0 ELSE 1 END
                WHERE UserId = @userId", conn);
            cmd.Parameters.AddWithValue("@userId", id);

            try
            {
                cmd.ExecuteNonQuery();
                TempData["Message"] = "User active status toggled.";
            }
            catch (SqlException ex)
            {
                TempData["Error"] = "Error toggling user status: " + ex.Message;
            }
            return RedirectToAction("Users");
        }

        // POST: /Admin/ToggleAdmin/5
        [HttpPost]
        public IActionResult ToggleAdmin(int id)
        {
            var currentUserName = User.FindFirst(ClaimTypes.Name)?.Value;
            var targetUser = GetUserById(id);

            if (targetUser == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Users");
            }

            if (currentUserName == targetUser.Username)
            {
                TempData["Error"] = "You cannot change your own admin status.";
                return RedirectToAction("Users");
            }

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var cmd = new SqlCommand(@"
                UPDATE Users
                SET IsAdmin = CASE WHEN IsAdmin = 1 THEN 0 ELSE 1 END
                WHERE UserId = @userId", conn);
            cmd.Parameters.AddWithValue("@userId", id);

            try
            {
                cmd.ExecuteNonQuery();
                TempData["Message"] = "User admin status toggled.";
            }
            catch (SqlException ex)
            {
                TempData["Error"] = "Error toggling admin status: " + ex.Message;
            }
            return RedirectToAction("Users");
        }

        // POST: /Admin/DeleteUser/5
        [HttpPost]
        public IActionResult DeleteUser(int id)
        {
            var currentUserName = User.FindFirst(ClaimTypes.Name)?.Value;
            var targetUser = GetUserById(id);

            if (targetUser == null)
            {
                TempData["Error"] = "User not found.";
                Console.WriteLine("DeleteUser: User not found with ID " + id);
                return RedirectToAction("Users");
            }

            if (currentUserName == targetUser.Username)
            {
                TempData["Error"] = "You cannot delete your own account.";
                Console.WriteLine("DeleteUser: Attempt to delete own account by " + currentUserName);
                return RedirectToAction("Users");
            }

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var cmd = new SqlCommand(@"
                DELETE FROM Users
                WHERE UserId = @userId", conn);
            cmd.Parameters.AddWithValue("@userId", id);

            try
            {
                cmd.ExecuteNonQuery();
                TempData["Message"] = "User account deleted successfully.";
                Console.WriteLine("DeleteUser: Successfully deleted user with ID " + id);
            }
            catch (SqlException ex)
            {
                TempData["Error"] = "Error deleting user account: " + ex.Message;
                Console.WriteLine("DeleteUser: SQL Exception - " + ex.Message);
            }

            return RedirectToAction("Users");
        }

        // GET: /Admin/Roles
        public IActionResult Roles()
        {
            var roles = new List<RoleModel>();
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var cmd = new SqlCommand("SELECT RoleId, RoleName, AccessiblePages FROM Roles ORDER BY RoleName", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                roles.Add(new RoleModel
                {
                    RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
                    RoleName = reader["RoleName"]?.ToString() ?? string.Empty,
                    AccessiblePages = reader["AccessiblePages"]?.ToString() ?? string.Empty
                });
            }
            return View(roles);
        }

        // GET: /Admin/CreateUser
        [HttpGet]
        public IActionResult CreateUser()
        {
            return View(new CreateUserViewModel());
        }

        // POST: /Admin/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateUser(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // ნიღბის დათვლა ახალი იუზერისთვის
            // შენიშვნა: დარწმუნდით რომ CreateUserViewModel კლასშიც გაქვთ დამატებული ის 4 bool ველი CheckBox-ებისთვის
            int mask = CalculateProductsMask(
                model.CanAccessWine,
                model.CanAccessSparkling,
                model.CanAccessSpirit,
                model.CanAccessWineBased
            );

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var checkCmd = new SqlCommand(@"SELECT COUNT(1) FROM Users WHERE Username = @username OR Email = @Email", conn);
            checkCmd.Parameters.AddWithValue("@username", model.Username);
            checkCmd.Parameters.AddWithValue("@Email", model.Email);

            var exists = (int)checkCmd.ExecuteScalar() > 0;
            if (exists)
            {
                ModelState.AddModelError("", "Username or Email already taken.");
                return View(model);
            }

            var (passwordHash, passwordSalt) = HashPassword(model.Password);

            // დამატებულია AllowedProductsMask ინსერტში
            var insertCmd = new SqlCommand(@"INSERT INTO Users (Username, Email, PasswordHash, PasswordSalt, FirstName, LastName, IsActive, IsAdmin, AllowedProductsMask) 
                                              VALUES (@username, @Email, @PasswordHash, @PasswordSalt, @FirstName, @LastName, @IsActive, @IsAdmin, @mask)", conn);

            insertCmd.Parameters.AddWithValue("@username", model.Username);
            insertCmd.Parameters.AddWithValue("@Email", model.Email);
            insertCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
            insertCmd.Parameters.AddWithValue("@PasswordSalt", passwordSalt);
            insertCmd.Parameters.AddWithValue("@FirstName", model.FirstName);
            insertCmd.Parameters.AddWithValue("@LastName", model.LastName);
            insertCmd.Parameters.AddWithValue("@IsActive", model.IsActive);
            insertCmd.Parameters.AddWithValue("@IsAdmin", model.IsAdmin);
            insertCmd.Parameters.AddWithValue("@mask", mask);

            try
            {
                insertCmd.ExecuteNonQuery();
                TempData["Success"] = "User created successfully.";
                return RedirectToAction("Users");
            }
            catch (SqlException ex)
            {
                TempData["Error"] = "An error occurred creating user.";
                return View(model);
            }
        }

        // GET: /Admin/ChangePassword/5
        [HttpGet]
        public IActionResult ChangePassword(int id)
        {
            var user = GetUserById(id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Users");
            }

            var model = new ChangePasswordViewModel
            {
                UserId = user.UserId,
                Username = user.Username
            };

            return View(model);
        }

        // POST: /Admin/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = GetUserById(model.UserId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Users");
            }

            var (passwordHash, passwordSalt) = HashPassword(model.NewPassword);

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var cmd = new SqlCommand(@"
                UPDATE Users
                SET PasswordHash = @passwordHash, PasswordSalt = @passwordSalt
                WHERE UserId = @userId", conn);

            cmd.Parameters.AddWithValue("@passwordHash", passwordHash);
            cmd.Parameters.AddWithValue("@passwordSalt", passwordSalt);
            cmd.Parameters.AddWithValue("@userId", model.UserId);

            try
            {
                cmd.ExecuteNonQuery();
                TempData["Message"] = $"Password for user '{user.Username}' has been changed successfully!";
                return RedirectToAction("EditUser", new { id = model.UserId });
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError("", "Error changing password: " + ex.Message);
                return View(model);
            }
        }

        // Private helper to get a user by ID
        private UserModel? GetUserById(int userId)
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            // დამატებულია AllowedProductsMask ბაზიდან ამოღებისას
            var cmd = new SqlCommand(@"
                SELECT UserId, Username, Email, PasswordHash, PasswordSalt, FirstName, LastName, IsActive, IsAdmin, AllowedProductsMask
                FROM Users
                WHERE UserId = @userId", conn);
            cmd.Parameters.AddWithValue("@userId", userId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                int mask = reader["AllowedProductsMask"] != DBNull.Value ? Convert.ToInt32(reader["AllowedProductsMask"]) : 0;

                return new UserModel
                {
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    Username = reader["Username"]?.ToString() ?? string.Empty,
                    Email = reader["Email"]?.ToString() ?? string.Empty,
                    PasswordHash = reader["PasswordHash"]?.ToString() ?? string.Empty,
                    PasswordSalt = reader["PasswordSalt"]?.ToString() ?? string.Empty,
                    FirstName = reader["FirstName"]?.ToString() ?? string.Empty,
                    LastName = reader["LastName"]?.ToString() ?? string.Empty,
                    IsActive = (bool)reader["IsActive"],
                    IsAdmin = (bool)reader["IsAdmin"],

                    // რიცხვის შენახვა და ოთხი CheckBox-ისთვის მნიშვნელობების (True/False) მინიჭება
                    AllowedProductsMask = mask,
                    CanAccessWine = (mask & 1) != 0,
                    CanAccessSparkling = (mask & 2) != 0,
                    CanAccessSpirit = (mask & 4) != 0,
                    CanAccessWineBased = (mask & 8) != 0
                };
            }
            return null;
        }

        private (string hash, string salt) HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            var salt = Convert.ToBase64String(saltBytes);

            var saltedPassword = Encoding.UTF8.GetBytes(password + salt);
            var computedHash = sha256.ComputeHash(saltedPassword);
            var hash = Convert.ToBase64String(computedHash);

            return (hash, salt);
        }

        private int CalculateProductsMask(bool wine, bool sparkling, bool spirit, bool wineBased)
        {
            int mask = 0;
            if (wine) mask |= 1;
            if (sparkling) mask |= 2;
            if (spirit) mask |= 4;
            if (wineBased) mask |= 8;
            return mask;
        }
    }
}