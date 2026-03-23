using InventoryManagementWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryManagementWebApp.Controllers
{
    [Authorize]
    public class DocumentTypeController : Controller
    {
        private readonly InventoryContext _context;

        public DocumentTypeController(InventoryContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var docTypes = await _context.DocumentTypes.ToListAsync();
            return View(docTypes);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DocumentType docType)
        {
            if (ModelState.IsValid)
            {
                _context.DocumentTypes.Add(docType);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(docType);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var docType = await _context.DocumentTypes.FindAsync(id);
            if (docType == null) return NotFound();
            return View(docType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DocumentType docType)
        {
            if (id != docType.DocumentTypeID)
                return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(docType);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.DocumentTypes.Any(e => e.DocumentTypeID == docType.DocumentTypeID))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(docType);
        }

        [HttpPost]
        public async Task<IActionResult> Activate(int id)
        {
            var docType = await _context.DocumentTypes.FindAsync(id);
            if (docType == null) return NotFound();
            docType.IsActive = true;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Deactivate(int id)
        {
            var docType = await _context.DocumentTypes.FindAsync(id);
            if (docType == null) return NotFound();
            docType.IsActive = false;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // API endpoint for F1 clipboard functionality
        [HttpGet]
        [Route("api/documenttypes")]
        public async Task<IActionResult> GetDocumentTypesApi()
        {
            var docTypes = await _context.DocumentTypes
                .Where(dt => dt.IsActive)
                .Select(dt => new { value = dt.DocumentTypeID.ToString(), text = dt.DocumentName })
                .ToListAsync();
            
            return Json(docTypes);
        }
    }
}
