using System.Collections.Generic;

namespace DoAnLTW.Areas.Admin.Models
{
    public class StatisticsViewModel
    {
        public int? SelectedYear { get; set; }
        public int? SelectedMonth { get; set; }
        public List<int> AvailableYears { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal OrderRevenue { get; set; }
        public decimal PetServiceRevenue { get; set; }
        public decimal PetHotelBookingRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalPetServices { get; set; }
        public int TotalPetHotelBookings { get; set; }
        public int TotalProducts { get; set; }
        public Dictionary<string, decimal> RevenueByCategory { get; set; }
        public Dictionary<string, decimal> RevenueByBrand { get; set; }
        public Dictionary<string, decimal> RevenueByRoomType { get; set; }
        public Dictionary<string, int> OrdersByStatus { get; set; }
        public Dictionary<string, int> PetServicesByStatus { get; set; }
        public Dictionary<string, int> PetHotelBookingsByStatus { get; set; }
        public Dictionary<string, int> ProductsByBrand { get; set; }
        public Dictionary<string, int> SoldProductsByCategory { get; set; }
        public Dictionary<string, int> PetServicesByServiceType { get; set; }
        public Dictionary<string, decimal> RevenueByMonth { get; set; }
        public Dictionary<string, decimal> RevenueByQuarter { get; set; }
        // New property for order counts by month
        public Dictionary<string, int> OrdersByMonth { get; set; }
        public List<ProductSalesModel> TopSellingProducts { get; set; }
        public List<ServicePopularityModel> TopPopularServices { get; set; }
        public List<RoomTypePopularityModel> TopPopularRoomTypes { get; set; }
    }

    // Supporting models (assumed based on controller usage)
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

    public class RoomTypePopularityModel
    {
        public int RoomTypeId { get; set; }
        public string RoomTypeName { get; set; }
        public int BookingCount { get; set; }
        public decimal Revenue { get; set; }
    }
}