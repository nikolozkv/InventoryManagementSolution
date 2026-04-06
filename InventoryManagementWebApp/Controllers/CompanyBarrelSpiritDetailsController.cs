using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Spreadsheet;
using InventoryManagementWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementWebApp.Controllers
{
    [Authorize]
    public class CompanyBarrelSpiritDetailsController : Controller
    {
        private readonly InventoryContext _context;

        public CompanyBarrelSpiritDetailsController(InventoryContext context)
        {
            _context = context;
        }

        // ==========================================
        // დამხმარე მეთოდი: ნიღბის (Working Mask) წაკითხვა
        // ==========================================
        private int GetWorkingMask()
        {
            // 1. იუზერის მაქსიმალური დაშვებები
            int userMask = 0;
            var maskClaim = User.FindFirst("AllowedProductsMask")?.Value;
            if (!string.IsNullOrEmpty(maskClaim)) int.TryParse(maskClaim, out userMask);

            // 2. ქუქიში დამახსოვრებული მიმდინარე სამუშაო პაკეტი (თუ არსებობს)
            int cookieMask = userMask;
            if (HttpContext.Request.Cookies.TryGetValue("CurrentWorkingMask", out string? cookieStr)
                && int.TryParse(cookieStr, out int parsedCookie))
            {
                cookieMask = parsedCookie;
            }

            // ვაბრუნებთ უსაფრთხოდ გადამოწმებულ ნიღაბს
            return userMask & cookieMask;
        }
        private int GetCurrentUserId()
        {
            // ვეძებთ NameIdentifier ქლეიმს (ეს არის სტანდარტული ID-ს ადგილი)
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // თუ ვერ იპოვა (რაც წესით არ უნდა მოხდეს), დეფოლტად იყოს ისევ 1, რომ არ გაჩერდეს პროგრამა
            return int.TryParse(userIdClaim, out int userId) ? userId : 1;
        }

        // ==========================================
        // INDEX
        // ==========================================
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Index(int companyId, string? search1, string? search2)
        {
            var company = await _context.Companies
                .Include(c => c.CompanyType)
                .FirstOrDefaultAsync(c => c.CompanyID == companyId);

            if (company == null) return NotFound("კომპანია ვერ მოიძებნა!");

            // ვიღებთ მიმდინარე სამუშაო ნიღაბს (სპირტის გვერდზე ეს იქნება სპირტის ბიტი)
            int workingMask = GetWorkingMask();

            var pId = new SqlParameter("@CompanyID", companyId);
            var pMask = new SqlParameter("@ProductTypeMask", workingMask);
            var p1 = new SqlParameter("@Search1", search1 ?? (object)DBNull.Value);
            var p2 = new SqlParameter("@Search2", search2 ?? (object)DBNull.Value);

            // SQL-ში ვაგზავნით ნიღაბს
            var barrelsData = await _context.BarrelViewModels
                .FromSqlRaw("EXEC [dbo].[GetCompanyBarrelsDetailed] @CompanyID, @ProductTypeMask, @Search1, @Search2", pId, pMask, p1, p2)
                .ToListAsync();

            // არქივის რაოდენობაც უნდა დავითვალოთ მხოლოდ ამ ნიღბის ფარგლებში
            int archivedCount = await _context.Barrels
                .CountAsync(b => b.CompanyID == companyId && !b.IsActive && (b.ProductTypeBitValue & workingMask) > 0);

            var viewModel = new CompanyBarrelDetailsViewModel
            {
                CompanyID = company.CompanyID,
                CompanyName = company.Name,
                CompanyType = company.CompanyType?.Name ?? "N/A",
                CompanyLot = company.CompanyLot,
                IdentifierCode = company.IdentifierCode,
                TotalStock = barrelsData.Sum(b => b.CurrentVolume),
                ArchivedBarrelsCount = archivedCount,
                Barrels = barrelsData
            };

            return View(viewModel);
        }

        // ==========================================
        // CREATE (GET)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Create(int companyId)
        {
            var company = await _context.Companies.FindAsync(companyId);
            if (company == null) return NotFound("კომპანია ვერ მოიძებნა!");

            int workingMask = GetWorkingMask();
            await PopulateDropDowns(workingMask);

            var newBarrel = new Barrel
            {
                CompanyID = companyId,
                Year = DateTime.Now.Year // მიმდინარე წელი როგორც ნაგულისხმევი მნიშვნელობა
            };
            return View(newBarrel);
        }

        // ==========================================
        // CREATE (POS)
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Barrel barrel)
        {
            int userId = GetCurrentUserId();
            int workingMask = GetWorkingMask();

            barrel.Year = DateTime.Now.Year; // მიმდინარე წელი როგორც ნაგულისხმევი მნიშვნელობა

            // --- დუბლიკატზე შემოწმების ბლოკი სრულად ამოღებულია ---

            try
            {
                var pNewId = new SqlParameter("@NewBarrelID", SqlDbType.Int) { Direction = ParameterDirection.Output };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC [dbo].[BarrelCreate] @CompanyID, @BeverageID, @Year, @CreatedByUserID, @NewBarrelID OUTPUT",
                    new SqlParameter("@CompanyID", barrel.CompanyID),
                    new SqlParameter("@BeverageID", barrel.BeverageID),
                    new SqlParameter("@Year", barrel.Year),
                    // TODO: აქ სასურველია რეალური UserID-ის წამოღება (ახლა ხისტად 1 წერია)
                    new SqlParameter("@CreatedByUserID", userId),
                    pNewId);

                TempData["SuccessMessage"] = "სპირტის კასრი წარმატებით შეიქმნა. ID: " + pNewId.Value;
                return RedirectToAction("Index", new { companyId = barrel.CompanyID });
            }
            catch (SqlException ex)
            {
                TempData["ErrorMessage"] = "ბაზის შეცდომა: " + ex.Message;
                await PopulateDropDowns(workingMask);
                return View(barrel);
            }
        }

        // ==========================================
        // EDIT (GET)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var barrel = await _context.Barrels
                .Include(b => b.Beverage).ThenInclude(x => x.ProductType)
                .FirstOrDefaultAsync(m => m.BarrelID == id);

            if (barrel == null) return NotFound();

            int workingMask = GetWorkingMask();
            await PopulateDropDowns(workingMask);
            return View(barrel);
        }

        // ==========================================
        // EDIT (POST)
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Barrel barrel)
        {
            if (id != barrel.BarrelID) return NotFound();

            int userId = GetCurrentUserId();
            int workingMask = GetWorkingMask();

            // --- დუბლიკატზე შემოწმების ბლოკი სრულად ამოღებულია ---

            try
            {
                var pMessage = new SqlParameter("@Message", SqlDbType.NVarChar, 200) { Direction = ParameterDirection.Output };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC [dbo].[BarrelBeverageEdit] @BarrelID, @NewBeverageID, @NewYear, @UpdatedByUserID, @Message OUTPUT",
                    new SqlParameter("@BarrelID", barrel.BarrelID),
                    new SqlParameter("@NewBeverageID", barrel.BeverageID),
                    new SqlParameter("@NewYear", barrel.Year ?? DateTime.Now.Year),
                    new SqlParameter("@UpdatedByUserID", userId),
                    pMessage);

                TempData["SuccessMessage"] = pMessage.Value.ToString();
                return RedirectToAction(nameof(Index), new { companyId = barrel.CompanyID });
            }
            catch (SqlException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                await PopulateDropDowns(workingMask);
                return View(barrel);
            }
        }

        // ==========================================
        // STATUS (Archive / Reactivate)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Status(int id)
        {
            var barrel = await _context.Barrels
            .Include(b => b.Beverage)
            .FirstOrDefaultAsync(b => b.BarrelID == id);

            if (barrel == null) return NotFound();
            return View(barrel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, bool newStatus)
        {
            var barrel = await _context.Barrels.FindAsync(id);
            if (barrel == null) return NotFound();

            if (newStatus == true)
            {
                bool activeDuplicateExists = await _context.Barrels.AnyAsync(b =>
                b.CompanyID == barrel.CompanyID &&
                b.BeverageID == barrel.BeverageID &&
                b.Year == barrel.Year &&
                b.IsActive == true &&
                b.BarrelID != barrel.BarrelID);

                if (activeDuplicateExists)
                {
                    TempData["ErrorMessage"] = "ვერ ხერხდება გააქტიურება: მსგავსი აქტიური კასრი უკვე არსებობს!";
                    return RedirectToAction("Status", new { id = id });
                }
            }
            barrel.IsActive = newStatus;
            _context.Update(barrel);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = newStatus ? "სპირტის კასრი წარმატებით აღდგა!" : "სპირტის კასრი გადავიდა არქივში.";
            return RedirectToAction("Index", new { companyId = barrel.CompanyID });
        }

        // ==========================================
        // DELETE
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var barrel = await _context.Barrels
            .Include(b => b.Beverage)
            .FirstOrDefaultAsync(b => b.BarrelID == id);

            if (barrel == null) return NotFound();
            return View(barrel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var barrel = await _context.Barrels.FindAsync(id);
            if (barrel == null) return NotFound();

            int companyId = barrel.CompanyID;
            try
            {
                _context.Barrels.Remove(barrel);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "კასრი წარმატებით წაიშალა.";
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is SqlException sqlEx && sqlEx.Number == 51000)
                {
                    TempData["ErrorMessage"] = sqlEx.Message;
                }
                else
                {
                    TempData["ErrorMessage"] = "წაშლა ვერ მოხერხდა: " + ex.Message;
                }
                return RedirectToAction("Delete", new { id = id });
            }
            return RedirectToAction("Index", new { companyId = companyId });
        }

        // ==========================================
        // ARCHIVE (მარტივი სია + სტატუსის ცვლილება)
        // ==========================================
        public async Task<IActionResult> Archive(int companyId, string? search1, string? search2)
        {
            var company = await _context.Companies
                    .Include(c => c.CompanyType)
                    .FirstOrDefaultAsync(c => c.CompanyID == companyId);

            if (company == null) return NotFound("კომპანია ვერ მოიძებნა!");

            int workingMask = GetWorkingMask();

            var query = _context.Barrels
                .Include(b => b.Beverage).ThenInclude(x => x.ProductType)
                .Include(b => b.Beverage.Category)
                .Include(b => b.Beverage.Color)
                .Include(b => b.Beverage.Sweetness)
                .Where(b => b.CompanyID == companyId && (b.ProductTypeBitValue & workingMask) > 0)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search1))
                query = query.Where(b => b.Beverage.Name.Contains(search1));

            if (!string.IsNullOrEmpty(search2))
                query = query.Where(b => b.Beverage.Category.Name.Contains(search2));

            var barrels = await query
                .OrderBy(b => b.IsActive)
                .ThenByDescending(b => b.BarrelID)
                .ToListAsync();

            var viewModel = new CompanyBarrelDetailsViewModel
            {
                CompanyID = company.CompanyID,
                CompanyName = company.Name,
                CompanyType = company.CompanyType?.Name ?? "N/A",
                CompanyLot = company.CompanyLot ?? "",
                IdentifierCode = company.IdentifierCode ?? "",
                TotalStock = barrels.Where(b => b.IsActive).Sum(b => b.CurrentVolume),
                Barrels = barrels.Select(b => new BarrelViewModel
                {
                    BarrelID = b.BarrelID,
                    BeverageName = b.Beverage?.Name ?? "უცნობი",
                    ProductType = b.Beverage?.ProductType?.Name ?? "",
                    Category = b.Beverage?.Category?.Name ?? "",
                    Color = b.Beverage?.Color?.Name ?? "",
                    Sweetness = b.Beverage?.Sweetness?.Name ?? "",
                    CurrentVolume = b.CurrentVolume,
                    Year = b.Year,
                    IsActive = b.IsActive
                }).ToList()
            };

            ViewData["CurrentSearch1"] = search1;
            ViewData["CurrentSearch2"] = search2;

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id, string? search1, string? search2)
        {
            var barrel = await _context.Barrels.FindAsync(id);
            if (barrel == null) return NotFound();

            // სპირტის შემთხვევაში პირდაპირ ვააქტიურებთ, შემოწმების გარეშე
            barrel.IsActive = true;
            _context.Update(barrel);
            await _context.SaveChangesAsync();

            // TempData["SuccessMessage"] = "სპირტის კასრი წარმატებით გააქტიურდა!";

            return RedirectToAction(nameof(Archive), new { companyId = barrel.CompanyID, search1, search2 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id, string? search1, string? search2)
        {
            var barrel = await _context.Barrels.FindAsync(id);
            if (barrel == null) return NotFound();

            barrel.IsActive = false;
            _context.Update(barrel);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Archive), new { companyId = barrel.CompanyID, search1, search2 });
        }

        // ==========================================
        // HELPERS
        // ==========================================
        private async Task PopulateDropDowns(int workingMask)
        {
            ViewBag.Beverages = await _context.Beverages
                .Where(b => b.IsActive && (b.ProductType.BitValue & workingMask) > 0)
                .Include(b => b.ProductType)
                .Include(b => b.Category)
                .Include(b => b.SubCategory)
                .Include(b => b.Color)
                .Include(b => b.Sweetness)
                .OrderBy(b => b.ProductType.Position)
                .ThenBy(b => b.Category.Position)
                .ThenBy(b => b.SubCategory.Position)
                .ThenBy(b => b.Color.Position)
                .ThenBy(b => b.Sweetness.Position)
                .ThenBy(b => b.Name)
                .ToListAsync();

            // სპირტისთვის შეიძლება წელი იშვიათად იყოს გამოყენებული,
            // მაგრამ DropDown მაინც გვჭირდება (0 ნიშნავს "წლის გარეშე")
            ViewBag.VintageYears = Enumerable.Range(DateTime.Now.Year - 20, 15)
                .Reverse()
                .Select(y => new SelectListItem { Value = y.ToString(), Text = y.ToString() })
                .Append(new SelectListItem { Value = "0", Text = "წლის გარეშე" })
                .ToList();
        }
    }
}