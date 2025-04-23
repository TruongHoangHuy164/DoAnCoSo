using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace DoAnLTW.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;

        public ResetPasswordModel(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập email")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
            [StringLength(100, ErrorMessage = "{0} phải có ít nhất {2} và tối đa {1} ký tự.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Xác nhận mật khẩu")]
            [Compare("Password", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp.")]
            public string ConfirmPassword { get; set; }

            public string Code { get; set; }
            public DateTime? Expired { get; set; }
        }

        public async Task<IActionResult> OnGet(string code = null)
        {
            if (code == null)
            {
                return BadRequest("Mã đặt lại mật khẩu là cần thiết.");
            }
            else
            {
                //NDYyMzg4?expire=3/25/2025 9:53:44 PM
                string[] parts = code.Split("?expire=");
                string expired = parts[1];
                 code = parts[0];

                Input = new InputModel
                {
                    Code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code)),
                    Expired = DateTime.Parse(expired)
                };
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToPage("./ResetPasswordConfirmation");
            }

                if (DateTime.Now.Subtract(Input.Expired.Value).TotalSeconds > 90)
                {
                    ModelState.AddModelError(string.Empty, "Mã đặt lại mật khẩu đã hết hạn.");
                    return Page();
                }

            // Check if the token is valid
            bool isTokenValid = await _userManager.VerifyUserTokenAsync(
                user, 
                _userManager.Options.Tokens.PasswordResetTokenProvider, 
                "ResetPassword", 
                Input.Code);
                
            if (!isTokenValid)
            {
                ModelState.AddModelError(string.Empty, "Mã đặt lại mật khẩu đã hết hạn.");
                return Page();
            }

            var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
            if (result.Succeeded)
            {
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }
    }
}
