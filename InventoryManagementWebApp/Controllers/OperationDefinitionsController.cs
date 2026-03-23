// File: Controllers/OperationDefinitionsController.cs

using InventoryManagementWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryManagementWebApp.Controllers
{
    [Authorize]
    public class OperationDefinitionsController : Controller
    {
        private readonly InventoryContext _context;

        public OperationDefinitionsController(InventoryContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var defs = await _context.OperationDefinitions.ToListAsync();
            return View(defs);
        }

        public async Task<IActionResult> Create()
        {
            await LoadSelectLists();
            return View(new OperationDefinition());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OperationDefinition model)
        {
            if (model.OperType > 1)
            {
                if (string.IsNullOrWhiteSpace(model.NameOpposite))
                    ModelState.AddModelError(nameof(model.NameOpposite), "აუცილებელია საპირწონე სახელის მითითება");

                if (model.Math == "-")
                    ModelState.AddModelError(nameof(model.Math), "მინუსი დაუშვებელია ოპერაციის ტიპისთვის >1");
            }

            if (!ModelState.IsValid)
            {
                await LoadSelectLists();
                return View(model);
            }

            _context.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var def = await _context.OperationDefinitions.FindAsync(id);
            if (def == null) return NotFound();
            await LoadSelectLists();
            return View(def);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OperationDefinition model)
        {
            if (id != model.OperationDefID)
                return NotFound();

            // ლოგიკური ვალიდაცია
            if (model.OperType > 1)
            {
                if (string.IsNullOrWhiteSpace(model.NameOpposite))
                    ModelState.AddModelError(nameof(model.NameOpposite), "აუცილებელია საპირწონე სახელის მითითება");

                if (model.Math == "-")
                    ModelState.AddModelError(nameof(model.Math), "მინუსი დაუშვებელია ოპერაციის ტიპისთვის >1");
            }

            if (!ModelState.IsValid)
            {
                await LoadSelectLists();
                return View(model);
            }

            // ✅ მოძებნე არსებული ჩანაწერი ბაზიდან
            var existing = await _context.OperationDefinitions.FindAsync(id);
            if (existing == null)
                return NotFound();

            // ✅ განაახლე მხოლოდ შესაბამისი ველები
            existing.Name = model.Name;
            existing.Math = model.Math;
            existing.OperType = model.OperType;
            existing.NameOpposite = model.NameOpposite;
            existing.PreserveBarrelState = model.PreserveBarrelState;

            // ✅ არ შეეხო IsActive-ს, რათა არ გადაიფაროს
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        private async Task LoadSelectLists()
        {
            ViewBag.MathOptions = await _context.OperationMathTypes
                .Select(x => new SelectListItem
                {
                    Value = x.Code,
                    //Text = $"{x.Code} - {x.Description}"
                    Text = $"{x.Description}"
                }).ToListAsync();

            ViewBag.OperTypeOptions = await _context.OperationTargetTypes
                .Select(x => new SelectListItem
                {
                    Value = x.Code.ToString(),
                    //Text = $"{x.Code} - {x.Description}"
                    Text = $"{x.Description}"
                }).ToListAsync();
        }

        [HttpPost]
        public async Task<IActionResult> Activate(int id)
        {
            var def = await _context.OperationDefinitions.FindAsync(id);
            def.IsActive = true;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Deactivate(int id)
        {
            var def = await _context.OperationDefinitions.FindAsync(id);
            def.IsActive = false;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

    }
}
