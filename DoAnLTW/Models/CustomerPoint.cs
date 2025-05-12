using System;
using System.ComponentModel.DataAnnotations;

namespace DoAnLTW.Models
{
    public class CustomerPoint
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } // Liên kết với IdentityUser

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Điểm phải lớn hơn hoặc bằng 0")]
        public int Points { get; set; } // Tổng số điểm hiện tại

        [Required]
        public int OrderId { get; set; } // Liên kết với đơn hàng tạo ra điểm

        [Required]
        public DateTime EarnedDate { get; set; } = DateTime.Now; // Ngày nhận điểm

        // Navigation property
        public Order Order { get; set; } // Liên kết với Order
    }
}