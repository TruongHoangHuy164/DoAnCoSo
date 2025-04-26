using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnLTW.Models;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using DoAnLTW.Extensions;
using Newtonsoft.Json;

namespace DoAnLTW.Controllers
{
    public class ShopController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ShopController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. Danh sách sản phẩm
        public async Task<IActionResult> Index(string searchString, int? categoryId, int? brandId, int? sizeId, decimal? minPrice, decimal? maxPrice, string sortOrder = "best_selling")
        {
            // Đếm số lượng sản phẩm trong giỏ hàng (nếu có)
            SetCartCount();

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductSizes)
                .ThenInclude(ps => ps.Size)
                .Include(p => p.Images)
                .Include(p => p.Reviews)
                .AsQueryable();

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(searchString) ||
                                         p.Description.ToLower().Contains(searchString) ||
                                         p.Brand.Name.ToLower().Contains(searchString) ||
                                         p.Category.Name.ToLower().Contains(searchString));
            }

            // Lọc theo danh mục
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // Lọc theo thương hiệu
            if (brandId.HasValue)
            {
                query = query.Where(p => p.BrandId == brandId.Value);
            }

            // Lọc theo kích thước
            if (sizeId.HasValue)
            {
                query = query.Where(p => p.ProductSizes.Any(ps => ps.SizeId == sizeId.Value));
            }

            // Lọc theo giá (lấy giá nhỏ nhất từ ProductSizes)
            if (minPrice.HasValue)
            {
                query = query.Where(p => p.ProductSizes.Any(ps => ps.Price >= minPrice.Value));
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.ProductSizes.Any(ps => ps.Price <= maxPrice.Value));
            }

            // Lấy danh sách sản phẩm
            var products = await query.ToListAsync();

            // Đếm số lượng sản phẩm còn hàng và hết hàng
            ViewBag.InStockCount = products.Count(p => p.TotalStock > 0);
            ViewBag.OutOfStockCount = products.Count(p => p.TotalStock == 0);

            // Chuẩn bị dữ liệu cho các bộ lọc
            var brands = await _context.Brands
                .Select(b => new
                {
                    b.BrandId,
                    b.Name,
                    ProductCount = _context.Products.Count(p => p.BrandId == b.BrandId)
                })
                .ToListAsync();

            var availableSizes = await _context.Sizes
                .Select(s => new
                {
                    s.SizeId,
                    SizeDisplay = s.size,
                    ProductCount = _context.ProductSizes.Count(ps => ps.SizeId == s.SizeId)
                })
                .OrderBy(s => s.SizeDisplay)
                .ToListAsync();

            var categories = await _context.Categories
                .Select(c => new
                {
                    c.CategoryId,
                    c.Name,
                    ProductCount = _context.Products.Count(p => p.CategoryId == c.CategoryId)
                })
                .ToListAsync();

            // Tính toán số lượng sản phẩm trong mỗi khoảng giá dựa trên ProductSizes
            var priceRanges = new List<PriceRange>
            {
                new PriceRange { Min = 0M, Max = 500000M },
                new PriceRange { Min = 500000M, Max = 1000000M },
                new PriceRange { Min = 1000000M, Max = 2000000M },
                new PriceRange { Min = 2000000M, Max = 5000000M },
                new PriceRange { Min = 5000000M, Max = null }
            };

            var priceRangeCounts = priceRanges.Select(range => new
            {
                Range = range,
                Count = _context.ProductSizes
                    .Where(ps => ps.Price >= range.Min && (!range.Max.HasValue || ps.Price <= range.Max.Value))
                    .Select(ps => ps.ProductId)
                    .Distinct()
                    .Count()
            }).ToList();

            // Truyền dữ liệu cho view
            ViewBag.Brands = brands;
            ViewBag.Sizes = availableSizes;
            ViewBag.Categories = categories;
            ViewBag.PriceRanges = priceRangeCounts;
            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.SortOrder = sortOrder;
            ViewBag.SizeId = sizeId;

            // Gán ImageUrl cho mỗi sản phẩm
            foreach (var product in products)
            {
                product.ImageUrl = product.Images?.FirstOrDefault()?.ImageUrl ?? "/img/default-product.jpg";
            }

            // Lấy danh sách sản phẩm yêu thích từ session
            var favouriteProducts = HttpContext.Session.GetString("FavouriteProducts");
            ViewBag.FavouriteProducts = string.IsNullOrEmpty(favouriteProducts)
                ? new List<int>()
                : JsonConvert.DeserializeObject<List<int>>(favouriteProducts);

            return View(products);
        }

        // 2. Chi tiết sản phẩm
        public async Task<IActionResult> Detail(int id)
        {
            // Lấy sản phẩm hiện tại
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .Include(p => p.ProductSizes)
                .ThenInclude(ps => ps.Size)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            // Gán ImageUrl cho sản phẩm chính
            if (product.Images != null && product.Images.Any())
            {
                product.ImageUrl = product.Images.First().ImageUrl; // Lấy ảnh đầu tiên từ danh sách Images
            }

            // Lấy sản phẩm liên quan (cùng danh mục, trừ sản phẩm hiện tại)
            var relatedProducts = await _context.Products
                .Include(p => p.ProductSizes)
                .ThenInclude(ps => ps.Size)
                .Include(p => p.Images)
                .Include(p => p.Reviews)
                .Where(p => p.CategoryId == product.CategoryId && p.ProductId != product.ProductId)
                .Take(5) // Lấy 5 sản phẩm liên quan
                .ToListAsync();

            // Tạo danh sách RelatedProductsWithMinPrice và gán ImageUrl cho từng sản phẩm liên quan
            var relatedProductsWithMinPrice = relatedProducts.Select(p => new ProductWithMinPrice
            {
                Product = p,
                MinPrice = p.ProductSizes != null && p.ProductSizes.Any() ? p.ProductSizes.Min(ps => ps.Price) : 0
            }).ToList();

            // Gán ImageUrl cho từng sản phẩm liên quan
            foreach (var relatedProduct in relatedProductsWithMinPrice)
            {
                if (relatedProduct.Product.Images != null && relatedProduct.Product.Images.Any())
                {
                    relatedProduct.Product.ImageUrl = relatedProduct.Product.Images.First().ImageUrl; // Lấy ảnh đầu tiên
                }
            }

            // Gán vào ViewBag
            ViewBag.RelatedProductsWithMinPrice = relatedProductsWithMinPrice;

            return View(product);
        }

        // 3. Thêm vào danh sách yêu thích (dùng WishProductList)
        [HttpPost]
        public async Task<IActionResult> AddToWishlist(int productId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized("Vui lòng đăng nhập để thêm vào danh sách yêu thích.");
            }

            // Kiểm tra sản phẩm
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound("Sản phẩm không tồn tại.");
            }

            // Kiểm tra xem sản phẩm đã có trong danh sách yêu thích chưa
            var existingWish = await _context.WishProductLists
                .FirstOrDefaultAsync(w => w.UserId == user.Id && w.ProductId == productId);

            if (existingWish == null)
            {
                _context.WishProductLists.Add(new WishProductList
                {
                    UserId = user.Id,
                    ProductId = productId
                });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        // 4. Đánh giá sản phẩm
        [HttpPost]
        public async Task<IActionResult> SubmitReview(int productId, int rating, string comment)
        {
            // Kiểm tra sản phẩm
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại." });
            }

            // Kiểm tra giá trị đánh giá
            if (rating < 1 || rating > 5)
            {
                return Json(new { success = false, message = "Điểm đánh giá phải từ 1 đến 5." });
            }

            // Thêm đánh giá
            var review = new Review
            {
                ProductId = productId,
                UserId = "Anonymous", // Dùng giá trị mặc định vì không yêu cầu đăng nhập
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // 5. Thêm hoặc xóa sản phẩm yêu thích (dùng Session)
        [HttpPost]
        public IActionResult ToggleFavourite(int productId)
        {
            var favouriteProducts = HttpContext.Session.GetString("FavouriteProducts");
            var productIds = string.IsNullOrEmpty(favouriteProducts)
                ? new List<int>()
                : JsonConvert.DeserializeObject<List<int>>(favouriteProducts);

            if (productIds.Contains(productId))
            {
                productIds.Remove(productId);
            }
            else
            {
                productIds.Add(productId);
            }

            HttpContext.Session.SetString("FavouriteProducts", JsonConvert.SerializeObject(productIds));
            ViewBag.FavouriteCount = productIds.Count;

            return Ok(new { success = true, count = productIds.Count });
        }

        // 6. Hiển thị danh sách sản phẩm yêu thích
        public async Task<IActionResult> FavouriteProducts()
        {
            SetCartCount();

            // Lấy danh sách sản phẩm yêu thích từ session
            var favouriteProducts = HttpContext.Session.GetString("FavouriteProducts");
            if (string.IsNullOrEmpty(favouriteProducts))
            {
                return View(new List<Product>());
            }

            var productIds = JsonConvert.DeserializeObject<List<int>>(favouriteProducts);
            var products = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.ProductSizes)
                .ThenInclude(ps => ps.Size)
                .Where(p => productIds.Contains(p.ProductId))
                .ToListAsync();

            // Gán ImageUrl cho mỗi sản phẩm
            foreach (var product in products)
            {
                product.ImageUrl = product.Images?.FirstOrDefault()?.ImageUrl ?? "/img/default-product.jpg";
            }

            return View(products);
        }

        // Phương thức hỗ trợ: Đếm số lượng sản phẩm trong giỏ hàng
        private void SetCartCount()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            ViewBag.CartCount = cart.Sum(item => item.Quantity);
        }
    }

    public class PriceRange
    {
        public decimal Min { get; set; }
        public decimal? Max { get; set; }
    }
}