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

            // ვიღებთ TypeCode-ის BitValue (მაგ: 1-'WINE', 4-'SPIRIT', 2-'SPARKLING', 16-'ALCOHOLIC BEVERAGES')
            int typeCode = barrel.Beverage?.ProductType?.BitValue ?? 11;

            // გადამისამართება შესაბამის კონტროლერზე
            switch (typeCode)
            {
                case (4): // სპირტი
                case (16): // სპირტიანი სასმელი

                    return RedirectToAction("Index", "Operations_Spirits", new { barrelId = barrelId });

                case (1): // ღვინო
                case (2): // ცქრიალა ღვინო
                    return RedirectToAction("Index", "Operations_Wine", new { barrelId = barrelId });

                case (8): // ღვინისეული
                    // თუ სამომავლოდ შეიქმნება:
                    return RedirectToAction("Index", "Operations_Wine", new { barrelId = barrelId }); // დროებით ისევ ღვინოზე

                default:
                    return RedirectToAction("Index", "Operations_Wine", new { barrelId = barrelId });
            }
        }
    }
}