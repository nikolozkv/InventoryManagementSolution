using InventoryManagementWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Text.Json;

namespace InventoryManagementWebApp.Controllers
{
    [Authorize]
    public class Operations_WineController : Controller
    {
        private readonly InventoryContext _context;

        public Operations_WineController(InventoryContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int barrelId)
        {
            var barrel = await _context.Barrels
                .Include(b => b.Company).ThenInclude(c => c.CompanyType)
                .Include(b => b.Beverage).ThenInclude(bv => bv.ProductType)
                .Include(b => b.Beverage).ThenInclude(bv => bv.Category)
                .Include(b => b.Beverage).ThenInclude(bv => bv.SubCategory)
                .Include(b => b.Beverage).ThenInclude(bv => bv.Color)
                .Include(b => b.Beverage).ThenInclude(bv => bv.Sweetness)
                .FirstOrDefaultAsync(b => b.BarrelID == barrelId);

            if (barrel == null)
                return NotFound("კასრი ვერ მოიძებნა.");

            ViewBag.OperationDefinitions = await _context.OperationDefinitions
                .Where(o => o.IsActive == true)
                .Select(o => new SelectListItem { Text = o.Name, Value = o.OperationDefID.ToString() })
                .ToListAsync();

            ViewBag.OperationDefinitionsFull = await _context.OperationDefinitions
                .Where(o => o.IsActive == true)
                .Select(o => new { o.OperationDefID, o.Name, o.OperType, o.PreserveBarrelState })
                .ToListAsync();

            ViewBag.DocumentTypes = await _context.DocumentTypes
                .Where(d => d.IsActive)
                .Select(d => new SelectListItem { Text = d.DocumentName, Value = d.DocumentTypeID.ToString() })
                .ToListAsync();

            ViewBag.PercentList = Enumerable.Range(85, 16).Select(i => new SelectListItem
                {
                    Value = (100 - (i - 85)).ToString(), // 100, 99, ..., 85
                    Text = (100 - (i - 85)).ToString()
                })
                .Append(new SelectListItem { Value = "0", Text = "%-ის გარეშეც" })
                .ToList();

            var operations = await _context.Operations
                .Where(o => o.BarrelID == barrelId)
                .Include(o => o.OperationDefinition)
                .Include(o => o.DocumentType)
                .Include(o => o.Beverage).ThenInclude(b => b.ProductType)
                .Include(o => o.Beverage).ThenInclude(b => b.Category)
                .Include(o => o.Beverage).ThenInclude(b => b.Color)
                .Include(o => o.Beverage).ThenInclude(b => b.Sweetness)
                .OrderByDescending(o => o.TransactionDate)
                .ThenBy(o => o.Math == "-" ? 0 : 1)
                .ThenByDescending(o => o.OperationID)
                .ToListAsync();

            // წლების სია ბოლო 20 წელზე
            ViewBag.VintageYears = Enumerable.Range(DateTime.Now.Year - 20, 21).Reverse().ToList();

            // Get all unique user IDs from operations to fetch user data
            var userIds = operations.Where(o => o.ExecutedByUserID.HasValue)
                                   .Select(o => o.ExecutedByUserID.Value)
                                   .Distinct()
                                   .ToList();

            // Get all unique source company IDs from operations to fetch company data
            var sourceCompanyIds = operations.Where(o => o.SourceCompanyID.HasValue)
                                           .Select(o => o.SourceCompanyID.Value)
                                           .Distinct()
                                           .ToList();

            // Fetch user data using raw SQL query to get FirstName and LastName
            var userData = new Dictionary<int, (string, string)>();
            if (userIds.Any())
            {
                using var conn = new SqlConnection(_context.Database.GetConnectionString());
                conn.Open();
                var userIdParams = string.Join(",", userIds);
                var cmd = new SqlCommand($"SELECT UserId, FirstName, LastName FROM Users WHERE UserId IN ({userIdParams})", conn);
                
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var userId = reader.GetInt32("UserId");
                    var firstName = reader.IsDBNull("FirstName") ? "" : reader.GetString("FirstName");
                    var lastName = reader.IsDBNull("LastName") ? "" : reader.GetString("LastName");
                    userData[userId] = (firstName, lastName);
                }
            }

            // Fetch source company data using raw SQL query to get Company Names
            var sourceCompanyData = new Dictionary<int, string>();
            if (sourceCompanyIds.Any())
            {
                using var conn = new SqlConnection(_context.Database.GetConnectionString());
                conn.Open();
                var companyIdParams = string.Join(",", sourceCompanyIds);
                var cmd = new SqlCommand($"SELECT CompanyID, Name FROM Companies WHERE CompanyID IN ({companyIdParams})", conn);
                
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var companyId = reader.GetInt32("CompanyID");
                    var companyName = reader.IsDBNull("Name") ? "" : reader.GetString("Name");
                    sourceCompanyData[companyId] = companyName;
                }
            }

            var viewModel = new OperationsPageViewModel
            {
                BarrelID = barrel.BarrelID,
                CompanyID = barrel.CompanyID,
                CompanyName = barrel.Company.Name,
                CompanyTypeName = barrel.Company.CompanyType?.Name,
                CompanyLot = barrel.Company.CompanyLot,
                BeverageName = barrel.Beverage.Name,
                HarvestYear = barrel.Year ?? 0,
                CurrentVolume = barrel.CurrentVolume,
                YearPercentage = barrel.YearPercentage ?? 0,
                PurePercentage = barrel.PurePercentage ?? 0,
                ProductType = barrel.Beverage.ProductType?.Name,
                Category = barrel.Beverage.Category?.Name,
                Color = barrel.Beverage.Color?.Name,
                Sweetness = barrel.Beverage.Sweetness?.Name,
                Operations = operations.Select(o => {
                    string firstName = "";
                    string lastName = "";
                    
                    if (o.ExecutedByUserID.HasValue && userData.ContainsKey(o.ExecutedByUserID.Value))
                    {
                        var userInfo = userData[o.ExecutedByUserID.Value];
                        firstName = userInfo.Item1;
                        lastName = userInfo.Item2;
                    }

                    string sourceCompanyName = "";
                    if (o.SourceCompanyID.HasValue && sourceCompanyData.ContainsKey(o.SourceCompanyID.Value))
                    {
                        sourceCompanyName = sourceCompanyData[o.SourceCompanyID.Value];
                    }
                    
                    return new OperationItemViewModel
                    {
                        OperationID = o.OperationID,
                        TransactionDate = o.TransactionDate,
                        OperName = o.OperName,
                        DocumentNumber = o.DocumentNumber,
                        DocumentTypeName = o.DocumentType?.DocumentName,
                        Quantity = (o.Math == "-" ? "-" : "+") + o.Quantity.ToString("0.##"),
                        VolumeLeft = o.VolumeLeft,
                        HarvestYear = o.HarvestYear,
                        YearPercentage = o.YearAPercent,
                        PurePercentage = o.PureAPercent,
                        BeverageName = o.Beverage?.Name,
                        ProductType = o.Beverage?.ProductType?.Name,
                        Category = o.Beverage?.Category?.Name,
                        Color = o.Beverage?.Color?.Name,
                        Sweetness = o.Beverage?.Sweetness?.Name,
                        ExecutedByUserID = o.ExecutedByUserID,
                        ExecutedByUserFirstName = firstName,
                        ExecutedByUserLastName = lastName,
                        LinkedOperationID = o.LinkedOperationID,
                        SourceCompanyID = o.SourceCompanyID,
                        SourceBarrelID = o.SourceBarrelID,
                        SourceCompanyName = sourceCompanyName
                    };
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateWineOperationViewModel model)
        {
            if (model.HarvestYear > DateTime.Now.Year)
            {
                TempData["Message"] = "შერჩეული ღვინის მოსავლის წელი არ შეიძლება იყოს მომავალში.";
                TempData["Status"] = "danger";
                // Preserve form data
                TempData["FormData"] = System.Text.Json.JsonSerializer.Serialize(model);
                return RedirectToAction("Index", new { barrelId = model.BarrelID });
            }

            try
            {
                // Fetch UserID based on username
                var currentUserName = User.Identity?.Name;
                if (!string.IsNullOrEmpty(currentUserName))
                {
                    using var conn = new SqlConnection(_context.Database.GetConnectionString());
                    conn.Open();

                    var cmd = new SqlCommand("SELECT UserId FROM Users WHERE Username = @username", conn);
                    cmd.Parameters.AddWithValue("@username", currentUserName);

                    var result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        model.ExecutedByUserID = Convert.ToInt32(result);
                    }
                }

                var parameters = new[]
                {
                    new SqlParameter("@OperationDefID", model.OperationDefID),
                    new SqlParameter("@BarrelID", model.BarrelID),
                    new SqlParameter("@Quantity", model.Quantity),
                    new SqlParameter("@DocumentNumber", model.DocumentNumber ?? ""),
                    new SqlParameter("@DocumentTypeID", model.DocumentTypeID),
                    new SqlParameter("@ExecutedByUserID", (object?)model.ExecutedByUserID ?? DBNull.Value),
                    new SqlParameter("@TransactionDate", model.TransactionDate),
                    new SqlParameter("@BeverageID", (object?)model.BeverageID ?? DBNull.Value),
                    new SqlParameter("@HarvestYear", (object?)model.HarvestYear ?? DBNull.Value),
                    new SqlParameter("@YearAPercent", (object?)model.YearAPercent ?? DBNull.Value),
                    new SqlParameter("@PureAPercent", (object?)model.PureAPercent ?? DBNull.Value),
                    new SqlParameter("@OppositeBarrelID", (object?)model.OppositeBarrelID ?? DBNull.Value),
                    new SqlParameter("@OppositeCompanyID", (object?)model.OppositeCompanyID ?? DBNull.Value),
                    new SqlParameter { ParameterName = "@NewOperationID", SqlDbType = SqlDbType.Int, Direction = ParameterDirection.Output },
                    new SqlParameter { ParameterName = "@Message", SqlDbType = SqlDbType.NVarChar, Size = 200, Direction = ParameterDirection.Output }
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC ExecuteOperation @OperationDefID, @BarrelID, @Quantity, @DocumentNumber, @DocumentTypeID, @ExecutedByUserID, @TransactionDate, @BeverageID, @HarvestYear, @YearAPercent, @PureAPercent, @OppositeBarrelID, @OppositeCompanyID, @NewOperationID OUTPUT, @Message OUTPUT",
                    parameters);

                var message = parameters[14].Value?.ToString();
                TempData["Message"] = message;
                var isSuccess = message?.Contains("წარმატ") == true;
                TempData["Status"] = isSuccess ? "success" : "warning";
                
                // Only preserve form data if there was an error/warning
                if (!isSuccess)
                {
                    TempData["FormData"] = System.Text.Json.JsonSerializer.Serialize(model);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}"); // Log the exception
                TempData["Message"] = "შეცდომა ოპერაციის შექმნისას: " + ex.Message;
                TempData["Status"] = "danger";
                // Preserve form data on exception
                TempData["FormData"] = System.Text.Json.JsonSerializer.Serialize(model);
            }

            return RedirectToAction("Index", new { barrelId = model.BarrelID });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int operationId)
        {
            int? barrelId = await _context.Operations
                .Where(o => o.OperationID == operationId)
                .Select(o => (int?)o.BarrelID)
                .FirstOrDefaultAsync();

            if (barrelId == null)
                return NotFound();

            var msgParam = new SqlParameter("@Message", SqlDbType.NVarChar, 200) { Direction = ParameterDirection.Output };
            var opIdParam = new SqlParameter("@OperationID", operationId);

            await _context.Database.ExecuteSqlRawAsync("EXEC dbo.DeleteOperation @OperationID, @Message OUTPUT", opIdParam, msgParam);

            var message = msgParam.Value?.ToString();
            TempData["DeleteMessage"] = message;

            // 👇 აქ შეცვალე "danger" => "error"
            if (message != null && message.Contains("წაიშალა"))
                TempData["DeleteStatus"] = "success";
            else if (message != null && (message.Contains("ვერ მოიძებნა") || message.Contains("უარყვნილი")))
                TempData["DeleteStatus"] = "warning";
            else
                TempData["DeleteStatus"] = "error";

            return RedirectToAction(nameof(Index), new { barrelId = barrelId.Value });
        }

        [HttpGet("/api/operations/filtered-companies")]
        public async Task<IActionResult> GetFilteredCompanies(int barrelId)
        {
            var result = new List<SelectListItem>();

            using (var conn = _context.Database.GetDbConnection() as SqlConnection)
            using (var cmd = new SqlCommand("GetFilteredCompanyForTransfer", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@BarrelID", barrelId);

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new SelectListItem
                        {
                            Value = reader["CompanyID"].ToString(),
                            Text = reader["NameWithCount"].ToString()
                        });
                    }
                }
            }

            return Json(result.Select(x => new { value = x.Value, text = x.Text }));
        }

        [HttpGet("/api/operations/filtered-barrels")]
        public async Task<IActionResult> GetFilteredBarrels(int barrelId, int operType, int? companyId)
        {
            var result = new List<SelectListItem>();

            using (var conn = _context.Database.GetDbConnection() as SqlConnection)
            using (var cmd = new SqlCommand("GetFilteredBarrelsForTransfer", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@BarrelID", barrelId);
                cmd.Parameters.AddWithValue("@OperType", operType);
                cmd.Parameters.AddWithValue("@OppositeCompanyID", (object?)companyId ?? DBNull.Value);

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new SelectListItem
                        {
                            Value = reader["BarrelID"].ToString(),
                            Text = $"{reader["BeverageName"]} ({reader["Year"]} წ.) - {reader["CurrentVolume"]} ლ"
                        });
                    }
                }
            }

            return Json(result.Select(x => new { value = x.Value, text = x.Text }));
        }

        [HttpGet("/api/operations/get-data-by-transdate")]
        public async Task<IActionResult> GetDataByTransDate(int operType, int barrelId, int? oppositeBarrelId, DateTime transactionDate)
        {
            int workBarrelId = operType == 1 ? barrelId : oppositeBarrelId ?? 0;
            if (workBarrelId == 0)
                return Json(new { volume = 0 });

            // ძირითადი barrel-ის მონაცემები
            int beverageId = 0;
            string beverageName = "";
            string productType = "";
            string category = "";
            string color = "";
            string sweetness = "";
            decimal volume = 0;
            decimal yearPercent = 0;
            decimal purePercent = 0;

            decimal yearLimit = 0;
            decimal pureLimit = 0;
            bool allowMix = false;

            using (var conn = _context.Database.GetDbConnection() as SqlConnection)
            using (var cmd = new SqlCommand("Get_Data_By_TransDate", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@BarrelID", barrelId);
                cmd.Parameters.AddWithValue("@WorkBarrelID", workBarrelId);
                cmd.Parameters.AddWithValue("@OperType", operType);
                cmd.Parameters.AddWithValue("@TransactionDate", transactionDate);

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    // პირველი Select – ძირითადი Barrel-ის მონაცემები
                    if (await reader.ReadAsync())
                    {
                        beverageId = Convert.ToInt32(reader["BeverageID"]);
                        beverageName = reader["BeverageName"]?.ToString();
                        productType = reader["ProductType"]?.ToString();
                        category = reader["Category"]?.ToString();
                        color = reader["Color"]?.ToString();
                        sweetness = reader["Sweetness"]?.ToString();
                        volume = Convert.ToDecimal(reader["VolumeLeft"]);
                        yearPercent = Convert.ToDecimal(reader["YearPercentage"]);
                        purePercent = Convert.ToDecimal(reader["PurePercentage"]);
                    }
                    // მეორე Select – YearLimit, PureLimit, AllowMix
                    if (await reader.NextResultAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            yearLimit = reader["YearLimit"] != DBNull.Value ? Convert.ToDecimal(reader["YearLimit"]) : 0;
                            pureLimit = reader["PureLimit"] != DBNull.Value ? Convert.ToDecimal(reader["PureLimit"]) : 0;
                            allowMix = reader["AllowMix"] != DBNull.Value && Convert.ToBoolean(reader["AllowMix"]);
                        }
                    }
                }
            }

            return Json(new
            {
                beverageId,
                beverageName,
                productType,
                category,
                color,
                sweetness,
                volume,
                yearPercent,
                purePercent,
                yearLimit,
                pureLimit,
                allowMix
            });
        }

        [HttpGet("/api/operations/document-types")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDocumentTypes()
        {
            try
            {
                var documentTypes = await _context.DocumentTypes
                    .Where(d => d.IsActive)
                    .Select(d => new { value = d.DocumentTypeID, text = d.DocumentName })
                    .ToListAsync();

                return Json(documentTypes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching document types: {ex.Message}");
                return Json(new List<object>());
            }
        }
    }
}
