using System.ComponentModel.DataAnnotations;

namespace DoAnLTW.Models
{
    public class Service
    {
        [Key]
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Tên dịch vụ là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên dịch vụ không được quá 100 ký tự")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Mô tả dịch vụ là bắt buộc")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Giá dịch vụ là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Thời gian thực hiện dịch vụ là bắt buộc")]
        public int Duration { get; set; } // Thời gian thực hiện dịch vụ (phút)

        // Bỏ trường ImageUrl
        // public string ImageUrl { get; set; }

        // Navigation property
        public List<PetService> PetServices { get; set; } = new List<PetService>();
    }
}