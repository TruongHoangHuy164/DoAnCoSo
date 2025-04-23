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

        public CheckoutController(IOrderRepository orderRepository, IMomoService momoService, ApplicationDbContext context, IProductRepository productRepository)
        {
            _vnpay = new Vnpay();
            _orderRepository = orderRepository;
            _momoService = momoService;
            _context = context;
            _productRepository = productRepository;
            _vnpay.Initialize("UNNRVRJ6", "RSG8ZUBQMVZCD5QSFTW4ZDIJBONYJ5SA",
                "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html", "https://localhost:5134/Checkout/VnPayReturn");
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
                // Giả sử orderId được lưu trong vnp_TxnRef hoặc một tham số khác trong phản hồi
                string orderIdStr = vnpResponse["vnp_TxnRef"].ToString();
                if (int.TryParse(orderIdStr, out int orderId))
                {
                    var order = await _orderRepository.GetByIdAsync(orderId);
                    if (order != null)
                    {
                        order.IsPaid = true; // Cập nhật trạng thái thanh toán
                        await _orderRepository.UpdateAsync(order);
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
                // Cập nhật trạng thái thanh toán của đơn hàng
                // MoMo OrderID có dạng <orderId>_<additionalData>, ví dụ: "39_638793445437982283"
                string orderIdStr = momoResponse.OrderID;
                if (!string.IsNullOrEmpty(orderIdStr))
                {
                    // Tách phần orderId từ OrderID bằng cách split theo dấu "_"
                    var orderIdParts = orderIdStr.Split('_');
                    if (orderIdParts.Length > 0 && int.TryParse(orderIdParts[0], out int orderId))
                    {
                        var order = await _orderRepository.GetByIdAsync(orderId);
                        if (order != null)
                        {
                            order.IsPaid = true; // Cập nhật trạng thái thanh toán
                            await _orderRepository.UpdateAsync(order);
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
                        // Đảm bảo OrderID được tạo với định dạng có thể tách được sau này
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
                            PaymentId = order.Id, // Sử dụng order.Id làm PaymentId
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