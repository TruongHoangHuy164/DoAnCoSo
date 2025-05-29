using DoAnLTW.Models;
using DoAnLTW.Models.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace DoAnLTW.Controllers
{
    [Authorize(Roles = "Customer")]
    public class UserActivityNotificationController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IPetServiceRepository _petServiceRepository;

        public UserActivityNotificationController(
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context,
            IPetServiceRepository petServiceRepository)
        {
            _userManager = userManager;
            _context = context;
            _petServiceRepository = petServiceRepository;
        }

        // GET: UserActivityNotification
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để xem thông báo.";
                return RedirectToAction("Login", "Account");
            }

            // Lấy ngày đặt hàng gần nhất
            var latestOrder = await _context.Orders
                .Where(o => o.Email == user.Email)
                .OrderByDescending(o => o.OrderDate)
                .FirstOrDefaultAsync();

            // Lấy ngày đặt dịch vụ gần nhất
            var userId = _userManager.GetUserId(User);
            var latestBooking = (await _petServiceRepository.GetByUserIdAsync(userId))
                .OrderByDescending(b => b.BookingDate)
                .FirstOrDefault();

            // Lấy ngày đặt khách sạn thú cưng gần nhất
            var latestHotelBooking = await _context.PetHotelBookings
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .FirstOrDefaultAsync();

            // Tính toán và định dạng thời gian
            ViewBag.TimeSinceLastOrder = latestOrder != null
                ? FormatTimeSpan(DateTime.Now - latestOrder.OrderDate)
                : "Bạn chưa mua sản phẩm nào.";

            ViewBag.TimeSinceLastBooking = latestBooking != null
                ? FormatTimeSpan(DateTime.Now - latestBooking.BookingDate)
                : "Bạn chưa sử dụng dịch vụ nào cho thú cưng.";

            ViewBag.TimeSinceLastHotelBooking = latestHotelBooking != null
                ? FormatTimeSpan(DateTime.Now - latestHotelBooking.BookingDate)
                : "Bạn chưa đặt phòng khách sạn thú cưng nào.";

            return View();
        }

        // GET: UserActivityNotification/GetNotificationCount
        [HttpGet]
        public async Task<IActionResult> GetNotificationCount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { notificationCount = 0, message = "Không có thông báo" });
            }

            var orderCount = await _context.Orders
                .Where(o => o.Email == user.Email)
                .CountAsync();

            var userId = _userManager.GetUserId(User);
            var bookingCount = (await _petServiceRepository.GetByUserIdAsync(userId)).Count();

            var hotelBookingCount = await _context.PetHotelBookings
                .Where(b => b.UserId == userId)
                .CountAsync();

            var totalCount = orderCount + bookingCount + hotelBookingCount;

            return Json(new
            {
                notificationCount = totalCount,
                message = totalCount > 0 ? "Có thông báo mới" : "Không có thông báo"
            });
        }

        // Hàm hỗ trợ định dạng thời gian
        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 365)
            {
                int years = (int)(timeSpan.TotalDays / 365);
                return $"{years} năm";
            }
            else if (timeSpan.TotalDays >= 30)
            {
                int months = (int)(timeSpan.TotalDays / 30);
                return $"{months} tháng";
            }
            else if (timeSpan.TotalDays >= 1)
            {
                return $"{(int)timeSpan.TotalDays} ngày";
            }
            else
            {
                return "Dưới 1 ngày";
            }
        }
    }
}