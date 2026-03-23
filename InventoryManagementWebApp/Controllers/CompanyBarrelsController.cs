using InventoryManagementWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.Data;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Security.Claims; // დამატებულია Claim-ებისთვის

namespace InventoryManagementWebApp.Controllers
{
    [Authorize]
    public class CompanyBarrelsController : Controller
    {
        private readonly InventoryContext _context;

        public CompanyBarrelsController(InventoryContext context)
        {
            _context = context;
        }

        // დაემატა typeMask პარამეტრი
        public async Task<IActionResult> Index(int? typeMask, string? search1, string? search2)
        {
            // 1. ვიღებთ იუზერის სრულ (მაქსიმალურ) უფლებებს ლოგინიდან
            int userMask = 0;
            var maskClaim = User.FindFirst("AllowedProductsMask")?.Value;
            if (!string.IsNullOrEmpty(maskClaim))
            {
                int.TryParse(maskClaim, out userMask);
            }

            // 2. განვსაზღვროთ მიმდინარე სამუშაო ნიღაბი (Working Mask)
            int workingMask = userMask; // საწყისი მნიშვნელობა

            if (typeMask.HasValue)
            {
                // თუ იუზერმა მთავარი მენიუდან კონკრეტულ ღილაკს დააჭირა (მაგ: მოვიდა 4 ან 11)
                workingMask = userMask & typeMask.Value; // უსაფრთხოების ფილტრი

                // ვინახავთ ამ არჩევანს ბრაუზერის Cookie-ში 1 დღით
                HttpContext.Response.Cookies.Append(
                    "CurrentWorkingMask",
                    workingMask.ToString(),
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(1) }
                );
            }
            else
            {
                // თუ ლინკში typeMask არ არის (მაგ: ძებნის ღილაკს დააჭირა), ვკითხულობთ დამახსოვრებულს Cookie-დან
                if (HttpContext.Request.Cookies.TryGetValue("CurrentWorkingMask", out string? cookieMaskStr)
                    && int.TryParse(cookieMaskStr, out int cookieMask))
                {
                    workingMask = userMask & cookieMask; // აქაც ვამოწმებთ უსაფრთხოებას ყოველი შემთხვევისთვის
                }
            }

            // View-ს ვაწვდით მიმდინარე ნიღაბს (თუ სადმე დაგვჭირდა, მაგალითად სათაურის შესაცვლელად)
            ViewBag.CurrentTypeMask = workingMask;
            ViewBag.Search1 = search1;
            ViewBag.Search2 = search2;

            // SQL-ს ვაწვდით დამახსოვრებულ (ან ახალ) ნიღაბს
            var pMask = new SqlParameter("@ProductTypeMask", workingMask);
            var p1 = new SqlParameter("@Search1", search1 ?? (object)DBNull.Value);
            var p2 = new SqlParameter("@Search2", search2 ?? (object)DBNull.Value);

            var companiesWithBarrels = await _context.CompanyBarrelSummary
                .FromSqlRaw("EXEC [dbo].[CompanyBarrelSummary] @ProductTypeMask, @Search1, @Search2", pMask, p1, p2)
                .ToListAsync();

            return View(companiesWithBarrels);
        }
        public async Task<IActionResult> Details(int companyId)
        {
            // დეტალების გვერდზეც ვიღებთ იუზერის ნიღაბს, რომ შიგნით კასრებიც გაიფილტროს
            int userMask = 0;
            var maskClaim = User.FindFirst("AllowedProductsMask")?.Value;
            if (!string.IsNullOrEmpty(maskClaim)) int.TryParse(maskClaim, out userMask);

            var company = await _context.Companies
                .Include(c => c.CompanyType)
                .FirstOrDefaultAsync(c => c.CompanyID == companyId);

            if (company == null)
                return NotFound("კომპანია ვერ მოიძებნა!");

            // (ოფციონალური) თუ კასრების ჩამონათვალის გამოტანას გადაწყვეტთ, 
            // აქ უკვე დაცულია და მხოლოდ მისთვის ნებადართულ კასრებს წამოიღებს:
            /*
            var barrels = await _context.Barrels
                .Include(b => b.Beverage)
                .Where(b => b.CompanyID == companyId && b.IsActive == true && (b.ProductTypeBitValue & userMask) > 0)
                .ToListAsync();
            */

            var viewModel = new CompanyBarrelDetailsViewModel
            {
                CompanyID = company.CompanyID,
                CompanyName = company.Name,
                CompanyType = company.CompanyType?.Name ?? "N/A",
                // Barrels = barrels
            };

            return View(viewModel);
        }

        // ექსპორტის მეთოდშიც დაემატა typeMask
        public async Task<IActionResult> ExportToExcel(int? typeMask, string search1, string search2)
        {
            int userMask = 0;
            var maskClaim = User.FindFirst("AllowedProductsMask")?.Value;
            if (!string.IsNullOrEmpty(maskClaim)) int.TryParse(maskClaim, out userMask);

            int effectiveMask = typeMask.HasValue ? (userMask & typeMask.Value) : userMask;

            DataTable dt = new DataTable("Barrels");

            using (var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
            {
                using (var command = new SqlCommand("sp_GetAllBarrelsForExport", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    // აუცილებლად ვატანთ ნიღაბს ექსპორტსაც!
                    command.Parameters.AddWithValue("@ProductTypeMask", effectiveMask);
                    command.Parameters.AddWithValue("@Search1", (object)search1 ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Search2", (object)search2 ?? DBNull.Value);

                    connection.Open();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        dt.Load(reader);
                    }
                }
            }

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Barrels Export");

                var table = worksheet.Cell(1, 1).InsertTable(dt);
                table.Theme = XLTableTheme.None;

                worksheet.Columns().Width = 20;

                var header = worksheet.Row(1);
                header.Style.Font.Bold = true;
                header.Style.Fill.BackgroundColor = XLColor.FromHtml("#B3B3B3");
                header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                var dataRange = worksheet.RangeUsed();
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"Barrels_Export_{DateTime.Now:yyyy-MM-dd_HH-mm}.xlsx";

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
        }
    }
}