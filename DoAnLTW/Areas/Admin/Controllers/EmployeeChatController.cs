using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnLTW.Models;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;
using Serilog; // Thêm namespace Serilog

namespace DoAnLTW.Areas.Admin.Controllers
{
    [Authorize(Roles = "Employee")]
    [Area("Admin")]
    public class EmployeeChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly Serilog.ILogger _logger; // Chỉ rõ Serilog.ILogger

        public EmployeeChatController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
            _logger = Log.ForContext<EmployeeChatController>();
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.Information("Employee accessing chat interface. User ID: {UserId}", _userManager.GetUserId(User));

                // Lấy danh sách khách hàng đã gửi tin nhắn chưa đọc
                var customerRequests = await _context.Messages
                    .Where(m => m.ReceiverId == _userManager.GetUserId(User) && !m.IsRead)
                    .GroupBy(m => m.SenderId)
                    .Select(g => new
                    {
                        CustomerId = g.Key,
                        CustomerName = _context.Users
                            .Where(u => u.Id == g.Key)
                            .Select(u => u.UserName)
                            .FirstOrDefault(),
                        LastMessage = g.OrderByDescending(m => m.Timestamp)
                            .Select(m => m.Content)
                            .FirstOrDefault(),
                        Timestamp = g.OrderByDescending(m => m.Timestamp)
                            .Select(m => m.Timestamp)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                if (!customerRequests.Any())
                {
                    _logger.Information("No unread customer messages found.");
                }
                else
                {
                    _logger.Information("Found {Count} customer requests.", customerRequests.Count);
                }

                ViewBag.CustomerRequests = customerRequests;
                return View();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in EmployeeChatController.Index");
                ViewBag.ErrorMessage = "Đã xảy ra lỗi khi tải danh sách yêu cầu chat. Vui lòng thử lại sau.";
                return View();
            }
        }
    }
}