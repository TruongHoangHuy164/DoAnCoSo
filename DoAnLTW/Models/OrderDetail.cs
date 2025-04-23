using System.ComponentModel.DataAnnotations;

namespace DoAnLTW.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public string ProductName { get; set; } // Thêm ProductName để lưu tên sản phẩm

        [Required]
        public string Size { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
        public decimal Price { get; set; }

        // Thuộc tính tính toán: Tổng giá của chi tiết đơn hàng
        public decimal TotalPrice => Price * Quantity;

        // Navigation properties
        public Order Order { get; set; }
        public Product Product { get; set; }
    }
}