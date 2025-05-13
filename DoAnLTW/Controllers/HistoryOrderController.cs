using DoAnLTW.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAnLTW.Controllers
{
    [Authorize]
    public class HistoryOrderController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public HistoryOrderController(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> OrderHistory(int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            int pageSize = 10;
            var orders = await _context.Orders
                .Where(o => o.Email == user.Email)
                .Include(o => o.OrderDetails)
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)_context.Orders.Count(o => o.Email == user.Email) / pageSize);
            return View(orders);
        }

        public async Task<IActionResult> OrderDetail(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id && o.Email == user.Email);

            if (order == null)
                return NotFound();

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var order = await _context.Orders
                .Include(o => o.CustomerPoints)
                .FirstOrDefaultAsync(o => o.Id == id && o.Email == user.Email);

            if (order == null)
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });

            if (order.Status != OrderStatus.ChoXuLy)
                return Json(new { success = false, message = "Chỉ có thể hủy đơn hàng ở trạng thái 'Chờ xử lý'." });

            order.Status = OrderStatus.DaHuy; // Hoặc tạo thêm enum OrderStatus.Huy
            _context.Orders.Update(order);

            if (order.CustomerPoints.Any())
                _context.CustomerPoints.RemoveRange(order.CustomerPoints);

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Hủy đơn hàng thành công." });
        }


    }
}
