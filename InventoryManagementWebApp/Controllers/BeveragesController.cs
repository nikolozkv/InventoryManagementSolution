using InventoryManagementWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims; // საჭიროა Claim-ების წასაკითხად

namespace InventoryManagementWebApp.Controllers
{
    [Authorize]
    public class BeveragesController : Controller
    {
        private readonly InventoryContext _context;

        public BeveragesController(InventoryContext context)
        {
            _context = context;
        }

        // ==========================================
        // დამხმარე მეთოდი: მთავარი უფლებების წაკითხვა ლოგინიდან (არა Cookie-დან!)
        // ==========================================
        private int GetUserAllowedMask()
        {
            var maskClaim = User.FindFirst("AllowedProductsMask")?.Value;
            if (!string.IsNullOrEmpty(maskClaim) && int.TryParse(maskClaim, out int userMask))
            {
                return userMask;
            }
            return 0; // უუფლებო
        }

        // ==========================================
        // დამხმარე მეთოდი: Dropdown-ების ჩატვირთვა, ფილტრაცია და სორტირება [Position]-ით
        // ==========================================
        private async Task LoadDropdownDataAsync(int userMask)
        {
            // 1. პროდუქტის ტიპები יფილტრება იუზერის მთავარი უფლებებით დალაგებული Position-ით
            ViewBag.ProductTypes = await _context.BeverageProductTypes
                .Where(pt => (pt.BitValue & userMask) > 0)
                .OrderBy(pt => pt.Position)
                .ToListAsync();

            // 2. ვტვირთავთ სხვა მახასიათებლებს და ვფილტრავთ TypeCodeMask-ით (userMask-ის მიმართ)
            ViewBag.Categories = await _context.BeverageCategories
                .Where(c => (c.TypeCodeMask & userMask) > 0)
                .OrderBy(c => c.Position)
                .ToListAsync();

            ViewBag.SubCategories = await _context.BeverageSubCategories
                .Where(c => (c.TypeCodeMask & userMask) > 0)
                .OrderBy(c => c.Position)
                .ToListAsync();

            ViewBag.Colors = await _context.BeverageColors
                .Where(c => (c.TypeCodeMask & userMask) > 0)
                .OrderBy(c => c.Position)
                .ToListAsync();

            ViewBag.SweetnessLevels = await _context.BeverageSweetnessLevels
                .Where(c => (c.TypeCodeMask & userMask) > 0)
                .OrderBy(c => c.Position)
                .ToListAsync();
        }

        // ==========================================
        // INDEX
        // ==========================================
        public async Task<IActionResult> Index(string search, int? productTypeID, int? categoryID, int? colorID, int? sweetnessID)
        {
            int userMask = GetUserAllowedMask();

            var query = _context.Beverages
                .Include(b => b.ProductType)
                .Include(b => b.Category)
                .Include(b => b.SubCategory)
                .Include(b => b.Color)
                .Include(b => b.Sweetness)
                // ცხრილში გამოჩნდეს მხოლოდ ის სასმელები, რომელზეც იუზერს აქვს წვდომა
                .Where(b => (b.ProductType.BitValue & userMask) > 0)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(b => b.Name.Contains(search));

            if (productTypeID.HasValue)
                query = query.Where(b => b.ProductTypeID == productTypeID.Value);

            if (categoryID.HasValue)
                query = query.Where(b => b.CategoryID == categoryID.Value);

            if (colorID.HasValue)
                query = query.Where(b => b.ColorID == colorID.Value);

            if (sweetnessID.HasValue)
                query = query.Where(b => b.SweetnessID == sweetnessID.Value);

            var beverageList = await query.Select(b => new BeverageViewModel
            {
                BeverageID = b.BeverageID,
                Name = b.Name,
                ProductType = b.ProductType != null ? b.ProductType.Name : "N/A",
                Category = b.Category != null ? b.Category.Name : "N/A",
                SubCategory = b.SubCategory != null ? b.SubCategory.Name : "N/A",
                Color = b.Color != null ? b.Color.Name : "N/A",
                Sweetness = b.Sweetness != null ? b.Sweetness.Name : "N/A",
                IsMix = b.IsMix,
                IsActive = b.IsActive
            }).ToListAsync();

            // ვიძახებთ ჩვენს ახალ მეთოდს Index-ის DropDown-ებისთვისაც (ფილტრის პანელისთვის)
            await LoadDropdownDataAsync(userMask);

            return View(beverageList);
        }

        // ==========================================
        // CREATE
        // ==========================================
        public async Task<IActionResult> Create()
        {
            int userMask = GetUserAllowedMask();
            await LoadDropdownDataAsync(userMask); // აქ ჯერ არ გვაქვს არჩეული ტიპი
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BeverageCreateViewModel model)
        {
            int userMask = GetUserAllowedMask();

            bool exists = await _context.Beverages.AnyAsync(b =>
                b.Name == model.Name &&
                b.ProductTypeID == model.ProductTypeID &&
                b.CategoryID == model.CategoryID &&
                b.SubCategoryID == model.SubCategoryID &&
                b.ColorID == model.ColorID &&
                b.SweetnessID == model.SweetnessID
            );

            if (exists)
            {
                ModelState.AddModelError("Name", "⚠️ ეს სასმელი უკვე დამატებულია იგივე მახასიათებლებით!");
            }

            if (!ModelState.IsValid)
            {
                // თუ შეცდომაა, ფორმაში ვაბრუნებთ ზუსტად მის მიერ არჩეული ტიპის მიხედვით გაფილტრულ სიებს
                await LoadDropdownDataAsync(userMask);
                return View(model);
            }

            try
            {
                var beverage = new Beverage
                {
                    Name = model.Name,
                    ProductTypeID = model.ProductTypeID,
                    CategoryID = model.CategoryID,
                    SubCategoryID = model.SubCategoryID,
                    ColorID = model.ColorID,
                    SweetnessID = model.SweetnessID,
                    IsMix = model.IsMix,
                    IsActive = model.IsActive
                };

                _context.Beverages.Add(beverage);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "სასმელი წარმატებით დაემატა!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"შეცდომა: {ex.Message}";
                await LoadDropdownDataAsync(userMask);
                return View(model);
            }
        }

        // ==========================================
        // EDIT (GET)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var beverage = await _context.Beverages.FindAsync(id);
            if (beverage == null) return NotFound();

            // 1. ვადგენთ სასმელის სტატუსს კასრების მიხედვით (UsageState)
            var barrelStatuses = await _context.Barrels
                .Where(b => b.BeverageID == id)
                .Select(b => b.IsDeletable)
                .ToListAsync();

            int usageState = 0; // 0 = თავისუფალი
            if (barrelStatuses.Any())
            {
                // თუ თუნდაც ერთ კასრს აქვს IsDeletable = false, მაშინ 2 (სრულად დაბლოკილი). სხვა შემთხვევაში 1 (ტიპი დაბლოკილი).
                usageState = barrelStatuses.Any(isDeletable => isDeletable == false) ? 2 : 1;
            }

            var model = new BeverageCreateViewModel
            {
                BeverageID = beverage.BeverageID,
                Name = beverage.Name,
                ProductTypeID = beverage.ProductTypeID,
                CategoryID = beverage.CategoryID,
                SubCategoryID = beverage.SubCategoryID,
                ColorID = beverage.ColorID,
                SweetnessID = beverage.SweetnessID,
                IsMix = beverage.IsMix,
                IsActive = beverage.IsActive,
                UsageState = usageState // ვატანთ სტატუსს ვიუში
            };

            int userMask = GetUserAllowedMask();
            await LoadDropdownDataAsync(userMask);

            return View(model);
        }

        // ==========================================
        // EDIT (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BeverageCreateViewModel model)
        {
            if (id != model.BeverageID) return NotFound();

            int userMask = GetUserAllowedMask();

            // 1. უსაფრთხოების გადამოწმება სერვერზე (კლიენტმა კოდი რომც შეცვალოს, სერვერი იცავს)
            var originalBeverage = await _context.Beverages.AsNoTracking().FirstOrDefaultAsync(b => b.BeverageID == id);
            if (originalBeverage == null) return NotFound();

            var barrelStatuses = await _context.Barrels.Where(b => b.BeverageID == id).Select(b => b.IsDeletable).ToListAsync();
            int actualUsageState = 0;
            if (barrelStatuses.Any()) actualUsageState = barrelStatuses.Any(d => !d) ? 2 : 1;

            // ვალიდაცია: შეცვალა თუ არა აკრძალული ველები?
            if (actualUsageState >= 1 && originalBeverage.ProductTypeID != model.ProductTypeID)
            {
                ModelState.AddModelError("ProductTypeID", "ტიპის შეცვლა დაუშვებელია, რადგან სასმელი უკვე კასრებშია.");
            }
            if (actualUsageState == 2 &&
               (originalBeverage.CategoryID != model.CategoryID ||
                originalBeverage.SubCategoryID != model.SubCategoryID ||
                originalBeverage.ColorID != model.ColorID ||
                originalBeverage.SweetnessID != model.SweetnessID))
            {
                ModelState.AddModelError("", "ისტორიული სასმელის მახასიათებლების შეცვლა დაუშვებელია.");
            }

            // 2. დუბლიკატის შემოწმება (რომ იგივე მახასიათებლებით 2 სასმელი არ არსებობდეს)
            bool exists = await _context.Beverages.AnyAsync(b =>
                b.BeverageID != id &&
                b.Name == model.Name &&
                b.ProductTypeID == model.ProductTypeID &&
                b.CategoryID == model.CategoryID &&
                b.SubCategoryID == model.SubCategoryID &&
                b.ColorID == model.ColorID &&
                b.SweetnessID == model.SweetnessID
            );

            if (exists)
            {
                ModelState.AddModelError("Name", "⚠️ ასეთი სასმელი უკვე არსებობს იგივე მახასიათებლებით!");
            }

            // თუ ვალიდაცია ვერ გაიარა (შეცდომებია), ვაბრუნებთ ფორმაში
            if (!ModelState.IsValid)
            {
                await LoadDropdownDataAsync(userMask);
                model.UsageState = actualUsageState; // აღვადგინოთ სტატუსი
                return View(model);
            }

            // 3. მონაცემების განახლება
            var beverage = await _context.Beverages.FindAsync(id);
            if (beverage == null) return NotFound();

            beverage.Name = model.Name;

            // უსაფრთხოების მიზნით, მხოლოდ იმ ველებს ვაახლებთ, რისი უფლებაც აქვს
            if (actualUsageState == 0)
            {
                beverage.ProductTypeID = model.ProductTypeID;
                beverage.CategoryID = model.CategoryID;
                beverage.SubCategoryID = model.SubCategoryID;
                beverage.ColorID = model.ColorID;
                beverage.SweetnessID = model.SweetnessID;
            }
            else if (actualUsageState == 1)
            {
                beverage.CategoryID = model.CategoryID;
                beverage.SubCategoryID = model.SubCategoryID;
                beverage.ColorID = model.ColorID;
                beverage.SweetnessID = model.SweetnessID;
            }

            beverage.IsMix = model.IsMix;
            beverage.IsActive = model.IsActive;

            try
            {
                _context.Update(beverage);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "სასმელის მონაცემები განახლდა.";
            }
            catch (DbUpdateException ex)
            {
                // თუ SQL ტრიგერმა (trg_PreventBeverageUpdate_Usage) დაბლოკა ცვლილება
                var sqlEx = ex.GetBaseException() as SqlException;
                if (sqlEx != null && (sqlEx.Number == 51001 || sqlEx.Number == 51002))
                {
                    ModelState.AddModelError("", sqlEx.Message);
                }
                else
                {
                    ModelState.AddModelError("", "ბაზის შეცდომა განახლებისას: " + ex.Message);
                }

                await LoadDropdownDataAsync(userMask);
                model.UsageState = actualUsageState;
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // DELETE & OTHERS (დარჩა უცვლელი)
        // ==========================================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var beverage = await _context.Beverages
                .Include(b => b.ProductType)
                .Include(b => b.Category)
                .Include(b => b.SubCategory)
                .Include(b => b.Color)
                .Include(b => b.Sweetness)
                .FirstOrDefaultAsync(m => m.BeverageID == id);

            if (beverage == null) return NotFound();

            return View(beverage);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var beverage = await _context.Beverages.FindAsync(id);
            if (beverage == null) return NotFound();

            try
            {
                _context.Beverages.Remove(beverage);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "სასმელი წარმატებით წაიშალა.";
            }
            catch (DbUpdateException ex)
            {
                var sqlException = ex.GetBaseException() as SqlException;

                if (sqlException != null)
                {
                    if (sqlException.Number == 547)
                    {
                        TempData["ErrorMessage"] = "შეცდომა: ამ სასმელის წაშლა შეუძლებელია, რადგან ის გამოყენებულია კასრებში. ჯერ გაათავისუფლეთ კასრები.";
                    }
                    else if (sqlException.Message.Contains("შეცდომა:"))
                    {
                        TempData["ErrorMessage"] = sqlException.Message;
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "ბაზის შეცდომა: " + sqlException.Message;
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "შეცდომა წაშლისას: " + (ex.InnerException?.Message ?? ex.Message);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "სისტემური შეცდომა: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Archive(int? id)
        {
            if (id == null) return NotFound();

            var beverage = await _context.Beverages
                .Include(b => b.ProductType)
                .Include(b => b.Category)
                .Include(b => b.SubCategory)
                .Include(b => b.Color)
                .Include(b => b.Sweetness)
                .FirstOrDefaultAsync(m => m.BeverageID == id);

            if (beverage == null) return NotFound();

            return View(beverage);
        }

        [HttpPost, ActionName("Archive")]
        public IActionResult ArchiveConfirmed(int id)
        {
            var beverage = _context.Beverages.Find(id);
            if (beverage == null) return NotFound();

            beverage.IsActive = false;
            _context.SaveChanges();

            TempData["SuccessMessage"] = "სასმელი გადავიდა არქივში.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Reactivate(int id)
        {
            var beverage = _context.Beverages
                .Include(b => b.ProductType)
                .Include(b => b.Category)
                .Include(b => b.SubCategory)
                .Include(b => b.Color)
                .Include(b => b.Sweetness)
                .FirstOrDefault(b => b.BeverageID == id);

            if (beverage == null) return NotFound();
            return View(beverage);
        }

        [HttpPost, ActionName("Reactivate")]
        public IActionResult ReactivateConfirmed(int id)
        {
            var beverage = _context.Beverages.Find(id);
            if (beverage == null) return NotFound();

            beverage.IsActive = true;
            _context.SaveChanges();

            TempData["SuccessMessage"] = "სასმელი წარმატებით გააქტიურდა.";
            return RedirectToAction("Index");
        }
    }
}