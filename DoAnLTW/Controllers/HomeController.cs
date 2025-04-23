using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DoAnLTW.Models;
using DoAnLTW.Models.Repositories;
using DoAnLTW.ViewModels;
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

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext context,
            IProductRepository productRepository,
            ICategoryRepository categoryRepository)
        {
            _logger = logger;
            _context = context;
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        public async Task<IActionResult> Index()
        {
            SetCartCount();

            try
            {
                // Lấy danh sách danh mục và chuyển thành List
                var categories = await _categoryRepository.GetAllAsync();
                var categoryList = categories.ToList(); // Explicitly convert to List<Category>

                // Lấy tất cả sản phẩm (giới hạn để tối ưu)
                var products = await _productRepository.GetAllAsync();

                // Lấy 4 sản phẩm mới nhất
                var recentProducts = products
                    .OrderByDescending(p => p.ProductId)
                    .Take(4)
                    .ToList();

                // Tạo ViewModel
                var viewModel = new HomeViewModel
                {
                    Categories = categoryList, // Assign List<Category>
                    Products = products.Take(8).ToList(), // Lấy tối đa 8 sản phẩm nổi bật
                    RecentProducts = recentProducts
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