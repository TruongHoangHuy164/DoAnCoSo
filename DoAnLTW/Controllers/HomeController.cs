using System.Diagnostics;
using System.Linq;
using DoAnLTW.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DoAnLTW.Models.Repositories;

namespace DoAnLTW.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IProductRepository _productRepository;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IProductRepository productRepository)
        {
            _logger = logger;
            _context = context;
            _productRepository = productRepository;
        }

        public async Task<IActionResult> Index()
        {
            SetCartCount();

            // Lấy danh sách danh mục
            var categories = await _context.Categories
                .Include(c => c.Products)
                    .ThenInclude(p => p.ProductSizes)
                .ToListAsync();

            // Lấy danh sách sản phẩm
            var products = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.ProductSizes)
                    .ThenInclude(ps => ps.Size)
                .Include(p => p.Brand)
                .Select(p => new Product
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    Description = p.Description,
                    Brand = p.Brand,
                    CategoryId = p.CategoryId,
                    ProductSizes = p.ProductSizes,
                    ImageUrl = p.Images != null && p.Images.Any()
                        ? p.Images.First().ImageUrl
                        : null
                })
                .ToListAsync();

            // Lấy 4 sản phẩm mới nhất (Id giảm dần)
            var recentProducts = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.ProductSizes)
                    .ThenInclude(ps => ps.Size)
                .Include(p => p.Brand)
                .OrderByDescending(p => p.ProductId)
                .Take(4)
                .Select(p => new Product
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    Description = p.Description,
                    Brand = p.Brand,
                    CategoryId = p.CategoryId,
                    ProductSizes = p.ProductSizes,
                    ImageUrl = p.Images != null && p.Images.Any()
                        ? p.Images.First().ImageUrl
                        : null
                })
                .ToListAsync();

            // Tạo model ViewModel để truyền cả hai danh sách vào View
            var viewModel = new HomeViewModel
            {
                Categories = categories,
                Products = products,
                RecentProducts = recentProducts
            };

            return View(viewModel);
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
