namespace DoAnLTW.Areas.Admin.Models
{
    public class StatisticsViewModel
    {
        // Tổng quan
        public decimal TotalRevenue { get; set; }                 // Tổng doanh thu
        public decimal OrderRevenue { get; set; }                 // Doanh thu từ đơn hàng
        public decimal PetServiceRevenue { get; set; }            // Doanh thu từ dịch vụ thú cưng
        public int TotalOrders { get; set; }                      // Tổng số đơn hàng
        public int TotalPetServices { get; set; }                 // Tổng số dịch vụ thú cưng
        public int TotalProducts { get; set; }                    // Tổng số sản phẩm

        // Dữ liệu cho biểu đồ
        public Dictionary<string, decimal> RevenueByCategory { get; set; }          // Doanh thu theo danh mục
        public Dictionary<string, decimal> RevenueByBrand { get; set; }             // Doanh thu theo thương hiệu
        public Dictionary<string, int> OrdersByStatus { get; set; }                 // Số đơn hàng theo trạng thái
        public Dictionary<string, int> PetServicesByStatus { get; set; }            // Số dịch vụ theo trạng thái
        public Dictionary<string, int> ProductsByBrand { get; set; }                // Số sản phẩm theo thương hiệu
        public Dictionary<string, decimal> RevenueByMonth { get; set; }             // Doanh thu theo tháng
        public Dictionary<string, decimal> RevenueByQuarter { get; set; }           // Doanh thu theo quý
        public Dictionary<string, int> SoldProductsByCategory { get; set; }         // Số lượng sản phẩm bán ra theo danh mục
        public Dictionary<string, int> PetServicesByServiceType { get; set; }       // Số lượng dịch vụ theo loại dịch vụ
        public List<ProductSalesModel> TopSellingProducts { get; set; }             // Top 5 sản phẩm bán chạy
        public List<ServicePopularityModel> TopPopularServices { get; set; }        // Top 5 dịch vụ phổ biến

        // Bộ lọc
        public int? SelectedYear { get; set; }
        public List<int> AvailableYears { get; set; }
    }

    public class ProductSalesModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class ServicePopularityModel
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public int BookingCount { get; set; }
        public decimal Revenue { get; set; }
    }
}
