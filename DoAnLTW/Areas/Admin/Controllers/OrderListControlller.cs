using DoAnLTW.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using DoAnLTW.Services;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

namespace DoAnLTW.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly IRazorViewToStringRenderer _razorRenderer;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            ApplicationDbContext context,
            IEmailSender emailSender,
            IRazorViewToStringRenderer razorRenderer,
            ILogger<OrderController> logger)
        {
            _context = context;
            _emailSender = emailSender;
            _razorRenderer = razorRenderer;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ToListAsync();

            return View(orders);
        }

        private async Task SendOrderStatusUpdateEmail(Order order)
        {
            try
            {
                _logger.LogInformation("Bắt đầu gửi email cập nhật trạng thái cho đơn hàng #{OrderId} tới {Email}", order.Id, order.Email);

                var viewPath = "Emails/OrderConfirmationEmail";
                _logger.LogInformation("Đang render email từ view: {ViewPath}", viewPath);

                var viewBag = new Dictionary<string, object> { { "ShippingFee", 10000 } };
                var emailContent = await _razorRenderer.RenderViewToStringAsync(viewPath, order);

                _logger.LogInformation("Render email thành công, nội dung: {Content}", emailContent.Substring(0, Math.Min(emailContent.Length, 100)));

                await _emailSender.SendEmailAsync(order.Email, $"Cập nhật trạng thái đơn hàng #{order.Id}", emailContent);
                _logger.LogInformation("Gửi email cập nhật trạng thái thành công cho đơn hàng #{OrderId}", order.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email cập nhật trạng thái cho đơn hàng #{OrderId}: {Message}", order.Id, ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusModel model)
        {
            if (model == null || !Enum.TryParse<OrderStatus>(model.Status, out var status))
            {
                return Json(new { success = false, message = "Trạng thái không hợp lệ." });
            }

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == model.Id);
            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
            }

            order.Status = status;
            await _context.SaveChangesAsync();

            // Gửi email thông báo cập nhật trạng thái
            await SendOrderStatusUpdateEmail(order);

            return Json(new { success = true });
        }

        public class UpdateStatusModel
        {
            public int Id { get; set; }
            public string Status { get; set; }
        }
    }
}