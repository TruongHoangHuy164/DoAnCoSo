using DoAnLTW.Areas.Admin.Models;
using DoAnLTW.Models;
using DoAnLTW.Models.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnLTW.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IOrderRepository _orderRepository;
        private readonly IPetServiceRepository _petServiceRepository;
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly ILogger<StatisticsController> _logger;

        public StatisticsController(
            ApplicationDbContext context,
            IOrderRepository orderRepository,
            IPetServiceRepository petServiceRepository,
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IBrandRepository brandRepository,
            IServiceRepository serviceRepository,
            ILogger<StatisticsController> logger)
        {
            _context = context;
            _orderRepository = orderRepository;
            _petServiceRepository = petServiceRepository;
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _brandRepository = brandRepository;
            _serviceRepository = serviceRepository;
            _logger = logger;
        }

        // GET: Admin/Statistics
        public async Task<IActionResult> Index(int? year)
        {
            try
            {
                _logger.LogInformation("Starting statistics retrieval for year {Year}", year ?? DateTime.Now.Year);

                var model = new StatisticsViewModel();

                // Lấy năm hiện tại nếu không có bộ lọc
                year ??= DateTime.Now.Year;

                // Lấy danh sách các năm có dữ liệu
                var orderYears = await _context.Orders
                    .AsNoTracking()
                    .Select(o => o.OrderDate.Year)
                    .Distinct()
                    .ToListAsync();
                var petServiceYears = await _context.PetServices
                    .AsNoTracking()
                    .Select(ps => ps.BookingDate.Year)
                    .Distinct()
                    .ToListAsync();
                model.AvailableYears = orderYears.Union(petServiceYears).OrderByDescending(y => y).ToList();
                model.SelectedYear = year;

                // Tổng quan
                model.TotalOrders = await _context.Orders.AsNoTracking().CountAsync();
                model.TotalPetServices = await _context.PetServices.AsNoTracking().CountAsync();
                model.TotalProducts = await _context.Products.AsNoTracking().CountAsync();

                // Tổng doanh thu
                model.OrderRevenue = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.OrderDate.Year == year)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
                model.PetServiceRevenue = await _context.PetServices
                    .AsNoTracking()
                    .Where(ps => ps.BookingDate.Year == year)
                    .SumAsync(ps => (decimal?)ps.Price) ?? 0;
                model.TotalRevenue = model.OrderRevenue + model.PetServiceRevenue;

                // Doanh thu theo danh mục
                var revenueByCategory = await _context.OrderDetails
                    .AsNoTracking()
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

                // Doanh thu theo thương hiệu
                var revenueByBrand = await _context.OrderDetails
                    .AsNoTracking()
                    .Include(od => od.Product)
                    .ThenInclude(p => p.Brand)
                    .Where(od => od.Order.OrderDate.Year == year)
                    .GroupBy(od => od.Product.Brand.Name)
                    .Select(g => new
                    {
                        Brand = g.Key ?? "Không có thương hiệu",
                        Revenue = g.Sum(od => od.Quantity * od.Price)
                    })
                    .ToDictionaryAsync(k => k.Brand, v => v.Revenue);

                model.RevenueByBrand = revenueByBrand.Any()
                    ? revenueByBrand
                    : new Dictionary<string, decimal> { { "Không có dữ liệu", 0 } };

                // Số đơn hàng theo trạng thái
                var ordersByStatus = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.OrderDate.Year == year)
                    .GroupBy(o => o.Status)
                    .Select(g => new
                    {
                        Status = g.Key.ToString(),
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(k => k.Status, v => v.Count);

                model.OrdersByStatus = ordersByStatus.Any()
                    ? ordersByStatus
                    : new Dictionary<string, int> { { "Không có dữ liệu", 0 } };

                // Số dịch vụ thú cưng theo trạng thái
                var petServicesByStatus = await _context.PetServices
                    .AsNoTracking()
                    .Where(ps => ps.BookingDate.Year == year)
                    .GroupBy(ps => ps.Status)
                    .Select(g => new
                    {
                        Status = g.Key.ToString(),
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(k => k.Status, v => v.Count);

                model.PetServicesByStatus = petServicesByStatus.Any()
                    ? petServicesByStatus
                    : new Dictionary<string, int> { { "Không có dữ liệu", 0 } };

                // Số sản phẩm theo thương hiệu
                var productsByBrand = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Brand)
                    .GroupBy(p => p.Brand.Name)
                    .Select(g => new
                    {
                        Brand = g.Key ?? "Không có thương hiệu",
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(k => k.Brand, v => v.Count);

                model.ProductsByBrand = productsByBrand.Any()
                    ? productsByBrand
                    : new Dictionary<string, int> { { "Không có dữ liệu", 0 } };

                // Số lượng sản phẩm bán ra theo danh mục
                var soldProductsByCategory = await _context.OrderDetails
                    .AsNoTracking()
                    .Include(od => od.Product)
                    .ThenInclude(p => p.Category)
                    .Where(od => od.Order.OrderDate.Year == year)
                    .GroupBy(od => od.Product.Category.Name)
                    .Select(g => new
                    {
                        Category = g.Key,
                        Quantity = g.Sum(od => od.Quantity)
                    })
                    .ToDictionaryAsync(k => k.Category, v => v.Quantity);

                model.SoldProductsByCategory = soldProductsByCategory.Any()
                    ? soldProductsByCategory
                    : new Dictionary<string, int> { { "Không có dữ liệu", 0 } };

                // Số lượng dịch vụ thú cưng theo loại dịch vụ
                var petServicesByServiceType = await _context.PetServices
                    .AsNoTracking()
                    .Include(ps => ps.Service)
                    .Where(ps => ps.BookingDate.Year == year)
                    .GroupBy(ps => ps.Service.Name)
                    .Select(g => new
                    {
                        ServiceName = g.Key,
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(k => k.ServiceName, v => v.Count);

                model.PetServicesByServiceType = petServicesByServiceType.Any()
                    ? petServicesByServiceType
                    : new Dictionary<string, int> { { "Không có dữ liệu", 0 } };

                // Doanh thu theo tháng
                var orderRevenueByMonth = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.OrderDate.Year == year)
                    .GroupBy(o => o.OrderDate.Month)
                    .Select(g => new { Month = g.Key, Revenue = g.Sum(o => o.TotalAmount) })
                    .ToDictionaryAsync(k => k.Month, v => v.Revenue);

                var petServiceRevenueByMonth = await _context.PetServices
                    .AsNoTracking()
                    .Where(ps => ps.BookingDate.Year == year)
                    .GroupBy(ps => ps.BookingDate.Month)
                    .Select(g => new { Month = g.Key, Revenue = g.Sum(ps => ps.Price) })
                    .ToDictionaryAsync(k => k.Month, v => v.Revenue);

                model.RevenueByMonth = Enumerable.Range(1, 12)
                    .ToDictionary(
                        month => $"Tháng {month}",
                        month =>
                        {
                            var orderRev = orderRevenueByMonth.ContainsKey(month) ? orderRevenueByMonth[month] : 0;
                            var petRev = petServiceRevenueByMonth.ContainsKey(month) ? petServiceRevenueByMonth[month] : 0;
                            return orderRev + petRev;
                        });

                // Doanh thu theo quý
                var orderRevenueByQuarter = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.OrderDate.Year == year)
                    .GroupBy(o => (o.OrderDate.Month - 1) / 3 + 1)
                    .Select(g => new { Quarter = g.Key, Revenue = g.Sum(o => o.TotalAmount) })
                    .ToDictionaryAsync(k => k.Quarter, v => v.Revenue);

                var petServiceRevenueByQuarter = await _context.PetServices
                    .AsNoTracking()
                    .Where(ps => ps.BookingDate.Year == year)
                    .GroupBy(ps => (ps.BookingDate.Month - 1) / 3 + 1)
                    .Select(g => new { Quarter = g.Key, Revenue = g.Sum(ps => ps.Price) })
                    .ToDictionaryAsync(k => k.Quarter, v => v.Revenue);

                model.RevenueByQuarter = Enumerable.Range(1, 4)
                    .ToDictionary(
                        quarter => $"Quý {quarter}",
                        quarter =>
                        {
                            var orderRev = orderRevenueByQuarter.ContainsKey(quarter) ? orderRevenueByQuarter[quarter] : 0;
                            var petRev = petServiceRevenueByQuarter.ContainsKey(quarter) ? petServiceRevenueByQuarter[quarter] : 0;
                            return orderRev + petRev;
                        });

                // Top 5 sản phẩm bán chạy nhất
                model.TopSellingProducts = await _context.OrderDetails
                    .AsNoTracking()
                    .Include(od => od.Product)
                    .Where(od => od.Order.OrderDate.Year == year)
                    .GroupBy(od => new { od.ProductId, od.Product.Name })
                    .Select(g => new ProductSalesModel
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = g.Key.Name,
                        QuantitySold = g.Sum(od => od.Quantity),
                        Revenue = g.Sum(od => od.Quantity * od.Price)
                    })
                    .OrderByDescending(p => p.QuantitySold)
                    .Take(5)
                    .ToListAsync();

                // Top 5 dịch vụ phổ biến nhất
                model.TopPopularServices = await _context.PetServices
                    .AsNoTracking()
                    .Include(ps => ps.Service)
                    .Where(ps => ps.BookingDate.Year == year)
                    .GroupBy(ps => new { ps.ServiceId, ps.Service.Name })
                    .Select(g => new ServicePopularityModel
                    {
                        ServiceId = g.Key.ServiceId,
                        ServiceName = g.Key.Name,
                        BookingCount = g.Count(),
                        Revenue = g.Sum(ps => ps.Price)
                    })
                    .OrderByDescending(s => s.BookingCount)
                    .Take(5)
                    .ToListAsync();

                _logger.LogInformation("Statistics retrieval completed successfully for year {Year}", year);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving statistics for year {Year}", year);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải dữ liệu thống kê. Vui lòng thử lại sau.";
                return View(new StatisticsViewModel
                {
                    AvailableYears = new List<int> { DateTime.Now.Year },
                    SelectedYear = year,
                    RevenueByCategory = new Dictionary<string, decimal> { { "Không có dữ liệu", 0 } },
                    RevenueByBrand = new Dictionary<string, decimal> { { "Không có dữ liệu", 0 } },
                    OrdersByStatus = new Dictionary<string, int> { { "Không có dữ liệu", 0 } },
                    PetServicesByStatus = new Dictionary<string, int> { { "Không có dữ liệu", 0 } },
                    ProductsByBrand = new Dictionary<string, int> { { "Không có dữ liệu", 0 } },
                    SoldProductsByCategory = new Dictionary<string, int> { { "Không có dữ liệu", 0 } },
                    PetServicesByServiceType = new Dictionary<string, int> { { "Không có dữ liệu", 0 } },
                    RevenueByMonth = Enumerable.Range(1, 12).ToDictionary(m => $"Tháng {m}", m => (decimal)0),
                    RevenueByQuarter = Enumerable.Range(1, 4).ToDictionary(q => $"Quý {q}", q => (decimal)0),
                    TopSellingProducts = new List<ProductSalesModel>(),
                    TopPopularServices = new List<ServicePopularityModel>()
                });
            }
        }
    }
}