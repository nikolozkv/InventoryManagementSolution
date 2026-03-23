using InventoryManagementWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace InventoryManagementWebApp.Controllers
{
    [Authorize]
    public class OperationsController : Controller
    {
        private readonly InventoryContext _context;

        public OperationsController(InventoryContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int barrelId)
        {
            // ვიღებთ მხოლოდ იმ ინფორმაციას, რაც სარეჟისოროდ (Routing) გვჭირდება
            var barrel = await _context.Barrels
                .Include(b => b.Beverage)
                .ThenInclude(bv => bv.ProductType)
                .FirstOrDefaultAsync(b => b.BarrelID == barrelId);

            if (barrel == null)
                return NotFound("კასრი ვერ მოიძებნა.");

            // ვიღებთ TypeCode-ს (მაგ: 'WINE', 'SPIRIT', 'SPARKLING')
            string typeCode = barrel.Beverage?.ProductType?.TypeCode?.ToUpper() ?? "WINE";

            // გადამისამართება შესაბამის კონტროლერზე
            switch (typeCode)
            {
                case "SPIRIT":
                    return RedirectToAction("Index", "Operations_Spirits", new { barrelId = barrelId });

                case "WINE":
                case "SPARKLING": // ცქრიალასაც ღვინის ლოგიკა აქვს
                    return RedirectToAction("Index", "Operations_Wine", new { barrelId = barrelId });

                case "WINE-BASED":
                    // თუ სამომავლოდ შეიქმნება:
                    // return RedirectToAction("Index", "Operations_WineBase", new { barrelId = barrelId });
                    return RedirectToAction("Index", "Operations_Wine", new { barrelId = barrelId }); // დროებით ისევ ღვინოზე

                default:
                    return RedirectToAction("Index", "Operations_Wine", new { barrelId = barrelId });
            }
        }
    }
}