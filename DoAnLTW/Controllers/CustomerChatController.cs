/*using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using Serilog; // Thêm namespace Serilog

namespace DoAnLTW.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerChatController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly Serilog.ILogger _logger; // Chỉ rõ Serilog.ILogger

        public CustomerChatController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
            _logger = Log.ForContext<CustomerChatController>();
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.Information("Customer accessing chat interface.");

                // Lấy danh sách nhân viên trực tuyến
                var employees = await _userManager.GetUsersInRoleAsync("Employee");
                var employee = employees.FirstOrDefault();

                if (employee == null)
                {
                    _logger.Warning("No employees are currently online.");
                    ViewBag.ErrorMessage = "Hiện tại không có nhân viên nào trực tuyến.";
                    return View();
                }

                // Trả về thông tin nhân viên cho giao diện
                ViewBag.EmployeeId = employee.Id;
                ViewBag.EmployeeName = employee.UserName;
                _logger.Information($"Found employee: ID={employee.Id}, Name={employee.UserName}");

                return View();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in CustomerChatController.Index");
                ViewBag.ErrorMessage = "Đã xảy ra lỗi khi tải giao diện chat. Vui lòng thử lại sau.";
                return View();
            }
        }
    }
}*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnLTW.Models;

namespace DoAnLTW.Controllers
{
    [Route("CustomerChat")]
    public class CustomerChatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerChatController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var employee = await _context.Users
                .Join(_context.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { User = u, UserRole = ur })
                .Join(_context.Roles, ur => ur.UserRole.RoleId, r => r.Id, (ur, r) => new { ur.User, RoleName = r.Name })
                .Where(ur => ur.RoleName == "Employee")
                .Select(ur => new { ur.User.Id, ur.User.UserName })
                .FirstOrDefaultAsync();

            if (employee == null)
            {
                return Json(new { errorMessage = "Không có nhân viên trực tuyến." });
            }

            return Json(new { employeeId = employee.Id, employeeName = employee.UserName });
        }
    }
}