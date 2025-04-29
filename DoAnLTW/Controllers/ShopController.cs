   
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
        public async Task<IActionResult> Index(string searchString, int? categoryId, int? brandId, int? sizeId, decimal? minPrice, decimal? maxPrice, string sortOrder = "name")
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
                // Loại bỏ dấu chấm cuối cùng và làm sạch chuỗi
                searchString = searchString.Trim().TrimEnd('.').ToLower();
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
        

            // Sắp xếp
            switch (sortOrder)
            { 
                //case "best_selling":
                //    query = query.OrderByDescending(p => p.OrderDetails.Sum(od => od.Quantity)); // Sắp xếp theo số lượng bán
                //    break;
                case "price_asc":
                    // Sắp xếp theo giá tăng dần (giá thấp nhất trước)
                    query = query.OrderBy(p => p.ProductSizes.Min(ps => ps.Price));
                    break;
                case "price_desc":
                    // Sắp xếp theo giá giảm dần (giá cao nhất trước)
                    query = query.OrderByDescending(p => p.ProductSizes.Min(ps => ps.Price));
                    break;
                case "name":
                    // Sắp xếp theo tên sản phẩm (A-Z)
                    query = query.OrderBy(p => p.Name);
                    break;
                // case "best_selling":
                //     query = query.OrderByDescending(p => p.OrderDetails.Sum(od => od.Quantity)); // Sắp xếp theo số lượng bán
                //     break;
                default:
                    query = query.OrderBy(p => p.Name); // Mặc định sắp xếp theo tên
                    break;
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
        /* public async Task<IActionResult> Details(int id)
         {
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

             return View(product);
         }*/

        public async Task<IActionResult> Details(int id)
        {
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

            // Lấy 4 sản phẩm liên quan trong cùng danh mục
            var relatedProducts = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .Include(p => p.ProductSizes)
                .ThenInclude(ps => ps.Size)
                .Include(p => p.Reviews)
                .Where(p => p.CategoryId == product.CategoryId && p.ProductId != id)
                .Take(4)
                .ToListAsync();

            // Gán ImageUrl cho các sản phẩm liên quan
            foreach (var relatedProduct in relatedProducts)
            {
                relatedProduct.ImageUrl = relatedProduct.Images?.FirstOrDefault()?.ImageUrl ?? "/img/default-product.jpg";
            }

            // Truyền danh sách sản phẩm liên quan vào ViewBag
            ViewBag.RelatedProducts = relatedProducts;

            // Lấy danh sách sản phẩm yêu thích từ session
            var favouriteProducts = HttpContext.Session.GetString("FavouriteProducts");
            ViewBag.FavouriteProducts = string.IsNullOrEmpty(favouriteProducts)
                ? new List<int>()
                : JsonConvert.DeserializeObject<List<int>>(favouriteProducts);

            return View(product);
        }

        // 3. Thêm vào giỏ hàng
        [HttpPost]
            public IActionResult AddToCart(int productId, int sizeId, int quantity)
            {
                // Kiểm tra sản phẩm và kích thước
                var productSize = _context.ProductSizes
                    .Include(ps => ps.Product)
                    .Include(ps => ps.Size)
                    .FirstOrDefault(ps => ps.ProductId == productId && ps.SizeId == sizeId);

                if (productSize == null || quantity <= 0 || productSize.Stock < quantity)
                {
                    return BadRequest("Sản phẩm hoặc số lượng không hợp lệ.");
                }

                // Lấy giỏ hàng từ Session
                var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();

                // Kiểm tra sản phẩm đã có trong giỏ hàng chưa
                var cartItem = cart.FirstOrDefault(c => c.ProductId == productId && c.Size == productSize.Size.size);
                if (cartItem != null)
                {
                    cartItem.Quantity += quantity;
                }
                else
                {
                    cart.Add(new CartItem
                    {
                        ProductId = productId,
                        ProductName = productSize.Product.Name,
                        Size = productSize.Size.size,
                        Price = productSize.Price,
                        Quantity = quantity
                    });
                }

                // Lưu giỏ hàng vào Session
                HttpContext.Session.SetObjectAsJson("Cart", cart);

                return RedirectToAction("Index");
            }

            // 4. Thêm vào danh sách yêu thích (dùng WishProductList)
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

            // 5. Đánh giá sản phẩm
            [HttpPost]
            public async Task<IActionResult> AddReview(int productId, int rating, string comment)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized("Vui lòng đăng nhập để đánh giá sản phẩm.");
                }

                // Kiểm tra sản phẩm
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return NotFound("Sản phẩm không tồn tại.");
                }

                // Kiểm tra giá trị đánh giá
                if (rating < 1 || rating > 5)
                {
                    return BadRequest("Điểm đánh giá phải từ 1 đến 5.");
                }

                // Thêm đánh giá
                var review = new Review
                {
                    ProductId = productId,
                    UserId = user.Id,
                    Rating = rating,
                    Comment = comment,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                return RedirectToAction("Details", new { id = productId });
            }

            // 6. Thêm hoặc xóa sản phẩm yêu thích (dùng Session)
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

            // 7. Hiển thị danh sách sản phẩm yêu thích
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