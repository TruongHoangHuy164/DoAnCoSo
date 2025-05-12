//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using DoAnLTW.Models;
//using DoAnLTW.Models.Repositories;
//using System.Linq;
//using System.Threading.Tasks;

//namespace DoAnLTW.Controllers
//{
//    [Authorize]
//    public class HistoryOrderController : Controller
//    {
//        private readonly IOrderRepository _orderRepository;
//        private readonly UserManager<IdentityUser> _userManager;
//        private readonly ApplicationDbContext _context;

//        public HistoryOrderController(IOrderRepository orderRepository, UserManager<IdentityUser> userManager, ApplicationDbContext context)
//        {
//            _orderRepository = orderRepository;
//            _userManager = userManager;
//            _context = context;
//        }

//        public async Task<IActionResult> OrderHistory(int page = 1)
//        {
//            var user = await _userManager.GetUserAsync(User);
//            if (user == null)
//            {
//                return RedirectToAction("Login", "Account");
//            }

//            int pageSize = 10;
//            var orders = _context.Orders
//                .Where(o => o.OrderDetails.Any(od => od. == user.Id))
//                .Include(o => o.PromotionCode)
//                .Include(o => o.OrderDetails)
//                .OrderByDescending(o => o.OrderDate)
//                .Skip((page - 1) * pageSize)
//                .Take(pageSize)
//                .ToListAsync();

//            ViewBag.Page = page;
//            ViewBag.TotalPages = (int)Math.Ceiling((double)_context.Orders.Count(o => o.OrderDetails.Any(od => od.UserId == user.Id)) / pageSize);
//            return View(orders);
//        }

//        public async Task<IActionResult> OrderDetail(int id)
//        {
//            var user = await _userManager.GetUserAsync(User);
//            var order = await _context.Orders
//                .Include(o => o.PromotionCode)
//                .Include(o => o.OrderDetails)
//                .FirstOrDefaultAsync(o => o.Id == id && o.OrderDetails.Any(od => od.UserId == user.Id));

//            if (order == null)
//            {
//                return NotFound();
//            }

//            return View(order);
//        }

//        [HttpPost]
//        public async Task<IActionResult> CancelOrder(int id)
//        {
//            var user = await _userManager.GetUserAsync(User);
//            var order = await _context.Orders
//                .Include(o => o.CustomerPoints)
//                .FirstOrDefaultAsync(o => o.Id == id && o.OrderDetails.Any(od => od.UserId == user.Id));

//            if (order == null)
//            {
//                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
//            }

//            if (order.Status != OrderStatus.ChoXuLy)
//            {
//                return Json(new { success = false, message = "Chỉ có thể hủy đơn hàng ở trạng thái 'Chờ xử lý'." });
//            }

//            order.Status = OrderStatus.Huy; // Giả định thêm trạng thái "Hủy"
//            if (order.CustomerPoints.Any())
//            {
//                _context.CustomerPoints.RemoveRange(order.CustomerPoints);
//            }
//            await _context.SaveChangesAsync();

//            return Json(new { success = true, message = "Hủy đơn hàng thành công." });
//        }
//    }
//}