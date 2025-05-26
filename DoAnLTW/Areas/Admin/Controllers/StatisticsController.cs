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
        private readonly ILogger _logger;

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
        public async Task<IActionResult> Index(int? year, int? month)
        {
            try
            {
                _logger.LogInformation("Starting statistics retrieval for year {Year}, month {Month}", year ?? DateTime.Now.Year, month ?? 0);

                var model = new StatisticsViewModel();

                // Lấy năm và tháng hiện tại nếu không có bộ lọc
                var currentDate = DateTime.Now;
                year ??= currentDate.Year;
                month ??= month.HasValue ? month : null;

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
                var petHotelBookingYears = await _context.PetHotelBookings
                    .AsNoTracking()
                    .Select(phb => phb.BookingDate.Year)
                    .Distinct()
                    .ToListAsync();
                model.AvailableYears = orderYears.Union(petServiceYears).Union(petHotelBookingYears).OrderByDescending(y => y).ToList();
                model.SelectedYear = year;
                model.SelectedMonth = month;

                // Tổng quan (luôn tính tổng số lượng tất cả thời gian)
                model.TotalOrders = await _context.Orders.AsNoTracking().CountAsync();
                model.TotalPetServices = await _context.PetServices.AsNoTracking().CountAsync();
                model.TotalPetHotelBookings = await _context.PetHotelBookings.AsNoTracking().CountAsync();
                model.TotalProducts = await _context.Products.AsNoTracking().CountAsync();

                // Điều kiện lọc theo năm và tháng
                var ordersQuery = _context.Orders.AsNoTracking().Where(o => o.OrderDate.Year == year);
                var petServicesQuery = _context.PetServices.AsNoTracking().Where(ps => ps.BookingDate.Year == year);
                var petHotelBookingsQuery = _context.PetHotelBookings.AsNoTracking().Where(phb => phb.BookingDate.Year == year && phb.Status != PetHotelBookingStatus.DaHuy);
                var orderDetailsQuery = _context.OrderDetails.AsNoTracking().Where(od => od.Order.OrderDate.Year == year);

                if (month.HasValue)
                {
                    ordersQuery = ordersQuery.Where(o => o.OrderDate.Month == month);
                    petServicesQuery = petServicesQuery.Where(ps => ps.BookingDate.Month == month);
                    petHotelBookingsQuery = petHotelBookingsQuery.Where(phb => phb.BookingDate.Month == month);
                    orderDetailsQuery = orderDetailsQuery.Where(od => od.Order.OrderDate.Month == month);
                }

                // Tổng doanh thu
                model.OrderRevenue = await ordersQuery.SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
                model.PetServiceRevenue = await petServicesQuery.SumAsync(ps => (decimal?)ps.Price) ?? 0;
                model.PetHotelBookingRevenue = await petHotelBookingsQuery.SumAsync(phb => (decimal?)phb.TotalPrice) ?? 0;
                model.TotalRevenue = model.OrderRevenue + model.PetServiceRevenue + model.PetHotelBookingRevenue;

                // Doanh thu theo danh mục
                var revenueByCategory = await orderDetailsQuery
                    .Include(od => od.Product)
                    .ThenInclude(p => p.Category)
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
                var revenueByBrand = await orderDetailsQuery
                    .Include(od => od.Product)
                    .ThenInclude(p => p.Brand)
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

                // Doanh thu theo loại phòng
                var revenueByRoomType = await petHotelBookingsQuery
                    .Include(phb => phb.Room)
                    .ThenInclude(r => r.RoomType)
                    .GroupBy(phb => phb.Room.RoomType.Name)
                    .Select(g => new
                    {
                        RoomType = g.Key,
                        Revenue = g.Sum(phb => phb.TotalPrice)
                    })
                    .ToDictionaryAsync(k => k.RoomType, v => v.Revenue);

                model.RevenueByRoomType = revenueByRoomType.Any()
                    ? revenueByRoomType
                    : new Dictionary<string, decimal> { { "Không có dữ liệu", 0 } };

                // Số đơn hàng theo trạng thái
                var ordersByStatus = await ordersQuery
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
                var petServicesByStatus = await petServicesQuery
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

                // Số đặt phòng khách sạn thú cưng theo trạng thái
                var petHotelBookingsByStatusQuery = _context.PetHotelBookings.AsNoTracking().Where(phb => phb.BookingDate.Year == year);
                if (month.HasValue)
                {
                    petHotelBookingsByStatusQuery = petHotelBookingsByStatusQuery.Where(phb => phb.BookingDate.Month == month);
                }
                var petHotelBookingsByStatus = await petHotelBookingsByStatusQuery
                    .GroupBy(phb => phb.Status)
                    .Select(g => new
                    {
                        Status = g.Key.ToString(),
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(k => k.Status, v => v.Count);

                model.PetHotelBookingsByStatus = petHotelBookingsByStatus.Any()
                    ? petHotelBookingsByStatus
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
                var soldProductsByCategory = await orderDetailsQuery
                    .Include(od => od.Product)
                    .ThenInclude(p => p.Category)
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
                var petServicesByServiceType = await petServicesQuery
                    .Include(ps => ps.Service)
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

                var petHotelBookingRevenueByMonth = await _context.PetHotelBookings
                    .AsNoTracking()
                    .Where(phb => phb.BookingDate.Year == year && phb.Status != PetHotelBookingStatus.DaHuy)
                    .GroupBy(phb => phb.BookingDate.Month)
                    .Select(g => new { Month = g.Key, Revenue = g.Sum(phb => phb.TotalPrice) })
                    .ToDictionaryAsync(k => k.Month, v => v.Revenue);

                // Số đơn hàng theo tháng (new)
                var ordersByMonth = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.OrderDate.Year == year)
                    .GroupBy(o => o.OrderDate.Month)
                    .Select(g => new { Month = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(k => k.Month, v => v.Count);

                if (month.HasValue)
                {
                    model.RevenueByMonth = new Dictionary<string, decimal>
                    {
                        { $"Tháng {month}", (orderRevenueByMonth.ContainsKey(month.Value) ? orderRevenueByMonth[month.Value] : 0) +
                                            (petServiceRevenueByMonth.ContainsKey(month.Value) ? petServiceRevenueByMonth[month.Value] : 0) +
                                            (petHotelBookingRevenueByMonth.ContainsKey(month.Value) ? petHotelBookingRevenueByMonth[month.Value] : 0) }
                    };
                    model.OrdersByMonth = new Dictionary<string, int>
                    {
                        { $"Tháng {month}", ordersByMonth.ContainsKey(month.Value) ? ordersByMonth[month.Value] : 0 }
                    };
                }
                else
                {
                    model.RevenueByMonth = Enumerable.Range(1, 12)
                        .ToDictionary(
                            m => $"Tháng {m}",
                            m =>
                            {
                                var orderRev = orderRevenueByMonth.ContainsKey(m) ? orderRevenueByMonth[m] : 0;
                                var petRev = petServiceRevenueByMonth.ContainsKey(m) ? petServiceRevenueByMonth[m] : 0;
                                var hotelRev = petHotelBookingRevenueByMonth.ContainsKey(m) ? petHotelBookingRevenueByMonth[m] : 0;
                                return orderRev + petRev + hotelRev;
                            });
                    model.OrdersByMonth = Enumerable.Range(1, 12)
                        .ToDictionary(
                            m => $"Tháng {m}",
                            m => ordersByMonth.ContainsKey(m) ? ordersByMonth[m] : 0);
                }

                // Doanh thu theo quý
                if (!month.HasValue)
                {
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

                    var petHotelBookingRevenueByQuarter = await _context.PetHotelBookings
                        .AsNoTracking()
                        .Where(phb => phb.BookingDate.Year == year && phb.Status != PetHotelBookingStatus.DaHuy)
                        .GroupBy(phb => (phb.BookingDate.Month - 1) / 3 + 1)
                        .Select(g => new { Quarter = g.Key, Revenue = g.Sum(phb => phb.TotalPrice) })
                        .ToDictionaryAsync(k => k.Quarter, v => v.Revenue);

                    model.RevenueByQuarter = Enumerable.Range(1, 4)
                        .ToDictionary(
                            quarter => $"Quý {quarter}",
                            quarter =>
                            {
                                var orderRev = orderRevenueByQuarter.ContainsKey(quarter) ? orderRevenueByQuarter[quarter] : 0;
                                var petRev = petServiceRevenueByQuarter.ContainsKey(quarter) ? petServiceRevenueByQuarter[quarter] : 0;
                                var hotelRev = petHotelBookingRevenueByQuarter.ContainsKey(quarter) ? petHotelBookingRevenueByQuarter[quarter] : 0;
                                return orderRev + petRev + hotelRev;
                            });
                }
                else
                {
                    model.RevenueByQuarter = new Dictionary<string, decimal>();
                }

                // Top 5 sản phẩm bán chạy nhất
                model.TopSellingProducts = await orderDetailsQuery
                    .Include(od => od.Product)
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
                model.TopPopularServices = await petServicesQuery
                    .Include(ps => ps.Service)
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

                // Top 5 loại phòng phổ biến nhất
                model.TopPopularRoomTypes = await petHotelBookingsQuery
                    .Include(phb => phb.Room)
                    .ThenInclude(r => r.RoomType)
                    .GroupBy(phb => new { phb.Room.RoomType.RoomTypeId, phb.Room.RoomType.Name })
                    .Select(g => new RoomTypePopularityModel
                    {
                        RoomTypeId = g.Key.RoomTypeId,
                        RoomTypeName = g.Key.Name,
                        BookingCount = g.Count(),
                        Revenue = g.Sum(phb => phb.TotalPrice)
                    })
                    .OrderByDescending(r => r.BookingCount)
                    .Take(5)
                    .ToListAsync();

                _logger.LogInformation("Statistics retrieval completed successfully for year {Year}, month {Month}", year, month ?? 0);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving statistics for year {Year}, month {Month}", year, month ?? 0);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải dữ liệu thống kê. Vui lòng thử lại sau.";
                return View(new StatisticsViewModel
                {
                    AvailableYears = new List<int> { DateTime.Now.Year },
                    SelectedYear = year,
                    SelectedMonth = month,
                    RevenueByCategory = new Dictionary<string, decimal> { { "Không có dữ liệu", 0 } },
                    RevenueByBrand = new Dictionary<string, decimal> { { "Không có dữ liệu", 0 } },
                    RevenueByRoomType = new Dictionary<string, decimal> { { "Không có dữ liệu", 0 } },
                    OrdersByStatus = new Dictionary<string, int> { { "Không có dữ liệu", 0 } },
                    PetServicesByStatus = new Dictionary<string, int> { { "Không có dữ liệu", 0 } },
                    PetHotelBookingsByStatus = new Dictionary<string, int> { { "Không có dữ liệu", 0 } },
                    ProductsByBrand = new Dictionary<string, int> { { "Không có dữ liệu", 0 } },
                    SoldProductsByCategory = new Dictionary<string, int> { { "Không có dữ liệu", 0 } },
                    PetServicesByServiceType = new Dictionary<string, int> { { "Không có dữ liệu", 0 } },
                    RevenueByMonth = month.HasValue ? new Dictionary<string, decimal> { { $"Tháng {month}", 0 } } : Enumerable.Range(1, 12).ToDictionary(m => $"Tháng {m}", m => (decimal)0),
                    OrdersByMonth = month.HasValue ? new Dictionary<string, int> { { $"Tháng {month}", 0 } } : Enumerable.Range(1, 12).ToDictionary(m => $"Tháng {m}", m => 0),
                    RevenueByQuarter = month.HasValue ? new Dictionary<string, decimal>() : Enumerable.Range(1, 4).ToDictionary(q => $"Quý {q}", q => (decimal)0),
                    TopSellingProducts = new List<ProductSalesModel>(),
                    TopPopularServices = new List<ServicePopularityModel>(),
                    TopPopularRoomTypes = new List<RoomTypePopularityModel>()
                });
            }
        }
    }
}