using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DoAnLTW.Areas.Identity.Pages.Account
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

        // Đăng nhập với Google
        public async Task LoginByGoogle()
        {
            await HttpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme,
                new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") });
        }

        // Phản hồi từ Google
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!result.Succeeded || result.Principal == null)
            {
                Console.WriteLine("Authentication failed. Succeeded: " + result.Succeeded);
                if (result.Failure != null)
                {
                    Console.WriteLine("Failure reason: " + result.Failure.Message);
                }
                return Redirect("/Identity/Account/Login");
            }

            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                Console.WriteLine("Email not found in claims.");
                return Redirect("/Identity/Account/Login");
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true // Đặt EmailConfirmed để tránh lỗi validation
                };

                var createUserResult = await _userManager.CreateAsync(user);
                if (!createUserResult.Succeeded)
                {
                    Console.WriteLine("Failed to create user: " + string.Join(", ", createUserResult.Errors.Select(e => e.Description)));
                    return Redirect("/Identity/Account/Login");
                }

                // Gán role "Customer" cho user mới
                var roleResult = await _userManager.AddToRoleAsync(user, "Customer");
                if (!roleResult.Succeeded)
                {
                    Console.WriteLine("Failed to assign Customer role: " + string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    return Redirect("/Identity/Account/Login");
                }

                Console.WriteLine("User created and assigned Customer role successfully.");
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }

        // Đăng xuất
        public async Task<IActionResult> Logout()
        {
            // Đăng xuất người dùng khỏi phiên Identity
            await _signInManager.SignOutAsync();

            // Xóa thêm các cookie xác thực (nếu cần thiết)
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Chuyển hướng về trang đăng nhập hoặc trang chủ
            return Redirect("/Identity/Account/Login");
        }
    }
}