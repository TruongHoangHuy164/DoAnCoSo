    using System.ComponentModel.DataAnnotations;

    namespace DoAnLTW.Models
    {
        public class Category
        {
            public int CategoryId { get; set; }

            [Required(ErrorMessage = "Tên danh mục là bắt buộc")]
            [StringLength(50, ErrorMessage = "Tên danh mục không được vượt quá 50 ký tự")]
            public string? Name { get; set; }

        // Một danh mục có nhiều sản phẩm
        [Required(ErrorMessage = "ảnh danh mục là bắt buộc")]
        public string ?ImageUrl { get; set; }
        public List<Product> Products { get; set; } = new List<Product>();
        }
    }
