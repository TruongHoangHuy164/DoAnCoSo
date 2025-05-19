using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnLTW.Models
{
    public enum PetHotelBookingStatus
    {
        [Display(Name = "Chờ xác nhận")]
        ChoXacNhan,
        [Display(Name = "Đã xác nhận")]
        DaXacNhan,
        [Display(Name = "Đang sử dụng")]
        DangSuDung,
        [Display(Name = "Hoàn thành")]
        HoanThanh,
        [Display(Name = "Đã hủy")]
        DaHuy
    }

    public class PetHotelBooking
    {
        [Key]
        public int BookingId { get; set; }

        [Required]
        public int PetId { get; set; }

        [ForeignKey("PetId")]
        public Pet Pet { get; set; }

        [Required]
        public int RoomId { get; set; }

        [ForeignKey("RoomId")]
        public HotelRoom Room { get; set; }

        [Required]
        public string UserId { get; set; } // Người đặt phòng

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
        [StringLength(200)]
        public string Address { get; set; }

        public string Note { get; set; }

        [Required]
        public DateTime BookingDate { get; set; } = DateTime.Now;

        [Required]
        public PetHotelBookingStatus Status { get; set; } = PetHotelBookingStatus.ChoXacNhan;

        [Range(0, double.MaxValue, ErrorMessage = "Tổng giá phải lớn hơn hoặc bằng 0")]
        public decimal TotalPrice { get; set; }
    }
}