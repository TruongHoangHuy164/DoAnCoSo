using System;
using System.ComponentModel.DataAnnotations;

namespace DoAnLTW.Models
{
    public class PromotionCode
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20, ErrorMessage = "Mã khuyến mãi không được quá 20 ký tự")]
        public string Code { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Giá trị giảm giá phải lớn hơn hoặc bằng 0")]
        public decimal DiscountAmount { get; set; } // Giảm giá cố định (VND)

        [Range(0, 100, ErrorMessage = "Phần trăm giảm giá phải từ 0 đến 100")]
        public decimal DiscountPercentage { get; set; } // Giảm giá theo phần trăm (%)

        public bool IsActive { get; set; } = true; // Trạng thái mã (true = active, false = disabled)

        public DateTime? StartDate { get; set; } // Ngày bắt đầu hiệu lực

        public DateTime? EndDate { get; set; } // Ngày hết hạn

        [Range(0, int.MaxValue, ErrorMessage = "Số lần sử dụng tối đa phải lớn hơn hoặc bằng 0")]
        public int MaxUsage { get; set; } // Số lần sử dụng tối đa (0 = không giới hạn)

        public int UsageCount { get; set; } // Số lần đã sử dụng
    }
}