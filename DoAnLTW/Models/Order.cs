using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DoAnLTW.Models
{
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
        public bool IsPaid { get; set; } // Thêm thuộc tính để lưu trạng thái thanh toán

        public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}