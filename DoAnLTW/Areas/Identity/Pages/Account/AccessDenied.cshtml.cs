using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DoAnLTW.Areas.Identity.Pages.Account
{
    public class AccessDeniedModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;

        public AccessDeniedModel(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public string RedirectUrl { get; set; }

        public async Task OnGetAsync()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    RedirectUrl = roles.Contains("Admin") || roles.Contains("Employee") ? "/Admin/Statistics" : "/";
                }
                else
                {
                    RedirectUrl = "/";
                }
            }
            else
            {
                RedirectUrl = "/";
            }
        }
    }
}