using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace InventoryManagementWebApp.TagHelpers
{
    [HtmlTargetElement(Attributes = "asp-policy")]
    public class AuthorizePolicyTagHelper : TagHelper
    {
        private readonly IAuthorizationService _authorizationService;

        public AuthorizePolicyTagHelper(IAuthorizationService authorizationService)
        {
            _authorizationService = authorizationService;
        }

        [HtmlAttributeName("asp-policy")]
        public string Policy { get; set; } = string.Empty;

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; } = null!;

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var user = ViewContext?.HttpContext?.User;
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
            {
                output.SuppressOutput();
                return;
            }

            if (!string.IsNullOrEmpty(Policy))
            {
                var authResult = await _authorizationService.AuthorizeAsync(user, Policy);
                if (!authResult.Succeeded)
                {
                    output.SuppressOutput();
                }
            }
        }
    }
}
