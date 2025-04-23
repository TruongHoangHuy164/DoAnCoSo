using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnLTW.Models
{
    public class Brand
    {
        [Key]
        public int BrandId { get; set; }

        [Required(ErrorMessage = "Thương hiệu là bắt buộc")]
        [StringLength(50, ErrorMessage = "Thương hiệu không được quá 50 ký tự")]
        public string Name { get; set; } 

        // Một brand có thể được dùng bởi nhiều sản phẩm
        public List<Product> Products { get; set; } = new List<Product>();
    }
}   