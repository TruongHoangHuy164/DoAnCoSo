using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DoAnLTW.Models
{
    public class BookServiceViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn thú cưng")]
        public int PetId { get; set; }

        [Required]
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Ngày hẹn là bắt buộc")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày hẹn")]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Giờ hẹn là bắt buộc")]
        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Giờ hẹn phải có định dạng HH:mm")]
        [Display(Name = "Giờ hẹn")]
        public string? AppointmentTimeString { get; set; }

        [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        // Danh sách thú cưng của người dùng để hiển thị trong dropdown
        public List<Pet>? UserPets { get; set; }

        // Thông tin dịch vụ đã chọn
        public Service? SelectedService { get; set; }
    }
}