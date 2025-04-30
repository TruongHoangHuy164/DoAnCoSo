using DoAnLTW.Models;
using DoAnLTW.Models.Repositories;
using Microsoft.AspNetCore.Mvc;
using DoAnLTW.Extensions;
using System.Text;
using VNPAY.NET.Enums;
using VNPAY.NET.Models;
using VNPAY.NET.Utilities;
using VNPAY.NET;
using DoAnLTW.Services.Momo;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using DoAnLTW.Services;
using Microsoft.Extensions.Logging; // Thêm namespace cho ILogger

namespace DoAnLTW.Controllers
{
    public class CheckoutController : BaseController
    {
        private const string CART_KEY = "Cart";
        private const decimal SHIPPING_FEE = 10000; // Phí vận chuyển mặc định
        private readonly IOrderRepository _orderRepository;
        private readonly IVnpay _vnpay;
        private readonly IMomoService _momoService;
        private readonly ApplicationDbContext _context;
        private readonly IProductRepository _productRepository;
        private readonly IEmailSender _emailSender;
        private readonly IRazorViewToStringRenderer _razorRenderer;
        private readonly ILogger<CheckoutController> _logger; // Thêm ILogger

        public CheckoutController(
            IOrderRepository orderRepository,
            IMomoService momoService,
            ApplicationDbContext context,
            IProductRepository productRepository,
            IEmailSender emailSender,
            IRazorViewToStringRenderer razorRenderer,
            ILogger<CheckoutController> logger) // Thêm ILogger vào constructor
        {
            _vnpay = new Vnpay();
            _orderRepository = orderRepository;
            _momoService = momoService;
            _context = context;
            _productRepository = productRepository;
            _emailSender = emailSender;
            _razorRenderer = razorRenderer;
            _logger = logger;
            _vnpay.Initialize("UNNRVRJ6", "RSG8ZUBQMVZCD5QSFTW4ZDIJBONYJ5SA",
                "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html", "https://localhost:5134/Checkout/VnPayReturn");
        }

        private async Task SendOrderConfirmationEmail(Order order)
        {
            try
            {
                _logger.LogInformation("Bắt đầu gửi email xác nhận cho đơn hàng #{OrderId} tới {Email}", order.Id, order.Email);

                // Sử dụng đường dẫn mới: ~/Views/Checkout/OrderConfirmationEmail.cshtml
                var viewPath = "Emails/OrderConfirmationEmail";
                _logger.LogInformation("Đang render email từ view: {ViewPath}", viewPath);

                var viewBag = new Dictionary<string, object> { { "ShippingFee", SHIPPING_FEE } };
                var emailContent = await _razorRenderer.RenderViewToStringAsync(viewPath, order);

                _logger.LogInformation("Render email thành công, nội dung: {Content}", emailContent.Substring(0, Math.Min(emailContent.Length, 100)));

                await _emailSender.SendEmailAsync(order.Email, "Xác nhận đơn hàng #" + order.Id, emailContent);
                _logger.LogInformation("Gửi email xác nhận thành công cho đơn hàng #{OrderId}", order.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email xác nhận cho đơn hàng #{OrderId}: {Message}", order.Id, ex.Message);
                TempData["EmailError"] = "Không thể gửi email xác nhận. Vui lòng kiểm tra email của bạn sau.";
            }
        }

        [HttpGet]
        public async Task<IActionResult> VnPayReturn()
        {
            var vnpResponse = HttpContext.Request.Query;

            // Kiểm tra mã giao dịch và chữ ký
            var secureHash = vnpResponse["vnp_SecureHash"].ToString();
            var paymentResult = _vnpay.GetPaymentResult(vnpResponse);

            if (paymentResult.IsSuccess)
            {
                string orderIdStr = vnpResponse["vnp_TxnRef"].ToString();
                if (int.TryParse(orderIdStr, out int orderId))
                {
                    var order = await _orderRepository.GetByIdAsync(orderId);
                    if (order != null)
                    {
                        order.IsPaid = true; // Cập nhật trạng thái thanh toán
                        await _orderRepository.UpdateAsync(order);

                        // Gửi email xác nhận
                        await SendOrderConfirmationEmail(order);

                        TempData["Success"] = "Thanh toán VNPay thành công";
                        return RedirectToAction("OrderSuccess", new { orderId });
                    }
                }

                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["Error"] = "Thanh toán VNPay không thành công.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentCallback()
        {
            var momoResponse = _momoService.PaymentExecuteAsync(HttpContext.Request.Query);
            if (momoResponse != null && momoResponse.ResultCode == 0) // Kiểm tra thanh toán thành công
            {
                string orderIdStr = momoResponse.OrderID;
                if (!string.IsNullOrEmpty(orderIdStr))
                {
                    var orderIdParts = orderIdStr.Split('_');
                    if (orderIdParts.Length > 0 && int.TryParse(orderIdParts[0], out int orderId))
                    {
                        var order = await _orderRepository.GetByIdAsync(orderId);
                        if (order != null)
                        {
                            order.IsPaid = true; // Cập nhật trạng thái thanh toán
                            await _orderRepository.UpdateAsync(order);

                            // Gửi email xác nhận
                            await SendOrderConfirmationEmail(order);

                            TempData["Success"] = "Thanh toán MoMo thành công";
                            return RedirectToAction("OrderSuccess", new { orderId });
                        }
                    }
                }

                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["Error"] = "Thanh toán MoMo không thành công.";
                return RedirectToAction("Index");
            }
        }

        public IActionResult Index()
        {
            SetCartCount();
            var cart = GetCartItems();
            if (!cart.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đã hết sản phẩm!";
                return RedirectToAction("Index", "Cart");
            }

            var viewModel = new CheckoutViewModel
            {
                CartItems = cart,
                ShippingFee = SHIPPING_FEE
            };
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessCheckout(CheckoutViewModel viewModel)
        {
            // Tạo đối tượng Order từ dữ liệu trong viewModel
            var order = new Order
            {
                FirstName = viewModel.Order.FirstName ?? string.Empty,
                LastName = viewModel.Order.LastName ?? string.Empty,
                Email = viewModel.Order.Email ?? string.Empty,
                PhoneNumber = viewModel.Order.PhoneNumber ?? string.Empty,
                Address = viewModel.Order.Address ?? string.Empty,
                AlternateAddress = viewModel.Order.AlternateAddress ?? string.Empty,
                PaymentMethod = viewModel.PaymentMethod ?? "COD",
                TotalAmount = viewModel.CartItems.Sum(item => item.Price * item.Quantity) + SHIPPING_FEE,
                OrderDate = DateTime.Now,
                IsPaid = (viewModel.PaymentMethod == "COD") ? false : true
            };

            order.OrderDetails = viewModel.CartItems.Select(item => new OrderDetail
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Size = item.Size,
                Quantity = item.Quantity,
                Price = item.Price
            }).ToList();

            try
            {
                // Lưu đơn hàng vào database
                await _orderRepository.AddAsync(order);

                // Xử lý thanh toán MoMo
                if (viewModel.PaymentMethod == "Momo")
                {
                    try
                    {
                        var momoResponse = await _momoService.CreatePaymentAsync(order);
                        if (momoResponse.ErrorCode == 0)
                        {
                            return Redirect(momoResponse.PayUrl);
                        }
                        else
                        {
                            await _orderRepository.DeleteAsync(order.Id);
                            viewModel.CartItems = GetCartItems();
                            viewModel.ShippingFee = SHIPPING_FEE;
                            TempData["Error"] = "Không thể tạo thanh toán MoMo. Vui lòng thử lại.";
                            return View("Index", viewModel);
                        }
                    }
                    catch (Exception momoEx)
                    {
                        Console.WriteLine("Lỗi khi tạo MoMo URL: " + momoEx.Message);
                        await _orderRepository.DeleteAsync(order.Id);
                        viewModel.CartItems = GetCartItems();
                        viewModel.ShippingFee = SHIPPING_FEE;
                        TempData["Error"] = "Có lỗi xảy ra khi xử lý thanh toán MoMo. Vui lòng thử lại.";
                        return View("Index", viewModel);
                    }
                }

                // Xử lý thanh toán VNPay
                if (viewModel.PaymentMethod == "VNPay")
                {
                    try
                    {
                        var ipAddress = NetworkHelper.GetIpAddress(HttpContext);

                        var request = new PaymentRequest
                        {
                            PaymentId = order.Id,
                            Money = ((double)(order.TotalAmount)),
                            Description = order.Address,
                            IpAddress = ipAddress,
                            BankCode = BankCode.ANY,
                            CreatedDate = DateTime.Now,
                            Currency = Currency.VND,
                            Language = DisplayLanguage.Vietnamese
                        };

                        var paymentUrl = _vnpay.GetPaymentUrl(request);
                        return Redirect(paymentUrl);
                    }
                    catch (Exception vnPayEx)
                    {
                        Console.WriteLine("Lỗi khi tạo VNPay URL: " + vnPayEx.Message);
                        await _orderRepository.DeleteAsync(order.Id);
                        viewModel.CartItems = GetCartItems();
                        viewModel.ShippingFee = SHIPPING_FEE;
                        TempData["Error"] = "Có lỗi xảy ra khi xử lý thanh toán VNPay. Vui lòng thử lại.";
                        return View("Index", viewModel);
                    }
                }

                // Gửi email xác nhận cho COD
                await SendOrderConfirmationEmail(order);

                // Xóa giỏ hàng sau khi đặt hàng thành công
                HttpContext.Session.Remove(CART_KEY);
                return RedirectToAction("OrderSuccess", new { orderId = order.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi khi lưu đơn hàng: " + ex.Message);
                viewModel.CartItems = GetCartItems();
                viewModel.ShippingFee = SHIPPING_FEE;
                TempData["Error"] = "Có lỗi xảy ra khi lưu đơn hàng. Vui lòng thử lại.";
                return View("Index", viewModel);
            }
        }

        public ActionResult<string> Callback()
        {
            if (Request.QueryString.HasValue)
            {
                try
                {
                    var paymentResult = _vnpay.GetPaymentResult(Request.Query);
                    var resultDescription = $"{paymentResult.PaymentResponse.Description}. {paymentResult.TransactionStatus.Description}.";

                    if (paymentResult.IsSuccess)
                    {
                        return Ok(resultDescription);
                    }

                    return BadRequest(resultDescription);
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }

            return NotFound("Không tìm thấy thông tin thanh toán.");
        }

        public async Task<IActionResult> OrderSuccess(int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }

            ViewBag.ShippingFee = SHIPPING_FEE;
            return View(order);
        }

        private List<CartItem> GetCartItems()
        {
            return HttpContext.Session.GetObjectFromJson<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
        }
    }
}