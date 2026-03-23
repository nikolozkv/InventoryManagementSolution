using InventoryManagementWebApp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;


public class AccountController : Controller
{
    private readonly IConfiguration _config;

    public AccountController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string returnUrl = "/")
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = GetUser(model.UsernameOrEmail);
        if (user == null || !VerifyPassword(model.Password, user))
        {
            ModelState.AddModelError("", "Invalid username or password.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("FullName", $"{user.FirstName} {user.LastName}".Trim()),
            new Claim("FirstName", user.FirstName),
            new Claim("LastName", user.LastName),
            new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User"),
            new Claim("AllowedProductsMask", user.AllowedProductsMask.ToString())
        };

        var userId = GetUserIdByUsernameOrEmail(user.Username);
        if (userId != null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        Console.WriteLine($"Signed in: {User.Identity?.IsAuthenticated}");
        Console.WriteLine($"Name: {User.Identity?.Name}");
        Console.WriteLine($"UserID claim added: {userId}");

        return LocalRedirect(model.ReturnUrl ?? "/");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Account");
    }

    private UserModel? GetUser(string usernameOrEmail)
    {
        using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        conn.Open();

        var cmd = new SqlCommand(@"
            SELECT TOP 1 Username, Email, PasswordHash, PasswordSalt, FirstName, LastName, IsAdmin, AllowedProductsMask
            FROM Users
            WHERE (Username = @login OR Email = @login) AND IsActive = 1", conn);

        cmd.Parameters.AddWithValue("@login", usernameOrEmail);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new UserModel
            {
                Username = reader["Username"]?.ToString() ?? string.Empty,
                Email = reader["Email"]?.ToString() ?? string.Empty,
                PasswordHash = reader["PasswordHash"]?.ToString() ?? string.Empty,
                PasswordSalt = reader["PasswordSalt"]?.ToString() ?? string.Empty,
                FirstName = reader["FirstName"]?.ToString() ?? string.Empty,
                LastName = reader["LastName"]?.ToString() ?? string.Empty,
                IsAdmin = reader["IsAdmin"] != DBNull.Value && (bool)reader["IsAdmin"],
                AllowedProductsMask = reader["AllowedProductsMask"] != DBNull.Value ? Convert.ToInt32(reader["AllowedProductsMask"]) : 0
            };
        }
        return null;
    }

    private (string hash, string salt) CreatePasswordHash(string password)
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

    private bool VerifyPassword(string enteredPassword, UserModel user)
    {
        using var sha256 = SHA256.Create();
        var saltedPassword = Encoding.UTF8.GetBytes(enteredPassword + user.PasswordSalt);
        var computedHash = sha256.ComputeHash(saltedPassword);
        var computedHashString = Convert.ToBase64String(computedHash);

        return string.Equals(user.PasswordHash, computedHashString, StringComparison.Ordinal);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public IActionResult Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (UserExists(model.Username, model.Email))
        {
            ModelState.AddModelError("", "Username or Email already taken.");
            return View(model);
        }

        // ნიღბის დათვლა მონიშნული CheckBox-ებიდან
        int mask = 0;
        if (model.CanAccessWine) mask |= 1;
        if (model.CanAccessSparkling) mask |= 2;
        if (model.CanAccessSpirit) mask |= 4;
        if (model.CanAccessWineBased) mask |= 8;

        var (passwordHash, passwordSalt) = CreatePasswordHash(model.Password);

        using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        conn.Open();

        // SQL-ში ჩაემატა AllowedProductsMask
        var cmd = new SqlCommand(@"
            INSERT INTO Users (Username, Email, PasswordHash, PasswordSalt, FirstName, LastName, IsActive, IsAdmin, AllowedProductsMask)
            VALUES (@username, @email, @passwordHash, @passwordSalt, @firstName, @lastName, 1, 0, @mask)", conn);

        cmd.Parameters.AddWithValue("@username", model.Username);
        cmd.Parameters.AddWithValue("@email", model.Email);
        cmd.Parameters.AddWithValue("@passwordHash", passwordHash);
        cmd.Parameters.AddWithValue("@passwordSalt", passwordSalt);
        cmd.Parameters.AddWithValue("@firstName", model.FirstName);
        cmd.Parameters.AddWithValue("@lastName", model.LastName);
        cmd.Parameters.AddWithValue("@mask", mask);

        try
        {
            cmd.ExecuteNonQuery();
            return RedirectToAction("Login", new { message = "Registration successful! Please log in." });
        }
        catch (SqlException ex)
        {
            ModelState.AddModelError("", "An error occurred during registration. Please try again.");
            return View(model);
        }
    }

    private bool UserExists(string username, string email)
    {
        using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        conn.Open();

        var cmd = new SqlCommand(@"
            SELECT COUNT(1) FROM Users WHERE Username = @username OR Email = @email", conn);
        cmd.Parameters.AddWithValue("@username", username);
        cmd.Parameters.AddWithValue("@email", email);

        return (int)cmd.ExecuteScalar() > 0;
    }

    private int? GetUserIdByUsernameOrEmail(string usernameOrEmail)
    {
        using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        conn.Open();

        var cmd = new SqlCommand(@"SELECT UserId FROM Users WHERE Username = @login OR Email = @login", conn);
        cmd.Parameters.AddWithValue("@login", usernameOrEmail);

        var result = cmd.ExecuteScalar();
        return result != null ? Convert.ToInt32(result) : (int?)null;
    }
}