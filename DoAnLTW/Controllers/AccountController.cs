using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DoAnLTW.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult LoginByGoogle()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GoogleCallback), "Account")
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleCallback()
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!result.Succeeded || result.Principal == null)
            {
                TempData["Error"] = "Đăng nhập Google thất bại";
                return RedirectToAction("Login", "Account");
            }

            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Email không hợp lệ từ Google";
                return RedirectToAction("Login", "Account");
            }

            // Tìm user theo email
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                // Tạo user mới nếu chưa tồn tại
                user = new IdentityUser
                {
                    UserName = name ?? email, // Sử dụng tên từ Google nếu có, không thì dùng email
                    Email = email,
                    EmailConfirmed = true // Email đã được xác nhận qua Google
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    TempData["Error"] = "Không thể tạo tài khoản mới";
                    return RedirectToAction("Login", "Account");
                }

                // Thêm claim cho user
                var userClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, name ?? email),
                    new Claim(ClaimTypes.Email, email)
                };

                await _userManager.AddClaimsAsync(user, userClaims);
            }

            // Đăng nhập user
            await _signInManager.SignInAsync(user, isPersistent: true);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}