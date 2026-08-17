using InventoryManagementWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.Data;
using Microsoft.Data.SqlClient;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Linq;
using DocumentFormat.OpenXml.Wordprocessing;

namespace InventoryManagementWebApp.Controllers
{
    [Authorize]
    public class OperationsExportController : Controller
    {
        private readonly InventoryContext _context;

        public OperationsExportController(InventoryContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> ExportToExcel(int companyId, int? typeMask, string search1, string search2)
        {
            try
            {
                int userMask = 0;
                var maskClaim = User.FindFirst("AllowedProductsMask")?.Value;
                if (!string.IsNullOrEmpty(maskClaim)) int.TryParse(maskClaim, out userMask);

                int effectiveMask = typeMask.HasValue ? (userMask & typeMask.Value) : userMask;

                DataSet ds = new DataSet();

                using (var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
                {
                    using (var command = new SqlCommand("sp_GetCompanyOperationsForExport", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@CompanyID", companyId);
                        command.Parameters.AddWithValue("@ProductTypeMask", effectiveMask);
                        command.Parameters.AddWithValue("@Search1", (object)search1 ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Search2", (object)search2 ?? DBNull.Value);

                        connection.Open();
                        using (var adapter = new SqlDataAdapter(command))
                        {
                            adapter.Fill(ds);
                        }
                    }
                }

                if (ds.Tables.Count < 2 || ds.Tables[1].Rows.Count == 0)
                {
                    return Content("მონაცემები ვერ მოიძებნა.");
                }

                DataTable dtHeader = ds.Tables[0];
                DataTable dtData = ds.Tables[1];

                // 1. კომპანიის სახელი Sheet-ისთვის
                string rawCompanyName = dtHeader.Rows[0]["მეწარმე"]?.ToString() ?? "ოპერაციები";
                string sheetName = string.Concat(rawCompanyName.Where(c => !@"\/?*[]:".Contains(c)));
                if (sheetName.Length > 31) sheetName = sheetName.Substring(0, 31);

                // ამოვიღოთ ლოტის კოდი ფაილის სახელისთვის
                string rawLotCode = dtHeader.Rows.Count > 0 ? dtHeader.Rows[0]["ლოტი"]?.ToString() ?? "" : "";
                string lotCode = string.Concat(rawLotCode.Where(c => !@"\/?*[]:".Contains(c)));

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add(sheetName);

                    // 2. განლაგება TOP
                    worksheet.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

                    // Header ინფორმაცია
                    if (dtHeader.Rows.Count > 0)
                    {
                        var row = dtHeader.Rows[0];
                        worksheet.Cell("A1").Value = rawCompanyName;
                        worksheet.Cell("A1").Style.Font.Bold = true;
                        worksheet.Cell("A1").Style.Font.FontSize = 14;
                        worksheet.Cell("A2").Value = $"{row["ტიპი"]} | ლოტი: {row["ლოტი"]}";
                    }

                    // 3. ცხრილის ჩასმა (A4-დან)
                    var table = worksheet.Cell("A4").InsertTable(dtData);
                    table.Theme = XLTableTheme.None;

                    // 4. სათაურის (მე-4 სტრიქონის) გამუქება და გაყინვა
                    worksheet.Row(4).Style.Font.Bold = true;
                    worksheet.SheetView.FreezeRows(4);

                    // 5. ბორდერების და ხაზების დამატება (აქ გასწორდა AsRange!)
                    var tableRange = table.AsRange();
                    tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    tableRange.Style.Border.InsideBorderColor = XLColor.Black;

                    // 6. სვეტების სიგანის ხელით მითითება (ყველა სვეტი)
                    worksheet.Column(1).Width = 10;  // თარიღი
                    worksheet.Column(2).Width = 12;  // კასრის ID
                    worksheet.Column(2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Column(3).Width = 18;  // ტიპი
                    worksheet.Column(3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Column(4).Width = 21;  // კასრის წელი
                    worksheet.Column(4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Column(5).Width = 25;  // სასმელი
                    worksheet.Column(6).Width = 25;  // ფერი/შაქრიანობა
                    worksheet.Column(7).Width = 22;  // ოპერაცია
                    worksheet.Column(8).Width = 18;  // რაოდენობა
                    worksheet.Column(8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    worksheet.Column(9).Width = 18;  // ნაშთი
                    worksheet.Column(9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    worksheet.Column(10).Width = 15; // საბუთი
                    worksheet.Column(11).Width = 15; // საბუთის №
                    worksheet.Column(11).Style.Alignment.WrapText = true;
                    worksheet.Column(12).Width = 14; // წელი/ასაკი
                    worksheet.Column(12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Column(13).Width = 13; // ალკოჰოლი %
                    worksheet.Column(14).Width = 40; // ინფორმაცია წყაროზე
                    worksheet.Column(14).Style.Alignment.WrapText = true;
                    worksheet.Column(15).Width = 20; // შემსრულებელი
                    // 7. რიცხვითი ფორმატირება
                    int[] numericCols = { 8, 9, 13 };
                    foreach (int colIdx in numericCols)
                    {
                        table.Column(colIdx).Style.NumberFormat.Format = "# ##0.00";
                        table.Column(colIdx).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        table.Column(colIdx).Style.Alignment.Indent = 2;
                    }

                    // სათაურის პირველი 6 სვეტის (A4:F4) გაყვითლება
                    worksheet.Range("A4:F4").Style.Fill.BackgroundColor = XLColor.Yellow;

                    // სათაურის სტრიქონის (Row 4) გასწორება მარცხნივ
                    // ეს უნდა იყოს ბოლოს, რომ სვეტების Right-ალგორითმმა არ გადაფაროს
                    worksheet.Row(4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    worksheet.Row(4).Style.Alignment.Indent = 0;
                    worksheet.Row(4).Style.Font.Bold = true; // ბარემ აქაც იყოს გამუქება

                    // 8. ფერები მინუს და პლიუს ოპერაციებზე
                    var dataRange = table.DataRange;
                    foreach (var row in dataRange.Rows())
                    {
                        if (row.Cell(8).TryGetValue(out decimal qty))
                        {
                            if (qty < 0)
                                row.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFCCCC");
                            else if (qty > 0)
                                row.Style.Fill.BackgroundColor = XLColor.FromHtml("#CCFFCC");
                        }
                    }

                    // განსაზღვრეთ დასახელება მასკის მიხედვით
                    string typeLabel = effectiveMask == 11 ? "ღვინო" : (effectiveMask == 20 ? "სპირტი" : "ოპერაციები");

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();

                        // ფაილის სახელი: ჯერ ლოტი, მერე კომპანია, ტიპი და თარიღი
                        string fileName = !string.IsNullOrEmpty(lotCode)
                            ? $"{lotCode}_{sheetName}_{typeLabel}_{DateTime.Now:yyyyMMddhhmm}.xlsx"
                            : $"{sheetName}_{typeLabel}_{DateTime.Now:yyyyMMddhhmm}.xlsx";

                        return File(
                            content,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                return Content($"შეცდომა ექსპორტისას: {ex.Message}");
            }
        }
    }
}