using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnLTW.Models
{
    public class Product_Images
    {
        [Key]
        public int Product_ImagesId { get; set; } // Khóa chính

        [Required]
        public int ProductId { get; set; } // Khóa ngoại liên kết với Product

        [Required(ErrorMessage = "Đường dẫn hình ảnh là bắt buộc")]
        public string ImageUrl { get; set; } // Lưu đường dẫn ảnh

        [ForeignKey("ProductId")]
        public Product? Product { get; set; } // Navigation property
    }
}
