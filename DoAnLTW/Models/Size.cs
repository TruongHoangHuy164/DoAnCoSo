using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnLTW.Models
{
    public class Size
    {
        [Key]
        public int SizeId { get; set; }

        [Required(ErrorMessage = "Kích thước là bắt buộc")]
        [StringLength(30, ErrorMessage = "Kích thước không được quá 30 ký tự")]
        public string? size { get; set; }

        public List<ProductSize> ProductSizes { get; set; } = new List<ProductSize>(); // Sửa thành ProductSizes
    }
}