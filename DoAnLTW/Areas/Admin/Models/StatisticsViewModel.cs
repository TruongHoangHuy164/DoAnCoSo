namespace DoAnLTW.Areas.Admin.Models
{
    public class StatisticsViewModel
    {// Tổng quan
      public decimal TotalRevenue { get; set; } 
        // Tổng doanh thu (Orders + PetServices)
        public int TotalOrders { get; set; } 
        // Tổng số đơn hàng
        public int TotalPetServices { get; set; } 
        // Tổng số dịch vụ thú cưng
        public int TotalProducts { get; set; }
        // Tổng số sản phẩm
        // Dữ liệu cho biểu đồ
        public Dictionary<string, decimal> RevenueByCategory { get; set; } // Doanh thu theo danh mục
        public Dictionary<string, int> OrdersByStatus { get; set; } // Số đơn hàng theo trạng thái
        public Dictionary<string, int> PetServicesByStatus { get; set; } // Số dịch vụ theo trạng thái
        public Dictionary<string, int> ProductsByBrand { get; set; } // Số sản phẩm theo thương hiệu
        public Dictionary<string, decimal> RevenueByMonth { get; set; } // Doanh thu theo tháng

        // Bộ lọc
        public int? SelectedYear { get; set; }
        public List<int> AvailableYears { get; set; }
    
}
}
