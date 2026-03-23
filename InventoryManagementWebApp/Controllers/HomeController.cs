using InventoryManagementWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims; // აუცილებელია Claim-ების წასაკითხად

namespace InventoryManagementWebApp.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // ვიღებთ იუზერის უფლებების ნიღაბს ქუქიში შენახული Claim-დან
            var maskClaim = User.FindFirst("AllowedProductsMask")?.Value;
            int userMask = 0;

            if (!string.IsNullOrEmpty(maskClaim))
            {
                int.TryParse(maskClaim, out userMask);
            }

            // ვაგზავნით რიცხვს View-ში
            ViewBag.AllowedProductsMask = userMask;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}