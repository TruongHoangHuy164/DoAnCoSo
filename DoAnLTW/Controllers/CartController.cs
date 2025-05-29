using DoAnLTW.Models;
using DoAnLTW.Models.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DoAnLTW.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CartController : BaseController
    {
        private const string CART_KEY = "Cart";
        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CartController> _logger;

        public CartController(IProductRepository productRepository, ApplicationDbContext context, ILogger<CartController> logger)
        {
            _productRepository = productRepository;
            _context = context;
            _logger = logger;
        }

        // Lấy danh sách sản phẩm trong giỏ hàng từ Session
        private List<CartItem> GetCartItems()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
            _logger.LogInformation("🛒 Dữ liệu trong Session: {JsonCart}", JsonConvert.SerializeObject(cart));
            return cart;
        }

        // Lưu giỏ hàng vào Session
        private void SaveCartSession(List<CartItem> cart)
        {
            HttpContext.Session.SetObjectAsJson(CART_KEY, cart);
            _logger.LogInformation("Đã lưu giỏ hàng vào session: {JsonCart}", JsonConvert.SerializeObject(cart));
        }

        // Cập nhật số lượng giỏ hàng
        private void SetCartCount()
        {
            var cart = GetCartItems();
            int count = cart.Sum(item => item.Quantity);
            HttpContext.Session.SetInt32("CartCount", count);
            _logger.LogInformation("Cập nhật số lượng giỏ hàng: {Count}", count);
        }

        // Hiển thị giỏ hàng
        [Authorize]
        public IActionResult Index()
        {
            var cart = GetCartItems();
            SetCartCount();
            _logger.LogInformation("Hiển thị giỏ hàng: Số lượng sản phẩm={Count}", cart.Count);
            return View(cart);
        }

        // Tăng số lượng
        [Authorize]
        public async Task<IActionResult> IncreaseQuantity(int productId, string size)
        {
            _logger.LogInformation("Nhận yêu cầu IncreaseQuantity: ProductId={ProductId}, Size={Size}", productId, size);

            // Tìm SizeId từ chuỗi size
            var sizeEntity = await _context.Sizes.FirstOrDefaultAsync(s => s.size == size);
            if (sizeEntity == null)
            {
                _logger.LogWarning("Size không hợp lệ: Size={Size}", size);
                return Json(new { success = false, message = $"Size {size} không hợp lệ." });
            }
            int sizeId = sizeEntity.SizeId; // Giả định SizeId là khóa chính của Size

            // Lấy giỏ hàng từ Session
            var cart = GetCartItems();
            var productInCart = cart.FirstOrDefault(c => c.ProductId == productId && c.Size == size);

            if (productInCart == null)
            {
                _logger.LogWarning("Không tìm thấy sản phẩm trong giỏ hàng: ProductId={ProductId}, Size={Size}, UserId={UserId}",
                    productId, size, User.Identity.Name);
                return Json(new { success = false, message = "Sản phẩm không có trong giỏ hàng." });
            }

            // Kiểm tra sản phẩm và kích thước
            var productSize = await _context.ProductSizes
                .Include(ps => ps.Product)
                .Include(ps => ps.Size)
                .FirstOrDefaultAsync(ps => ps.ProductId == productId && ps.SizeId == sizeId);

            if (productSize == null)
            {
                _logger.LogWarning("Không tìm thấy kích thước: ProductId={ProductId}, SizeId={SizeId}", productId, sizeId);
                return Json(new { success = false, message = $"Size {size} không hợp lệ." });
            }

            if (productInCart.Quantity + 1 > productSize.Stock)
            {
                _logger.LogWarning("Không đủ tồn kho: ProductId={ProductId}, Size={Size}, Stock={Stock}, Requested={Requested}",
                    productId, size, productSize.Stock, productInCart.Quantity + 1);
                return Json(new { success = false, message = $"Không đủ tồn kho cho sản phẩm (kích thước: {size})." });
            }

            // Tăng số lượng
            productInCart.Quantity++;
            SaveCartSession(cart);
            SetCartCount();

            _logger.LogInformation("Tăng số lượng thành công: ProductId={ProductId}, Size={Size}, NewQuantity={Quantity}",
                productId, size, productInCart.Quantity);

            return Json(new
            {
                success = true,
                newQuantity = productInCart.Quantity,
                newTotal = (productInCart.Price * productInCart.Quantity).ToString("#,##0 VNĐ"),
                cartTotal = (cart.Sum(item => item.Price * item.Quantity) + 10000).ToString("#,##0 VNĐ")
            });
        }

        // Giảm số lượng
        [Authorize]
        public IActionResult DecreaseQuantity(int productId, string size)
        {
            _logger.LogInformation("Nhận yêu cầu DecreaseQuantity: ProductId={ProductId}, Size={Size}", productId, size);

            var cart = GetCartItems();
            var productInCart = cart.FirstOrDefault(p => p.ProductId == productId && p.Size == size);

            if (productInCart != null)
            {
                productInCart.Quantity--;
                if (productInCart.Quantity <= 0)
                {
                    cart.Remove(productInCart);
                    _logger.LogInformation("Xóa sản phẩm khỏi giỏ hàng: ProductId={ProductId}, Size={Size}", productId, size);
                }
                SaveCartSession(cart);
                SetCartCount();
            }

            _logger.LogInformation("Giảm số lượng thành công: ProductId={ProductId}, Size={Size}, NewQuantity={Quantity}",
                productId, size, productInCart?.Quantity ?? 0);

            return Json(new
            {
                success = true,
                newQuantity = productInCart?.Quantity ?? 0,
                newTotal = productInCart != null ? (productInCart.Price * productInCart.Quantity).ToString("#,##0 VNĐ") : "0 VNĐ",
                cartTotal = (cart.Sum(item => item.Price * item.Quantity) + 10000).ToString("#,##0 VNĐ")
            });
        }

        // Xóa sản phẩm khỏi giỏ hàng
        [Authorize]
        public IActionResult RemoveFromCart(int productId, string size)
        {
            _logger.LogInformation("Nhận yêu cầu RemoveFromCart: ProductId={ProductId}, Size={Size}", productId, size);

            var cart = GetCartItems();
            var productInCart = cart.FirstOrDefault(p => p.ProductId == productId && p.Size == size);

            if (productInCart != null)
            {
                cart.Remove(productInCart);
                SaveCartSession(cart);
                SetCartCount();
                _logger.LogInformation("Xóa sản phẩm khỏi giỏ hàng: ProductId={ProductId}, Size={Size}", productId, size);
            }

            return Json(new
            {
                success = true,
                cartTotal = (cart.Sum(item => item.Price * item.Quantity) + 10000).ToString("#,##0 VNĐ")
            });
        }
    }

    // Extension methods for session
    public static class SessionExtensions
    {
        public static T GetObjectFromJson<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonConvert.DeserializeObject<T>(value);
        }

        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            session.SetString(key, JsonConvert.SerializeObject(value));
        }
    }
}