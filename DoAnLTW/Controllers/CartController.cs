using DoAnLTW.Models;
using DoAnLTW.Models.Repositories;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace DoAnLTW.Controllers
{
    public class CartController : BaseController
    {
        private const string CART_KEY = "Cart"; // Tên session giỏ hàng
        private readonly IProductRepository _productRepository;

        public CartController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        // Lấy danh sách sản phẩm trong giỏ hàng từ Session
        private List<CartItem> GetCartItems()
        {
            var session = HttpContext.Session;
            string jsonCart = session.GetString(CART_KEY);

            if (!string.IsNullOrEmpty(jsonCart))
            {
                Console.WriteLine("🛒 Dữ liệu trong Session: " + jsonCart);
                return JsonConvert.DeserializeObject<List<CartItem>>(jsonCart);
            }

            Console.WriteLine("🚨 Giỏ hàng rỗng!");
            return new List<CartItem>();
        }

        // Lưu giỏ hàng vào Session
        private void SaveCartSession(List<CartItem> cart)
        {
            var session = HttpContext.Session;
            string jsonCart = JsonConvert.SerializeObject(cart);
            session.SetString(CART_KEY, jsonCart);
        }

        // Thêm sản phẩm vào giỏ hàng
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int sizeId, int quantity)
        {
            // Default to 1 if quantity is invalid
            if (quantity < 1)
            {
                quantity = 1; // Fallback to 1 instead of returning an error
                // Alternatively, you can return the error as before:
                // return Json(new { success = false, message = "Số lượng phải lớn hơn hoặc bằng 1." });
            }

            var cart = GetCartItems();
            var productInCart = cart.FirstOrDefault(p => p.ProductId == productId && p.Size == sizeId.ToString());

            if (productInCart != null)
            {
                // Kiểm tra số lượng tồn kho trước khi tăng
                var product = await _productRepository.GetByIdAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại." });
                }

                var productSize = product.ProductSizes.FirstOrDefault(ps => ps.SizeId == sizeId);
                if (productSize == null)
                {
                    return Json(new { success = false, message = "Size không hợp lệ." });
                }

                if (productInCart.Quantity + quantity > productSize.Stock)
                {
                    return Json(new { success = false, message = "Số lượng trong giỏ hàng vượt quá số lượng tồn kho." });
                }

                productInCart.Quantity += quantity; // Tăng số lượng theo quantity
            }
            else
            {
                // Lấy thông tin sản phẩm từ cơ sở dữ liệu
                var product = await _productRepository.GetByIdAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại." });
                }

                // Kiểm tra size và số lượng tồn kho
                var productSize = product.ProductSizes.FirstOrDefault(ps => ps.SizeId == sizeId);
                if (productSize == null)
                {
                    return Json(new { success = false, message = "Size không hợp lệ." });
                }

                if (productSize.Stock <= 0)
                {
                    return Json(new { success = false, message = "Sản phẩm đã hết hàng." });
                }

                if (quantity > productSize.Stock)
                {
                    return Json(new { success = false, message = "Số lượng vượt quá số lượng tồn kho." });
                }

                // Tạo CartItem mới
                var cartItem = new CartItem
                {
                    ProductId = product.ProductId,
                    ProductName = product.Name,
                    Price = productSize.Price, // Lấy giá từ ProductSize
                    Size = sizeId.ToString(), // Lưu size dưới dạng string (SizeId) để hiển thị
                    Quantity = quantity // Sử dụng quantity từ người dùng
                };

                cart.Add(cartItem); // Thêm vào giỏ hàng
            }

            SaveCartSession(cart); // Lưu lại session
            SetCartCount(); // Cập nhật số lượng giỏ hàng

            return Json(new { success = true, count = cart.Sum(item => item.Quantity) });
        }

        // Hiển thị giỏ hàng
        public IActionResult Index()
        {
            var cart = GetCartItems();
            SetCartCount();
            return View(cart);
        }

        public async Task<IActionResult> IncreaseQuantity(int productId, string size)
        {
            // Chuyển đổi size từ string sang int (SizeId)
            if (!int.TryParse(size, out int sizeId))
            {
                return BadRequest("Size không hợp lệ. Vui lòng chọn một size hợp lệ.");
            }

            var cart = GetCartItems();
            var productInCart = cart.FirstOrDefault(p => p.ProductId == productId && p.Size == size);

            if (productInCart != null)
            {
                // Kiểm tra số lượng tồn kho
                var product = await _productRepository.GetByIdAsync(productId);
                if (product == null)
                {
                    return NotFound("Sản phẩm không tồn tại.");
                }

                var productSize = product.ProductSizes.FirstOrDefault(ps => ps.SizeId == sizeId);
                if (productSize == null)
                {
                    return BadRequest("Size không hợp lệ.");
                }

                if (productInCart.Quantity + 1 > productSize.Stock)
                {
                    return BadRequest("Số lượng trong giỏ hàng vượt quá số lượng tồn kho.");
                }

                productInCart.Quantity++;
                SaveCartSession(cart);
                SetCartCount();
            }

            return RedirectToAction("Index");
        }

        public IActionResult DecreaseQuantity(int productId, string size)
        {
            var cart = GetCartItems();
            var productInCart = cart.FirstOrDefault(p => p.ProductId == productId && p.Size == size);

            if (productInCart != null)
            {
                productInCart.Quantity--;
                if (productInCart.Quantity <= 0)
                {
                    cart.Remove(productInCart); // Xóa sản phẩm nếu số lượng <= 0
                }
                SaveCartSession(cart);
                SetCartCount();
            }

            return RedirectToAction("Index");
        }

        public IActionResult RemoveFromCart(int productId, string size)
        {
            var cart = GetCartItems();
            var productInCart = cart.FirstOrDefault(p => p.ProductId == productId && p.Size == size);

            if (productInCart != null)
            {
                cart.Remove(productInCart);
                SaveCartSession(cart);
                SetCartCount();
            }

            return RedirectToAction("Index");
        }
    }
}