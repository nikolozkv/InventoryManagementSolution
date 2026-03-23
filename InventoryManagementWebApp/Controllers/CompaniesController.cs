using InventoryManagementWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementWebApp.Controllers
{
    [Authorize]
    public class CompaniesController : Controller
    {
        private readonly InventoryContext _context;

        public CompaniesController(InventoryContext context)
        {
            _context = context;
        }

        // ყველა კომპანიას ხილვა
        public async Task<IActionResult> Index(string? search)
        {
            // მონაცემთა ბაზაში ყველა კომპანიის წამოღება და შემდგომი ფილტრაცია, თუ ძებნა შეივსება
            var query = _context.Companies
                .Include(c => c.CompanyType)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c =>
                    c.Name.Contains(search) ||
                    (c.IdentifierCode != null && c.IdentifierCode.Contains(search)) ||
                    (c.CompanyLot != null && c.CompanyLot.Contains(search)) ||
                    (c.ContactInfo != null && c.ContactInfo.Contains(search)) ||
                    (c.Address != null && c.Address.Contains(search))
                );
            }
            ///////////
            var companies = await query.ToListAsync();
            return View(companies);
        }

        // კომპანიის დეტალების ჩვენება
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var company = await _context.Companies
                .Include(c => c.CompanyType)
                .FirstOrDefaultAsync(m => m.CompanyID == id);
            if (company == null) return NotFound();
            return View(company);
        }

        // ახალი კომპანიის დამატების ფორმა
        public IActionResult CompanyAdd()
        {
            ViewData["CompanyTypeID"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.CompanyTypes, "CompanyTypeID", "FullName");
            return View();
        }

        // კომპანიის დამატება Stored Procedure–ის გამოძახებით
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompanyAdd(string? CompanyLot, string Name, int CompanyTypeID, string IdentifierCode, string ContactInfo, string Address, DateTime? StartDate)
        {
            try
            {
                var reactivateParam = new SqlParameter
                {
                    ParameterName = "@Reactivate",
                    SqlDbType = System.Data.SqlDbType.Bit,
                    Direction = System.Data.ParameterDirection.Output
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC [dbo].[CompanyAdd] @CompanyLot, @Name, @CompanyTypeID, @IdentifierCode, @ContactInfo, @Address, @StartDate, @Reactivate OUT",
                    new SqlParameter("@CompanyLot", CompanyLot ?? (object)DBNull.Value),
                    new SqlParameter("@Name", Name),
                    new SqlParameter("@CompanyTypeID", CompanyTypeID),
                    new SqlParameter("@IdentifierCode", IdentifierCode),
                    new SqlParameter("@ContactInfo", ContactInfo ?? (object)DBNull.Value),
                    new SqlParameter("@Address", Address ?? (object)DBNull.Value),
                    new SqlParameter("@StartDate", StartDate ?? DateTime.Now),
                    reactivateParam
                );

                if ((bool)reactivateParam.Value)
                {
                    var inactiveCompany = await _context.Companies.FirstOrDefaultAsync(c =>
                        (c.IdentifierCode == IdentifierCode || c.CompanyLot == CompanyLot) && c.IsActive == false);
                    if (inactiveCompany != null)
                    {
                        TempData["Reactivate"] = true;
                        TempData["CompanyID"] = inactiveCompany.CompanyID;
                        return RedirectToAction(nameof(CompanyReactivate), new { id = inactiveCompany.CompanyID });
                    }
                }

                TempData["SuccessMessage"] = "ჩანაწერი წარმატებით დაემატა.";
                return RedirectToAction(nameof(Index));
            }
            catch (SqlException ex) when (ex.Number == 50001)
            {
                ModelState.AddModelError("", "ჩანაწერი ასეთი ID ან ლოტით უკვე არსებობს.");
                ViewData["CompanyTypeID"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.CompanyTypes, "CompanyTypeID", "Name", CompanyTypeID);
                return View();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Cannot add company: " + ex.Message);
                ViewData["CompanyTypeID"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.CompanyTypes, "CompanyTypeID", "Name", CompanyTypeID);
                return View();
            }
        }

        // კომპანიის რეაქტივაციის გვერდი
        [HttpGet]
        public async Task<IActionResult> CompanyReactivate(int? id)
        {
            if (id == null) return NotFound();
            var company = await _context.Companies
                .Include(c => c.CompanyType)
                .FirstOrDefaultAsync(c => c.CompanyID == id && c.IsActive == false);
            if (company == null) return NotFound();
            return View(company);
        }

        // კომპანიის რეაქტივაცია Stored Procedure–ის გამოძახებით
        [HttpPost, ActionName("CompanyReactivate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompanyReactivateConfirmed(int id)
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC [dbo].[CompanyReactivate] @CompanyID",
                    new SqlParameter("@CompanyID", id)
                );
                TempData["SuccessMessage"] = "ჩანაწერი წარმატებით გააქტიურდა.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "რეაქტივაცია ვერ მოხერხდა: " + ex.Message);
                return RedirectToAction(nameof(CompanyReactivate), new { id });
            }
        }

        // კომპანიის რედაქტირება
        public async Task<IActionResult> CompanyEdit(int? id)
        {
            if (id == null) return NotFound();
            var company = await _context.Companies.FindAsync(id);
            if (company == null) return NotFound();
            ViewData["CompanyTypeID"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.CompanyTypes, "CompanyTypeID", "FullName", company.CompanyTypeID);
            return View(company);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompanyEdit(
            int CompanyID,
            string? CompanyLot,
            string Name,
            int CompanyTypeID,
            string IdentifierCode,
            string ContactInfo,
            string Address,
            DateTime? StartDate)
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC [dbo].[CompanyEdit] @CompanyID, @CompanyLot, @Name, @CompanyTypeID, @IdentifierCode, @ContactInfo, @Address, @StartDate",
                    new SqlParameter("@CompanyID", CompanyID),
                    new SqlParameter("@CompanyLot", CompanyLot ?? (object)DBNull.Value),
                    new SqlParameter("@Name", Name),
                    new SqlParameter("@CompanyTypeID", CompanyTypeID),
                    new SqlParameter("@IdentifierCode", IdentifierCode),
                    new SqlParameter("@ContactInfo", ContactInfo ?? (object)DBNull.Value),
                    new SqlParameter("@Address", Address ?? (object)DBNull.Value),
                    new SqlParameter("@StartDate", StartDate ?? DateTime.Now)
                );
                TempData["SuccessMessage"] = "ჩანაწერი წარმატებით განახლდა.";
                return RedirectToAction(nameof(Index));
            }
            catch (SqlException ex) when (ex.Number == 50002)
            {
                ModelState.AddModelError("", "ჩანაწერი მსგავსი ID ან ლოტით უკვე არსებობს.");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "ცვლილებების შეტანა ვერ მოხერხდა: " + ex.Message);
            }
            ViewData["CompanyTypeID"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.CompanyTypes, "CompanyTypeID", "FullName", CompanyTypeID);
            return View();
        }

        // კომპანიის არქივაციის გვერდი
        public async Task<IActionResult> CompanyArchive(int? id)
        {
            if (id == null) return NotFound();
            var company = await _context.Companies.Include(c => c.CompanyType)
                .FirstOrDefaultAsync(m => m.CompanyID == id);
            if (company == null) return NotFound();
            return View("CompanyArchive", company);
        }

        [HttpPost, ActionName("CompanyArchive")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompanyArchiveConfirmed(int id)
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC [dbo].[CompanyArchive] @CompanyID, @EndDate",
                    new SqlParameter("@CompanyID", id),
                    new SqlParameter("@EndDate", DateTime.Now)
                );
                TempData["SuccessMessage"] = "ჩანაწერი წარმატებით დაარქივდა.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "არქივში გადატანა ვერ მოხერხდა: " + ex.Message);
                return RedirectToAction(nameof(CompanyArchive), new { id });
            }
        }
    }
}
