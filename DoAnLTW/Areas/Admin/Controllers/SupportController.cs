// Areas/Admin/Controllers/SupportController.cs
using DoAnLTW.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Area("Admin")]
[Authorize(Roles = "Admin,Employee")]
public class SupportController : Controller
{
    private readonly IChatService _chatService;

    public SupportController(IChatService chatService)
    {
        _chatService = chatService;
    }

    public IActionResult Index()
    {
        var supportRequests = _chatService.GetActiveSupportRequests();
        return View(supportRequests);
    }

    public IActionResult Chat(string customerId)
    {
        ViewBag.CustomerId = customerId;
        ViewBag.UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        ViewBag.Role = "Admin";
        return View();
    }
}