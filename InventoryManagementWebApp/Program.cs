using InventoryManagementWebApp.Helpers;
using InventoryManagementWebApp.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1️⃣ Database connection
builder.Services.AddDbContext<InventoryContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2️⃣ Cookie-based Authentication with enhanced security for production
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";              // Redirect here if not authenticated
        options.AccessDeniedPath = "/Account/AccessDenied"; // Redirect here if no permission
        options.LogoutPath = "/Account/Logout";            // Optional logout path
        options.Cookie.Name = "InventoryAppAuth";          // Unique cookie name
        options.Cookie.HttpOnly = true;                    // Secure from JS access
        options.Cookie.SameSite = SameSiteMode.Lax;        // Prevent issues on localhost
        options.Cookie.SecurePolicy = builder.Environment.IsProduction() 
            ? CookieSecurePolicy.Always    // Force HTTPS in production
            : CookieSecurePolicy.SameAsRequest; // Flexible for development
        options.ExpireTimeSpan = TimeSpan.FromHours(2);    // Cookie lifetime
        options.SlidingExpiration = true;                  // Refresh cookie on activity
    });

// 3️⃣ MVC and Razor Pages support
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// 4️⃣ Session support (optional but useful for temp data)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // Required for GDPR compliance
    options.Cookie.SecurePolicy = builder.Environment.IsProduction() 
        ? CookieSecurePolicy.Always    // Force HTTPS in production
        : CookieSecurePolicy.SameAsRequest;
});

// 5️⃣ Swagger / OpenAPI for API testing (only in development)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Inventory API",
            Version = "v1",
            Description = "Swagger API documentation for Inventory Management System"
        });
        c.EnableAnnotations();
    });
}

// 🔥 Global Fallback Policy
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// 6️⃣ Build app
var app = builder.Build();

// 7️⃣ Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Add cache-busting middleware for development
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
        context.Response.Headers.Append("Pragma", "no-cache");
        context.Response.Headers.Append("Expires", "0");
        await next();
    });
}
else
{
    // Production error handling
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // Enable HSTS for production
    
    // Add security headers for production
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Append("Content-Security-Policy", "default-src 'self' 'unsafe-inline' 'unsafe-eval' data: https:; img-src 'self' data: https:;");
        await next();
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// 8️⃣ Routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// 9️⃣ Run
app.Run();
