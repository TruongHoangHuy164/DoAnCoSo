using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DoAnLTW.Models;
using DoAnLTW.Models.Repositories;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DoAnLTW.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IBrandRepository _brandRepository;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext context,
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IBrandRepository brandRepository)
        {
            _logger = logger;
            _context = context;
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _brandRepository = brandRepository;
        }

        public async Task<IActionResult> Index()
        {
            SetCartCount();

            try
            {
                // Lấy danh sách danh mục và chuyển thành List
                var categories = await _categoryRepository.GetAllAsync();
                var categoryList = categories.ToList();

                // Lấy danh sách thương hiệu và chuyển thành List
                var brands = await _brandRepository.GetAllAsync();
                var brandList = brands.ToList();

                // Lấy tất cả sản phẩm và tính toán giá của kích thước nhỏ nhất
                var products = await _productRepository.GetAllAsync();

                // Sử dụng ProductWithMinPrice để lưu sản phẩm và giá của kích thước nhỏ nhất
                var productsWithMinPrice = products.Select(p => new ProductWithMinPrice
                {
                    Product = p,
                    MinPrice = p.ProductSizes.Min(ps => ps.Price)
                })
                .OrderBy(p => p.MinPrice) // Sắp xếp theo giá của kích thước nhỏ nhất
                .ToList();

                // Lấy 4 sản phẩm mới nhất (theo ProductId)
                var recentProducts = productsWithMinPrice
                    .OrderByDescending(p => p.Product.ProductId)
                    .Take(4)
                    .ToList();

                // Tạo ViewModel
                var viewModel = new HomeViewModel
                {
                    Categories = categoryList, // Assign List<Category>
                    Brands = brandList,        // Assign List<Brand>
                    Products = productsWithMinPrice.Select(p => p.Product).ToList(), // Lấy tối đa 8 sản phẩm nổi bật
                    RecentProducts = recentProducts.Select(p => p.Product).ToList(), // 4 sản phẩm mới nhất
                    ProductsWithMinPrice = productsWithMinPrice // Include products with their minimum prices
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading home page data");
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }




        public IActionResult Privacy()
        {
            SetCartCount();
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}