using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnLTW.Models
{
    public class Pet
    {
        [Key]
        public int PetId { get; set; }

        [Required(ErrorMessage = "Tên thú cưng là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên thú cưng không được quá 100 ký tự")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Loại thú cưng là bắt buộc")]
        [StringLength(50, ErrorMessage = "Loại thú cưng không được quá 50 ký tự")]
        public string? Type { get; set; } // Chó, mèo, v.v.

        [StringLength(50)]
        public string? Breed { get; set; } // Giống

        [Range(0, 30, ErrorMessage = "Tuổi phải từ 0 đến 30")]
        public int Age { get; set; }

        public string? Gender { get; set; } // Đực, Cái

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public string?  UserId { get; set; } // Liên kết với người dùng sở hữu

        // Navigation property
        public List<PetService> PetServices { get; set; } = new List<PetService>();
    }
}