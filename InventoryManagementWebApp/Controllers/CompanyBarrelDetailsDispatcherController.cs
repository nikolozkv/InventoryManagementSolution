using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementWebApp.Controllers
{
    public class CompanyBarrelDetailsDispatcherController : Controller
    {
        public IActionResult Index(int companyId)
        {
            // 1. ვკითხულობთ დამახსოვრებულ ნიღაბს ქუქიდან
            int workingMask = 0;
            if (HttpContext.Request.Cookies.TryGetValue("CurrentWorkingMask", out string? maskStr))
            {
                int.TryParse(maskStr, out workingMask);
            }

            // 2. დისპეტჩერის ლოგიკა:
            // თუ ნიღაბში არის სპირტი (ბიტი 4)
            if ((workingMask & 4) > 0)
            {
                return RedirectToAction("Index", "CompanyBarrelSpiritDetails", new { companyId = companyId });
            }

            // სხვა შემთხვევაში (ღვინო)
            return RedirectToAction("Index", "CompanyBarrelDetails", new { companyId = companyId });
        }
    }
}