using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnLTW.Models
{
    public class WishProductList
    {
        [Key] // Đánh dấu Id là khóa chính
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } // Liên kết với bảng AspNetUsers

        [Required]
        public int ProductId { get; set; } // Liên kết với bảng Products

        // Navigation property
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }
}
