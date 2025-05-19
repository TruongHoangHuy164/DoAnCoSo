using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace DoAnLTW.Models
{
    public class RoomType
    {
        [Key]
        public int RoomTypeId { get; set; }

        [Required(ErrorMessage = "Tên loại phòng là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên loại phòng không được quá 100 ký tự")]
        public string Name { get; set; } // Ví dụ: Standard, Deluxe, VIP

        [Required(ErrorMessage = "Mô tả là bắt buộc")]
        [StringLength(500)]
        public string Description { get; set; }

        [Required(ErrorMessage = "Giá phòng là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
        public decimal PricePerNight { get; set; } // Giá mỗi đêm

        public List<HotelRoom> Rooms { get; set; } = new List<HotelRoom>();
    }
}