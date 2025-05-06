using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnLTW.Models
{
    // Định nghĩa enum cho trạng thái đặt dịch vụ
    public enum PetServiceStatus
    {
        [Display(Name = "Chờ xác nhận")]
        ChoXacNhan,

        [Display(Name = "Đã xác nhận")]
        DaXacNhan,

        [Display(Name = "Đang thực hiện")]
        DangThucHien,

        [Display(Name = "Hoàn thành")]
        HoanThanh,

        [Display(Name = "Đã hủy")]
        DaHuy
    }

    public class PetService
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PetId { get; set; }

        [ForeignKey("PetId")]
        public Pet? Pet { get; set; }

        [Required]
        public int ServiceId { get; set; }

        [ForeignKey("ServiceId")]
        public Service? Service { get; set; }

        [Required]
        public string? UserId { get; set; } // Người đặt dịch vụ

        [Required(ErrorMessage = "Ngày đặt dịch vụ là bắt buộc")]
        [DataType(DataType.Date)]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Giờ đặt dịch vụ là bắt buộc")]
        [DataType(DataType.Time)]
        public DateTime AppointmentTime { get; set; }

        [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
        public string? Address { get; set; }

        public string? Note { get; set; }

        [Required]
        public DateTime BookingDate { get; set; } = DateTime.Now;

        [Required]
        public PetServiceStatus Status { get; set; } = PetServiceStatus.ChoXacNhan;

        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
        public decimal Price { get; set; }
    }
}