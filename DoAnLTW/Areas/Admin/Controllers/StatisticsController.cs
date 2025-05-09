using DoAnLTW.Models.Repositories;
using DoAnLTW.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DoAnLTW.Areas.Admin.Models;
using Microsoft.EntityFrameworkCore;

namespace DoAnLTW.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IOrderRepository _orderRepository;
        private readonly IPetServiceRepository _petServiceRepository;
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IBrandRepository _brandRepository;

        public StatisticsController(
            ApplicationDbContext context,
            IOrderRepository orderRepository,
            IPetServiceRepository petServiceRepository,
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IBrandRepository brandRepository)
        {
            _context = context;
            _orderRepository = orderRepository;
            _petServiceRepository = petServiceRepository;
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _brandRepository = brandRepository;
        }

        // GET: Admin/Statistics
        public async Task<IActionResult> Index(int? year)
        {
            var model = new StatisticsViewModel();

            // Lấy năm hiện tại nếu không có bộ lọc
            year ??= DateTime.Now.Year;

            // Lấy danh sách các năm có dữ liệu
            var orderYears = await _context.Orders
                .Select(o => o.OrderDate.Year)
                .Distinct()
                .ToListAsync();

            var petServiceYears = await _context.PetServices
                .Select(ps => ps.BookingDate.Year)
                .Distinct()
                .ToListAsync();

            model.AvailableYears = orderYears
                .Union(petServiceYears)
                .OrderByDescending(y => y)
                .ToList();

            model.SelectedYear = year;

            // Tổng quan
            model.TotalOrders = await _context.Orders.CountAsync();
            model.TotalPetServices = await _context.PetServices.CountAsync();
            model.TotalProducts = await _context.Products.CountAsync();

            // Tổng doanh thu
            var orderRevenue = await _context.Orders
                .Where(o => o.OrderDate.Year == year)
                .SumAsync(o => o.TotalAmount);

            var petServiceRevenue = await _context.PetServices
                .Where(ps => ps.BookingDate.Year == year)
                .SumAsync(ps => ps.Price);

            model.TotalRevenue = orderRevenue + petServiceRevenue;

            // Doanh thu theo danh mục
            var revenueByCategory = await _context.OrderDetails
                .Include(od => od.Product)
                .ThenInclude(p => p.Category)
                .Where(od => od.Order.OrderDate.Year == year)
                .GroupBy(od => od.Product.Category.Name)
                .Select(g => new
                {
                    Category = g.Key,
                    Revenue = g.Sum(od => od.Quantity * od.Price)
                })
                .ToDictionaryAsync(k => k.Category, v => v.Revenue);

            model.RevenueByCategory = revenueByCategory.Any()
                ? revenueByCategory
                : new Dictionary<string, decimal> { { "Không có dữ liệu", 0 } };

            // Số đơn hàng theo trạng thái
            model.OrdersByStatus = await _context.Orders
                .Where(o => o.OrderDate.Year == year)
                .GroupBy(o => o.Status)
                .Select(g => new
                {
                    Status = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToDictionaryAsync(k => k.Status, v => v.Count);

            // Số dịch vụ thú cưng theo trạng thái
            model.PetServicesByStatus = await _context.PetServices
                .Where(ps => ps.BookingDate.Year == year)
                .GroupBy(ps => ps.Status)
                .Select(g => new
                {
                    Status = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToDictionaryAsync(k => k.Status, v => v.Count);

            // Số sản phẩm theo thương hiệu
            model.ProductsByBrand = await _context.Products
                .Include(p => p.Brand)
                .GroupBy(p => p.Brand.Name)
                .Select(g => new
                {
                    Brand = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(k => k.Brand, v => v.Count);

            // Doanh thu theo tháng trong năm được chọn
            var revenueByMonth = await _context.Orders
                .Where(o => o.OrderDate.Year == year)
                .GroupBy(o => o.OrderDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .ToDictionaryAsync(k => $"Tháng {k.Month}", v => v.Revenue);

            var petServiceRevenueByMonth = await _context.PetServices
                .Where(ps => ps.BookingDate.Year == year)
                .GroupBy(ps => ps.BookingDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Revenue = g.Sum(ps => ps.Price)
                })
                .ToDictionaryAsync(k => k.Month, v => v.Revenue);

            // Kết hợp doanh thu từ Orders và PetServices
            model.RevenueByMonth = new Dictionary<string, decimal>();

            for (int month = 1; month <= 12; month++)
            {
                var monthKey = $"Tháng {month}";
                var orderRev = revenueByMonth.ContainsKey(monthKey) ? revenueByMonth[monthKey] : 0;
                var petRev = petServiceRevenueByMonth.ContainsKey(month) ? petServiceRevenueByMonth[month] : 0;

                model.RevenueByMonth[monthKey] = orderRev + petRev;
            }

            return View(model);
        }
    }
}
