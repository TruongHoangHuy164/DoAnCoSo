using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DoAnLTW.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<IdentityUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập email")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null)
                {
                    // Không tiết lộ rằng người dùng không tồn tại
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // Tạo mã OTP (6 chữ số ngẫu nhiên)
                var otp = new Random().Next(100000, 999999).ToString();
                var otpExpiration = DateTime.Now.AddSeconds(200); // OTP hết hạn sau 200 giây

                // Lưu OTP và thời gian hết hạn (ví dụ: vào TempData hoặc database)
                // Ở đây sử dụng TempData để đơn giản, nhưng trong thực tế nên dùng database hoặc Redis
                TempData["OTP"] = otp;
                TempData["OTPExpiration"] = otpExpiration.ToString();
                TempData["OTPEmail"] = Input.Email;

                // Gửi email chứa OTP
                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "Mã OTP Đặt lại mật khẩu",
                    $@"Mã OTP của bạn là: <strong>{otp}</strong>. <br/><br/><strong>Lưu ý:</strong> Mã này chỉ có hiệu lực trong vòng 200 giây và chỉ có thể sử dụng một lần.");

                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}