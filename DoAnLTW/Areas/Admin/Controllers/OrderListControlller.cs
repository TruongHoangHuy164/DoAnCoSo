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
                if (string.IsNullOrEmpty(order.Email))
                {
                    _logger.LogWarning("Email rỗng cho đơn hàng #{OrderId}", order.Id);
                    return;
                }

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
            _logger.LogInformation("Nhận yêu cầu UpdateStatus: Id={Id}, Status={Status}", model?.Id, model?.Status);

            // Kiểm tra dữ liệu đầu vào
            if (model == null || !Enum.TryParse<OrderStatus>(model.Status, true, out var status))
            {
                _logger.LogWarning("Model hoặc trạng thái không hợp lệ: Model={Model}, Status={Status}", model, model?.Status);
                return Json(new { success = false, message = "Trạng thái không hợp lệ." });
            }

            // Tìm đơn hàng
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == model.Id);
            if (order == null)
            {
                _logger.LogWarning("Không tìm thấy đơn hàng: Id={Id}", model.Id);
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
            }

            // Lưu trạng thái hiện tại của đơn hàng
            var previousStatus = order.Status;

            // Bắt đầu giao dịch
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Cập nhật trạng thái thanh toán cho COD
                if (order.PaymentMethod == "COD")
                {
                    order.IsPaid = status == OrderStatus.DaGiaoHang;
                }

                // Cập nhật trạng thái đơn hàng
                order.Status = status;
                await _context.SaveChangesAsync(); // Lưu trạng thái trước

                // Gửi email thông báo
                await SendOrderStatusUpdateEmail(order);

                // Xử lý trừ tồn kho khi trạng thái là DangGiaoHang và trạng thái trước đó không phải DangGiaoHang
                if (status == OrderStatus.DangGiaoHang && previousStatus != OrderStatus.DangGiaoHang)
                {
                    foreach (var detail in order.OrderDetails)
                    {
                        var productSize = await _context.ProductSizes
                            .FirstOrDefaultAsync(ps => ps.ProductId == detail.ProductId && ps.Size.size == detail.Size);

                        if (productSize == null)
                        {
                            _logger.LogWarning("Không tìm thấy kích thước: ProductId={ProductId}, Size={Size}", detail.ProductId, detail.Size);
                            await transaction.RollbackAsync();
                            return Json(new { success = false, message = $"Không tìm thấy kích thước {detail.Size} cho sản phẩm {detail.ProductName}" });
                        }

                        if (productSize.Stock < detail.Quantity)
                        {
                            _logger.LogWarning("Không đủ tồn kho: Product={ProductName}, Size={Size}, Stock={Stock}, Requested={Quantity}",
                                detail.ProductName, detail.Size, productSize.Stock, detail.Quantity);
                            await transaction.RollbackAsync();
                            return Json(new { success = false, message = $"Không đủ tồn kho cho sản phẩm {detail.ProductName} (kích thước: {detail.Size})" });
                        }

                        productSize.Stock -= detail.Quantity;
                    }
                    await _context.SaveChangesAsync(); // Lưu thay đổi tồn kho
                }

                await transaction.CommitAsync(); // Hoàn tất giao dịch
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi lưu trạng thái hoặc tồn kho cho đơn hàng #{OrderId}: {Message}", model.Id, ex.Message);
                return Json(new { success = false, message = "Lỗi khi lưu trạng thái hoặc tồn kho đơn hàng." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi không xác định khi xử lý đơn hàng #{OrderId}: {Message}", model.Id, ex.Message);
                return Json(new { success = false, message = "Lỗi không xác định khi xử lý đơn hàng." });
            }

            return Json(new { success = true, message = "Cập nhật trạng thái đơn hàng và tồn kho thành công" });
        }

        public class UpdateStatusModel
        {
            public int Id { get; set; }
            public string Status { get; set; }
        }
    }
}