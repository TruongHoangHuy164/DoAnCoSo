using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnLTW.Models
{
    public class PetImages
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PetId { get; set; }

        [ForeignKey("PetId")]
        public Pet? Pet { get; set; }

        [Required(ErrorMessage = "URL ảnh là bắt buộc")]
        public string ImageUrl { get; set; }

        public bool IsMainImage { get; set; } // Xác định ảnh chính
    }
}