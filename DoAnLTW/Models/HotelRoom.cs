using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnLTW.Models
{
    public class HotelRoom
    {
        [Key]
        public int RoomId { get; set; }

        [Required]
        public int RoomTypeId { get; set; }

        [NotMapped]
        [ForeignKey("RoomTypeId")]
        public RoomType RoomType { get; set; }

        [Required(ErrorMessage = "Số phòng là bắt buộc")]
        [StringLength(50, ErrorMessage = "Số phòng không được quá 50 ký tự")]
        public string RoomNumber { get; set; } // Ví dụ: Room 101, Room 102

        public bool IsAvailable { get; set; } = true; // Trạng thái phòng (trống hay không)

        public List<PetHotelBooking> Bookings { get; set; } = new List<PetHotelBooking>();
    }
}