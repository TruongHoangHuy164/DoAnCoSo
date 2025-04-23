// ChatController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DoAnLTW.Models;

namespace DoAnLTW.Controllers
{
    [AllowAnonymous]
    public class ChatController : BaseController
    {
        public IActionResult Index()
        {
            SetCartCount();
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var roles = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            Console.WriteLine($"UserId: {userId}, Roles: {roles}");

            ViewBag.UserId = userId;
            ViewBag.Role = "Customer";
            return View();
        }
    }
}