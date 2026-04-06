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

namespace InventoryManagementWebApp.Controllers
{
    [Authorize]
    public class Operations_SpiritsController : Controller
    {
        private readonly InventoryContext _context;

        public Operations_SpiritsController(InventoryContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int barrelId)
        {
            // ✅ პირდაპირ ვაფიქსირებთ სპირტის ფერს
            ViewBag.CurrentThemeColor = "#e9f2fa";

            var barrel = await _context.Barrels
                .Include(b => b.Company).ThenInclude(c => c.CompanyType)
                .Include(b => b.Beverage).ThenInclude(bv => bv.ProductType)
                .Include(b => b.Beverage).ThenInclude(bv => bv.Category)
                .Include(b => b.Beverage).ThenInclude(bv => bv.SubCategory)
                .Include(b => b.Beverage).ThenInclude(bv => bv.Color)
                .Include(b => b.Beverage).ThenInclude(bv => bv.Sweetness)
                .FirstOrDefaultAsync(b => b.BarrelID == barrelId);

            if (barrel == null)
                return NotFound("სპირტის კასრი ვერ მოიძებნა.");

            // ვიღებთ სასმელის BitValue-ს (სპირტისთვის იქნება 4)
            // თუ რატომღაც ცარიელია, 0-ით დავაზღვევთ რომ ერორი არ ამოაგდოს
            // int productBitValue = 4; // ტესტისთვის, რომ დარწმუნდე ფილტრი მუშაობს. რეალურად უნდა იყოს სასმელის BitValue 
            // int productBitValue = barrel.ProductTypeBitValue is int pbv ? pbv : 0;

            // 1. DropDown-ისთვის განკუთვნილი სია
            // ✅ OperationDefinition ვიღებთ ბიტს კატეგორიიდან და არა პროდუქტის ტიპიდან
            int categoryBitValue = barrel.Beverage?.Category?.BitValue ?? 0;

            // 2. DropDown-ის სია (უკვე შეცვლილი გაქვს)
            ViewBag.OperationDefinitions = await _context.OperationDefinitions
                .Where(o => o.IsActive == true && (o.TypeCodeMask & categoryBitValue) > 0)
                .OrderBy(o => o.Position)
                .Select(o => new SelectListItem { Text = o.Name, Value = o.OperationDefID.ToString() })
                .ToListAsync();

            // 3. 🆕 JavaScript-ის სრული ობიექტი - აქაც categoryBitValue უნდა იყოს!
            ViewBag.OperationDefinitionsFull = await _context.OperationDefinitions
                .Where(o => o.IsActive == true && (o.TypeCodeMask & categoryBitValue) > 0)
                .Select(o => new { o.OperationDefID, o.Name, o.OperType, o.PreserveBarrelState })
                .ToListAsync();

            ViewBag.DocumentTypes = await _context.DocumentTypes
                .Where(d => d.IsActive)
                .Select(d => new SelectListItem { Text = d.DocumentName, Value = d.DocumentTypeID.ToString() })
                .ToListAsync();

            var operations = await _context.Operations
                    .Where(o => o.BarrelID == barrelId)
                    .Include(o => o.OperationDefinition)
                    .Include(o => o.DocumentType)
                    .Include(o => o.Beverage).ThenInclude(b => b.ProductType)
                    .Include(o => o.Beverage).ThenInclude(b => b.Category)
                    .Include(o => o.Beverage).ThenInclude(b => b.Color)
                    .Include(o => o.Beverage).ThenInclude(b => b.Sweetness)
                    .OrderByDescending(o => o.TransactionDate)
                    .ThenBy(o => o.CalcOrder)
                    .ThenByDescending(o => o.OperationID)
                    .ToListAsync();

            var userIds = operations.Where(o => o.ExecutedByUserID.HasValue)
                                   .Select(o => o.ExecutedByUserID.Value)
                                   .Distinct()
                                   .ToList();

            var sourceCompanyIds = operations.Where(o => o.SourceCompanyID.HasValue)
                                             .Select(o => o.SourceCompanyID.Value)
                                             .Distinct()
                                             .ToList();

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

            var viewModel = new OperationsSpiritPageViewModel
            {
                BarrelID = barrel.BarrelID,
                CompanyID = barrel.CompanyID,
                CompanyName = barrel.Company.Name,
                CompanyTypeName = barrel.Company.CompanyType?.Name,
                CompanyLot = barrel.Company.CompanyLot,

                BeverageID = barrel.BeverageID,
                BeverageName = barrel.Beverage.Name,
                ProductType = barrel.Beverage.ProductType?.Name,
                Category = barrel.Beverage.Category?.Name,
                Color = barrel.Beverage.Color?.Name,
                Sweetness = barrel.Beverage.Sweetness?.Name,

                CurrentVolume = barrel.CurrentVolume, // ✅ დაემატა "m" 
                WeightedAvgDate = barrel.WeightedAvgDate,
                CurrentAlcPercent = barrel.CurrentAlcPercent,

                // ✅ გასწორდა Select-ის ლოგიკა (დაემატა ბლოკი { ... return new ... })
                Operations = operations.Select(o =>
                {
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

                        BeverageName = o.Beverage?.Name,
                        ProductType = o.Beverage?.ProductType?.Name,
                        Category = o.Beverage?.Category?.Name,
                        Color = o.Beverage?.Color?.Name,
                        Sweetness = o.Beverage?.Sweetness?.Name,
                        ExecutedByUserID = o.ExecutedByUserID,
                        ExecutedByUserFirstName = firstName,     // ✅ ახლა უკვე დაინახავს ცვლადს
                        ExecutedByUserLastName = lastName,       // ✅ ახლა უკვე დაინახავს ცვლადს
                        LinkedOperationID = o.LinkedOperationID,
                        SourceCompanyID = o.SourceCompanyID,
                        SourceBarrelID = o.SourceBarrelID,
                        SourceCompanyName = sourceCompanyName    // ✅ ახლა უკვე დაინახავს ცვლადს
                    };
                }).ToList()
            };

            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateSpiritOperationViewModel model)
        {
            try
            {
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
                    new SqlParameter("@TransactionDate", model.TransactionDate),
                    new SqlParameter("@WineAlcPercent", (object?)model.WineAlcPercent ?? DBNull.Value),
                    new SqlParameter("@LossPercent", (object?)model.LossPercent ?? DBNull.Value),
                    new SqlParameter("@OppositeBarrelID", (object?)model.OppositeBarrelID ?? DBNull.Value),
                    new SqlParameter("@DocumentNumber", model.DocumentNumber ?? ""),
                    new SqlParameter("@DocumentTypeID", model.DocumentTypeID),
                    new SqlParameter("@ExecutedByUserID", (object?)model.ExecutedByUserID ?? DBNull.Value),
                    new SqlParameter { ParameterName = "@NewOperationID", SqlDbType = SqlDbType.Int, Direction = ParameterDirection.Output },
                    new SqlParameter { ParameterName = "@Message", SqlDbType = SqlDbType.NVarChar, Size = 200, Direction = ParameterDirection.Output }
                };

                // აქ ვიძახებთ სპირტის სპეციალურ პროცედურას!
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC dbo.Spirit_ExecuteOperation @OperationDefID, @BarrelID, @Quantity, @TransactionDate, @WineAlcPercent, @LossPercent, @OppositeBarrelID, @DocumentNumber, @DocumentTypeID, @ExecutedByUserID, @NewOperationID OUTPUT, @Message OUTPUT",
                    parameters);

                var message = parameters.First(p => p.ParameterName == "@Message").Value?.ToString();
                var newOpIdParam = parameters.First(p => p.ParameterName == "@NewOperationID").Value;
                TempData["Message"] = message;
                var isSuccess = message?.Contains("წარმატ") == true;
                TempData["Status"] = isSuccess ? "success" : "warning";

                if (!isSuccess)
                {
                    TempData["FormData"] = System.Text.Json.JsonSerializer.Serialize(model);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                TempData["Message"] = "შეცდომა ოპერაციის შექმნისას: " + ex.Message;
                TempData["Status"] = "danger";
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

            // ვიყენებთ არსებულ DeleteOperation პროცედურას, რადგან ის უნივერსალურია და 
            // წაშლის შემდეგ გადათვლისთვის RecalculateRippleEffect-ს იძახებს. 
            // (შენიშვნა: თუ სპირტისთვის ცალკე Spirit_DeleteOperation გაქვს ბაზაში, აქ ის უნდა გამოიძახო)
            await _context.Database.ExecuteSqlRawAsync("EXEC dbo.DeleteOperation @OperationID, @Message OUTPUT", opIdParam, msgParam);

            var message = msgParam.Value?.ToString();
            TempData["DeleteMessage"] = message;

            if (message != null && message.Contains("წაიშალა"))
                TempData["DeleteStatus"] = "success";
            else if (message != null && (message.Contains("ვერ მოიძებნა") || message.Contains("უარყვნილი")))
                TempData["DeleteStatus"] = "warning";
            else
                TempData["DeleteStatus"] = "error";

            return RedirectToAction(nameof(Index), new { barrelId = barrelId.Value });
        }

        // --- API მარშრუტები შეცვლილია /api/operations-spirit/... კონფლიქტის ასარიდებლად ---

        [HttpGet("/api/operations-spirit/filtered-companies")]
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

        //1. კასრების ფილტრაციის API(სპირტის ლოგიკით)
        //აქ ვიძახებთ ახალ Spirit_GetFilteredBarrelsForTransfer პროცედურას.

        [HttpGet("/api/operations-spirit/filtered-barrels")]
        public async Task<IActionResult> GetFilteredBarrels(int barrelId, int operType, int? companyId)
        {
            var result = new List<SelectListItem>();

            using (var conn = _context.Database.GetDbConnection() as SqlConnection)
            using (var cmd = new SqlCommand("Spirit_GetFilteredBarrelsForTransfer", conn)) // ✅ ახალი პროცედურა
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
                            // ✅ ტექსტში შეგვიძლია წელი ამოვიღოთ, ან დავტოვოთ თუ გინდა ჩანდეს
                            Text = $"{reader["BeverageName"]} - {reader["CurrentVolume"]} ლ"
                        });
                    }
                }
            }

            return Json(result.Select(x => new { value = x.Value, text = x.Text }));
        }

        //2. თარიღით მონაცემების წამოღების API(ჭკვიანი ნაშთით)
        //  აქ ვიძახებთ Spirit_Get_Data_By_TransDate - ს,
        //  რომელიც გვიბრუნებს MaxAllowed(მომავლის ლიმიტი)
        //  და WeightedAvgDate(ასაკი) ველებს.

        [HttpGet("/api/operations-spirit/get-data-by-transdate")]
        public async Task<IActionResult> GetDataByTransDate(int operType, int barrelId, int? oppositeBarrelId, DateTime transactionDate)
        {
            int workBarrelId = operType == 1 ? barrelId : oppositeBarrelId ?? 0;

            // --- აი ეს ბლოკი შეცვალე ასე: ---
            if (workBarrelId == 0)
            {
                return Json(new
                {
                    beverageId = 0,
                    beverageName = "",
                    productType = "",
                    category = "",
                    color = "",
                    sweetness = "",
                    volume = 0,
                    maxAllowed = 0,
                    harvestYear = "",
                    weightedAvgDate = "",
                    purePercent = 0,
                    yearLimit = -1,
                    pureLimit = -1,
                    allowMix = false
                });
            }
            // --------------------------------

            // ცვლადების ინიციალიზაცია (აქედან კოდი უცვლელია...)
            int beverageId = 0;
            string beverageName = "";
            string productType = "";
            string category = "";
            string color = "";
            string sweetness = "";
            decimal volume = 0;
            decimal maxAllowed = 0;
            string harvestYear = "";
            string weightedAvgDate = "";
            decimal purePercent = 0;
            decimal yearLimit = -1;
            decimal pureLimit = -1;
            bool allowMix = true;

            using (var conn = _context.Database.GetDbConnection() as SqlConnection)
            using (var cmd = new SqlCommand("Spirit_Get_Data_By_TransDate", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@TargetBarrelID", barrelId);
                cmd.Parameters.AddWithValue("@SourceBarrelID", workBarrelId);
                cmd.Parameters.AddWithValue("@OperType", operType);
                cmd.Parameters.AddWithValue("@TransactionDate", transactionDate);

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        beverageId = Convert.ToInt32(reader["BeverageID"]);
                        beverageName = reader["BeverageName"]?.ToString() ?? "";
                        productType = reader["ProductType"]?.ToString() ?? "";
                        category = reader["Category"]?.ToString() ?? "";
                        color = reader["Color"]?.ToString() ?? "";
                        sweetness = reader["Sweetness"]?.ToString() ?? "";
                        volume = Convert.ToDecimal(reader["Volume"]);
                        maxAllowed = Convert.ToDecimal(reader["MaxAllowed"]);

                        harvestYear = reader["HarvestYear"]?.ToString() ?? "";
                        weightedAvgDate = reader["WeightedAvgDate"] != DBNull.Value ? Convert.ToDateTime(reader["WeightedAvgDate"]).ToString("yyyy-MM-dd") : "";
                        purePercent = Convert.ToDecimal(reader["PurePercent"]);
                        yearLimit = Convert.ToDecimal(reader["YearLimit"]);
                        pureLimit = Convert.ToDecimal(reader["PureLimit"]);
                        allowMix = Convert.ToBoolean(reader["AllowMix"]);
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
                maxAllowed,
                harvestYear,
                weightedAvgDate,
                purePercent,
                yearLimit,
                pureLimit,
                allowMix
            });
        }

        [HttpGet("/api/operations-spirit/document-types")]
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