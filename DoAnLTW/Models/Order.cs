
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DoAnLTW.Models
{
    public enum OrderStatus
    {
        [Display(Name = "Chờ xử lý")]
        ChoXuLy, //0

        [Display(Name = "Đang giao hàng")]
        DangGiaoHang,//1

        [Display(Name = "Đã giao hàng")]
        DaGiaoHang,//2
        [Display(Name = "Đã hủy")]
        DaHuy //3
    }

    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [RegularExpression(@"^\+?\d{10,15}$", ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; }

        [Required]
        public string Address { get; set; }

        public string AlternateAddress { get; set; }

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Tổng tiền phải lớn hơn hoặc bằng 0")]
        public decimal TotalAmount { get; set; }

        [Required]
        public string PaymentMethod { get; set; } // "COD", "Momo", "VNPay"

        [Required]
        public bool IsPaid { get; set; } // Trạng thái thanh toán

        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.ChoXuLy;

        public int? PromotionCodeId { get; set; } // Liên kết với mã khuyến mãi (nullable)

        public PromotionCode PromotionCode { get; set; } // Navigation property

        public int PointsEarned { get; set; } // Điểm tích lũy từ đơn hàng

        public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

        public List<CustomerPoint> CustomerPoints { get; set; } = new List<CustomerPoint>(); // Navigation property
    }
}